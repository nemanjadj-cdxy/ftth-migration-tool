namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Normalizes NEO's various sourceSystem spellings into the two values CxO expects.
public static class SourceSystemMapper
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        if (raw.Contains("hansen", StringComparison.OrdinalIgnoreCase))
        {
            return "HansenProv";
        }

        if (
            raw.Contains("listener", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("kafka", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "GKEL";
        }

        return raw;
    }
}
