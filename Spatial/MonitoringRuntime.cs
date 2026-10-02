using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace OpenSpatial.Spatial;

// Decoded view of a 16-bit monitoring path word.
// High byte: enable mask  [P1|P2|P3|S1|S2|S3|E_|Uk]
// Low byte:  sample rates [Out:3|Sink:3|E-flag|U-flag]
public readonly struct PathConfig
{
    public readonly ushort Word;
    public PathConfig(ushort word) => Word = word;

    // Enable bits — per path
    public bool P1 => (Word & 0x8000) != 0;
    public bool P2 => (Word & 0x4000) != 0;
    public bool P3 => (Word & 0x2000) != 0;
    public bool S1 => (Word & 0x1000) != 0;
    public bool S2 => (Word & 0x0800) != 0;
    public bool S3 => (Word & 0x0400) != 0;
    public bool E_ => (Word & 0x0200) != 0;
    public bool Uk => (Word & 0x0100) != 0;

    // Sample rates — per group
    public double OutputRate => _decode((Word >> 5) & 0x07);
    public double SinkRate   => _decode((Word >> 2) & 0x07);
    public double ERate      => (Word & 0x02) != 0 ? 1.0 : 0.10;
    public double URate      => (Word & 0x01) != 0 ? 1.0 : 0.10;

    public bool ShouldSampleOutput() => (P1 || P2 || P3) && _roll(OutputRate);
    public bool ShouldSampleSink()   => (S1 || S2 || S3) && _roll(SinkRate);
    public bool ShouldSampleE()      => E_ && _roll(ERate);
    public bool ShouldSampleU()      => Uk && _roll(URate);

    private static double _decode(int code) => code switch
    {
        0 => 0.00, 1 => 0.02, 2 => 0.05, 3 => 0.10,
        4 => 0.25, 5 => 0.50, 6 => 1.00, _ => 1.00
    };

    private static bool _roll(double rate) =>
        rate >= 1.0 || (rate > 0 && Random.Shared.NextDouble() < rate);

    private string _enabledPaths => string.Concat(
        P1 ? "P1 " : "-- ", P2 ? "P2 " : "-- ", P3 ? "P3 " : "-- ",
        S1 ? "S1 " : "-- ", S2 ? "S2 " : "-- ", S3 ? "S3 " : "-- ",
        E_ ? "E_" : "--", " ", Uk ? "Uk" : "--").TrimEnd();

    public override string ToString() =>
        $"0x{Word:X4}  [{_enabledPaths}]  out@{OutputRate * 100:0}%  sink@{SinkRate * 100:0}%  E@{ERate * 100:0}%  U@{URate * 100:0}%";
}

// Thread-safe registry for runtime monitoring overrides.
// Resolution order: in-process override → env var → decorator default
public static class MonitoringRuntime
{
    private static readonly ConcurrentDictionary<string, ushort> _overrides = new();

    // Override the path config for a coordinate (feature flag, admin API, incident responder).
    public static void SetPath(string coordinate, ushort path) =>
        _overrides[coordinate] = path;

    // Remove override → falls back to env var or decorator default.
    public static void ClearPath(string coordinate) =>
        _overrides.TryRemove(coordinate, out _);

    // Resolve: in-process → env var → decorator default.
    public static ushort GetEffectivePath(string coordinate, ushort defaultPath)
    {
        if (_overrides.TryGetValue(coordinate, out var v)) return v;

        var envKey = "SPATIAL_PATH_" +
            coordinate.ToUpperInvariant().Replace('.', '_').Replace(':', '_');
        var env = Environment.GetEnvironmentVariable(envKey);
        if (env is not null)
        {
            var hex = env.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? env[2..] : env;
            if (ushort.TryParse(hex, NumberStyles.HexNumber, null, out var ep))
                return ep;
        }

        return defaultPath;
    }

    public static PathConfig Decode(string coordinate, ushort defaultPath) =>
        new(GetEffectivePath(coordinate, defaultPath));

    // Print the effective config for a coordinate — source + decoded breakdown.
    public static void Inspect(string coordinate, ushort defaultPath)
    {
        var envKey = "SPATIAL_PATH_" +
            coordinate.ToUpperInvariant().Replace('.', '_').Replace(':', '_');
        var src = _overrides.ContainsKey(coordinate)              ? "runtime override"
                : Environment.GetEnvironmentVariable(envKey) != null ? "env var"
                : "decorator default";

        var cfg   = Decode(coordinate, defaultPath);
        var label = coordinate.Contains(':') ? coordinate.Split(':')[1] : coordinate;
        Console.WriteLine($"  {label}");
        Console.WriteLine($"    source : {src}");
        Console.WriteLine($"    config : {cfg}");
    }
}
