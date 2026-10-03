using System.Diagnostics;

namespace OpenSpatial.Spatial;

public static class SpatialTracer
{
    public static readonly ActivitySource Source = new("openspatial");

    // Services call DeclareOutput/DeclareSink from inside a RunAsync lambda
    // to tag which named path (P1/P2/P3 or S1/S2/S3) was taken.
    // AsyncLocal flows through async continuations correctly.
    private static readonly AsyncLocal<string?> _currentPath = new();
    public static void DeclareOutput(string path) => _currentPath.Value = path;
    public static void DeclareSink(string path)   => _currentPath.Value = path;

    // ── Monitored async overload (emits I_k/O_k/S_k counters + health register)
    public static async Task<T> RunAsync<T>(
        SpatialAttribute    attr,
        MonitoringAttribute monitor,
        string              leafName,
        Func<Task<T>>       operation)
    {
        var coordinate = $"{attr.Coordinate}.{leafName}";
        var cfg        = MonitoringRuntime.Decode(coordinate, monitor.Path);

        _currentPath.Value = null;
        var sw = Stopwatch.StartNew();
        SpatialMeter.RecordEntry(coordinate, cfg);

        using var span = Source.StartActivity(coordinate);
        span?.SetTag("spatial.coordinate", coordinate);
        span?.SetTag("spatial.capability", attr.Capability);
        span?.SetTag("spatial.context",    attr.Context);
        span?.SetTag("spatial.container",  attr.Container);
        span?.SetTag("spatial.component",  attr.Component);

        try
        {
            var result = await operation();
            sw.Stop();
            var path = _currentPath.Value ?? "P1";
            SpatialMeter.RecordOutput(coordinate, path, sw.ElapsedMilliseconds, cfg);
            span?.SetTag("spatial.outcome", "OUTPUT");
            span?.SetTag("spatial.path",    path);
            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var path = _currentPath.Value ?? "S1";
            SpatialMeter.RecordSink(coordinate, path, sw.ElapsedMilliseconds, cfg);
            span?.SetTag("spatial.outcome", "SINK:TIMEOUT");
            span?.SetTag("spatial.path",    path);
            span?.SetStatus(ActivityStatusCode.Error, "TIMEOUT");
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            SpatialMeter.RecordException(coordinate, cfg);
            span?.SetTag("spatial.outcome", "SINK:ERROR");
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    // ── Non-monitored async overload (backward compatible — traces only)
    public static async Task<T> RunAsync<T>(
        SpatialAttribute attr,
        string           leafName,
        Func<Task<T>>    operation)
    {
        var coordinate = $"{attr.Coordinate}.{leafName}";

        using var span = Source.StartActivity(coordinate);
        span?.SetTag("spatial.coordinate", coordinate);
        span?.SetTag("spatial.capability", attr.Capability);
        span?.SetTag("spatial.context",    attr.Context);
        span?.SetTag("spatial.container",  attr.Container);
        span?.SetTag("spatial.component",  attr.Component);

        try
        {
            var result = await operation();
            span?.SetTag("spatial.outcome", "OUTPUT");
            return result;
        }
        catch (OperationCanceledException)
        {
            span?.SetTag("spatial.outcome", "SINK:TIMEOUT");
            span?.SetStatus(ActivityStatusCode.Error, "TIMEOUT");
            throw;
        }
        catch (Exception ex)
        {
            span?.SetTag("spatial.outcome", "SINK:ERROR");
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    // ── Monitored sync overload
    public static T Run<T>(
        SpatialAttribute    attr,
        MonitoringAttribute monitor,
        string              leafName,
        Func<T>             operation)
    {
        var coordinate = $"{attr.Coordinate}.{leafName}";
        var cfg        = MonitoringRuntime.Decode(coordinate, monitor.Path);

        _currentPath.Value = null;
        var sw = Stopwatch.StartNew();
        SpatialMeter.RecordEntry(coordinate, cfg);

        using var span = Source.StartActivity(coordinate);
        span?.SetTag("spatial.coordinate", coordinate);
        span?.SetTag("spatial.capability", attr.Capability);

        try
        {
            var result = operation();
            sw.Stop();
            var path = _currentPath.Value ?? "P1";
            SpatialMeter.RecordOutput(coordinate, path, sw.ElapsedMilliseconds, cfg);
            span?.SetTag("spatial.outcome", "OUTPUT");
            span?.SetTag("spatial.path",    path);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            SpatialMeter.RecordException(coordinate, cfg);
            span?.SetTag("spatial.outcome", "SINK:ERROR");
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    // ── Non-monitored sync overload (backward compatible)
    public static T Run<T>(
        SpatialAttribute attr,
        string           leafName,
        Func<T>          operation)
    {
        var coordinate = $"{attr.Coordinate}.{leafName}";

        using var span = Source.StartActivity(coordinate);
        span?.SetTag("spatial.coordinate", coordinate);
        span?.SetTag("spatial.capability", attr.Capability);

        var result = operation();
        span?.SetTag("spatial.outcome", "OUTPUT");
        return result;
    }
}
