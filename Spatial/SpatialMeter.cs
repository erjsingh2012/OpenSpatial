using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace OpenSpatial.Spatial;

// OTel Meter for [Monitoring] counters, gauges, and health registers.
// Production: swap the console exporter for AddOtlpExporter → Grafana.
public static class SpatialMeter
{
    public static readonly Meter Meter = new("openspatial.monitoring", "1.0.0");

    // Counters — always incremented (no sampling); cheap long add
    private static readonly Counter<long> _ik =
        Meter.CreateCounter<long>("spatial.events.in",
            description: "I_k — events entered a monitored coordinate");
    private static readonly Counter<long> _ok =
        Meter.CreateCounter<long>("spatial.events.out",
            description: "O_k — events exited via a named output path");
    private static readonly Counter<long> _sk =
        Meter.CreateCounter<long>("spatial.events.sink",
            description: "S_k — events terminated via a named sink path");
    private static readonly Counter<long> _ek =
        Meter.CreateCounter<long>("spatial.events.exception",
            description: "E_ — unhandled exceptions (not mapped to any sink path)");

    // Sampled histograms — only recorded when PathConfig.ShouldSample*() passes
    private static readonly Histogram<long> _latOut =
        Meter.CreateHistogram<long>("spatial.latency.output.ms", "ms",
            "Output path round-trip latency (sampled per Path config)");
    private static readonly Histogram<long> _latSink =
        Meter.CreateHistogram<long>("spatial.latency.sink.ms", "ms",
            "Sink path latency to terminal state (sampled per Path config)");

    // Per-coordinate raw counts — used for U_k computation and DumpHealthReport
    private static readonly ConcurrentDictionary<string, long> _ikRaw = new();
    private static readonly ConcurrentDictionary<string, long> _okRaw = new();
    private static readonly ConcurrentDictionary<string, long> _skRaw = new();

    // 8-bit health register per coordinate [O_P1|O_P2|O_P3|S_S1|S_S2|S_S3|E_|Uk]
    private static readonly ConcurrentDictionary<string, int> _health = new();

    private const int B_P1 = 0x80, B_P2 = 0x40, B_P3 = 0x20;
    private const int B_S1 = 0x10, B_S2 = 0x08, B_S3 = 0x04;
    private const int B_E  = 0x02, B_UK = 0x01;

    static SpatialMeter()
    {
        // U_k gauge: I_k − (O_k + S_k) per coordinate — alert when > 0
        Meter.CreateObservableGauge<long>("spatial.uk.gauge",
            () => _ikRaw.Keys.Select(coord =>
            {
                var uk = _ikRaw.GetValueOrDefault(coord, 0)
                       - (_okRaw.GetValueOrDefault(coord, 0)
                        + _skRaw.GetValueOrDefault(coord, 0));
                return new Measurement<long>(uk,
                    new KeyValuePair<string, object?>("spatial.coordinate", coord));
            }),
            description: "U_k = I_k - (O_k + S_k) — unaccounted events; alert when > 0");

        // 8-bit health register — which paths have fired for each coordinate
        Meter.CreateObservableGauge<int>("spatial.health.register",
            () => _health.Select(kv =>
                new Measurement<int>(kv.Value,
                    new KeyValuePair<string, object?>("spatial.coordinate", kv.Key))),
            description: "8-bit health register [O_P1|O_P2|O_P3|S_S1|S_S2|S_S3|E_|Uk]");
    }

    public static void RecordEntry(string coordinate, PathConfig cfg)
    {
        if (cfg.Word == 0) return;
        _ik.Add(1, new KeyValuePair<string, object?>("spatial.coordinate", coordinate));
        _ikRaw.AddOrUpdate(coordinate, 1, (_, v) => v + 1);
        _syncUkBit(coordinate);
    }

