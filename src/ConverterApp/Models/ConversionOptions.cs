namespace ConverterApp.Models;

/// <summary>
/// Supported conversion modes.
/// </summary>
public enum ConversionMode
{
    HtmlToMaui,
    MauiToHtml,
    WinFormsToMaui,
    WinFormsToHtml,
    WinFormsToUnity,
    HtmlToUnity
}

/// <summary>
/// Options that control conversion behaviour.
/// </summary>
public class ConversionOptions
{
    /// <summary>Direction of the conversion.</summary>
    public ConversionMode Mode { get; set; } = ConversionMode.HtmlToMaui;

    /// <summary>Generate MVVM binding code instead of code-behind.</summary>
    public bool GenerateMvvm { get; set; } = true;

    /// <summary>Generate XML comments in XAML output.</summary>
    public bool GenerateComments { get; set; } = true;

    /// <summary>Indentation string used for output code.</summary>
    public string Indent { get; set; } = "    ";

    /// <summary>MAUI namespace prefix used in XAML output.</summary>
    public string MauiXmlns { get; set; } = "http://schemas.microsoft.com/dotnet/2021/maui";

    /// <summary>Target .NET version for generated project files.</summary>
    public string TargetFramework { get; set; } = "net9.0";

    /// <summary>Default MAUI app namespace for generated code.</summary>
    public string AppNamespace { get; set; } = "MyApp";
}
