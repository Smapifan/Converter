namespace ConverterApp.Models;

/// <summary>
/// Holds the output produced by a conversion operation.
/// </summary>
public class ConversionResult
{
    /// <summary>Whether the conversion succeeded without fatal errors.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable error or warning messages.</summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Named output files produced by the conversion.
    /// Key = suggested filename (e.g. "MainPage.xaml"),
    /// Value = file content.
    /// </summary>
    public Dictionary<string, string> OutputFiles { get; set; } = new();

    /// <summary>Convenience: primary output text (first file, or combined).</summary>
    public string PrimaryOutput =>
        OutputFiles.Count > 0 ? string.Join("\n\n", OutputFiles.Values) : string.Empty;

    public static ConversionResult Fail(string message) =>
        new() { Success = false, Messages = { message } };

    public static ConversionResult Ok(Dictionary<string, string> files, IEnumerable<string>? warnings = null)
    {
        var result = new ConversionResult { Success = true, OutputFiles = files };
        if (warnings != null)
            result.Messages.AddRange(warnings);
        return result;
    }
}
