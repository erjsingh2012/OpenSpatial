using System.Diagnostics;

namespace OpenSpatial.Spatial;

public static class SpatialTracer
{
    public static readonly ActivitySource Source = new("openspatial");

    // Wraps async T8 Leaf / T6 Action methods
    public static async Task<T> RunAsync<T>(
        SpatialAttribute attr,
        string           leafName,
        Func<Task<T>>    operation)
    {
        var coordinate = $"{attr.Coordinate}.{leafName}";

        using var span = Source.StartActivity(coordinate);
        span?.SetTag("spatial.coordinate",  coordinate);
        span?.SetTag("spatial.capability",  attr.Capability);
        span?.SetTag("spatial.context",     attr.Context);
        span?.SetTag("spatial.container",   attr.Container);
        span?.SetTag("spatial.component",   attr.Component);

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

    // Wraps sync T7 Task methods
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
