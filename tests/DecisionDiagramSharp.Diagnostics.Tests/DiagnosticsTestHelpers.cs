namespace DecisionDiagramSharp.Diagnostics.Tests;

internal static class DiagnosticsTestHelpers
{
    public static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