    public static void RecordOutput(string coordinate, string path, long ms, PathConfig cfg)
    {
        if (cfg.Word == 0) return;
        _ok.Add(1,
            new KeyValuePair<string, object?>("spatial.coordinate", coordinate),
            new KeyValuePair<string, object?>("spatial.path", path));
        _okRaw.AddOrUpdate(coordinate, 1, (_, v) => v + 1);

        var bit = path switch { "P1" => B_P1, "P2" => B_P2, "P3" => B_P3, _ => 0 };
        if (bit != 0) _health.AddOrUpdate(coordinate, bit, (_, v) => v | bit);

        if (cfg.ShouldSampleOutput())
            _latOut.Record(ms,
                new KeyValuePair<string, object?>("spatial.coordinate", coordinate),
                new KeyValuePair<string, object?>("spatial.path", path));

        _syncUkBit(coordinate);
    }

    public static void RecordSink(string coordinate, string path, long ms, PathConfig cfg)
    {
        if (cfg.Word == 0) return;
        _sk.Add(1,
            new KeyValuePair<string, object?>("spatial.coordinate", coordinate),
            new KeyValuePair<string, object?>("spatial.path", path));
        _skRaw.AddOrUpdate(coordinate, 1, (_, v) => v + 1);

        var bit = path switch { "S1" => B_S1, "S2" => B_S2, "S3" => B_S3, _ => 0 };
        if (bit != 0) _health.AddOrUpdate(coordinate, bit, (_, v) => v | bit);

        if (cfg.ShouldSampleSink())
            _latSink.Record(ms,
                new KeyValuePair<string, object?>("spatial.coordinate", coordinate),
                new KeyValuePair<string, object?>("spatial.path", path));

        _syncUkBit(coordinate);
    }

    public static void RecordException(string coordinate, PathConfig cfg)
    {
        if (cfg.Word == 0) return;
        _ek.Add(1, new KeyValuePair<string, object?>("spatial.coordinate", coordinate));
        // exceptions count as sinks so they close the I_k window
        _skRaw.AddOrUpdate(coordinate, 1, (_, v) => v + 1);
        _health.AddOrUpdate(coordinate, B_E, (_, v) => v | B_E);
        _syncUkBit(coordinate);
    }

    // Uk bit: set when unaccounted events exist, cleared when balanced
    private static void _syncUkBit(string coordinate)
    {
        var uk = _ikRaw.GetValueOrDefault(coordinate, 0)
               - (_okRaw.GetValueOrDefault(coordinate, 0)
                + _skRaw.GetValueOrDefault(coordinate, 0));
        _health.AddOrUpdate(coordinate,
            uk > 0 ? B_UK : 0,
            (_, v) => uk > 0 ? v | B_UK : v & ~B_UK);
    }

    // Human-readable health dump — shown at end of demo
    public static void DumpHealthReport()
    {
        Console.WriteLine("── Monitoring health report ────────────────────────────────────────");
        if (_ikRaw.IsEmpty) { Console.WriteLine("  (no monitored coordinates recorded)"); return; }

        foreach (var coord in _ikRaw.Keys.OrderBy(x => x))
        {
            var ik  = _ikRaw.GetValueOrDefault(coord, 0);
            var ok  = _okRaw.GetValueOrDefault(coord, 0);
            var sk  = _skRaw.GetValueOrDefault(coord, 0);
            var uk  = ik - (ok + sk);
            var reg = _health.GetValueOrDefault(coord, 0);

            var paths = string.Concat(
                (reg & B_P1) != 0 ? "P1 " : "",
                (reg & B_P2) != 0 ? "P2 " : "",
                (reg & B_P3) != 0 ? "P3 " : "",
                (reg & B_S1) != 0 ? "S1 " : "",
                (reg & B_S2) != 0 ? "S2 " : "",
                (reg & B_S3) != 0 ? "S3 " : "",
                (reg & B_E)  != 0 ? "E_ " : "",
                (reg & B_UK) != 0 ? "Uk"  : "").TrimEnd();

            var label  = coord.Contains(':') ? coord.Split(':')[1] : coord;
            var ukFlag = uk > 0 ? " ⚠ U_k ALERT" : "";
            Console.WriteLine($"  {label}");
            Console.WriteLine($"    counters : I_k={ik}  O_k={ok}  S_k={sk}  U_k={uk}{ukFlag}");
            Console.WriteLine($"    health   : 0x{reg:X2}  [{paths}]");
        }
    }
}
