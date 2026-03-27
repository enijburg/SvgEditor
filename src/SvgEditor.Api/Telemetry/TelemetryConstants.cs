using System.Diagnostics;

namespace SvgEditor.Api.Telemetry;

public static class TelemetryConstants
{
    public const string ServiceName = "SvgEditor.Api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}
