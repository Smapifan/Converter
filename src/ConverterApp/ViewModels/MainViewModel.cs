using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterApp.Models;
using ConverterApp.Services;

namespace ConverterApp.ViewModels;

/// <summary>
/// ViewModel for the main converter page.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ConversionEngine _engine;
    private readonly FileService _fileService;
    private readonly GitHubImportService _gitHubService;

    // ── Inputs ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _primaryInput = string.Empty;

    [ObservableProperty]
    private string _secondaryInput = string.Empty;  // CSS or C# code-behind

    [ObservableProperty]
    private string _tertiaryInput = string.Empty;   // JS (only for HTML→MAUI)

    // ── Output ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _primaryOutput = string.Empty;

    [ObservableProperty]
    private string _allOutputFiles = string.Empty;

    [ObservableProperty]
    private Dictionary<string, string> _outputFiles = new();

    // ── State ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ConversionMode _selectedMode = ConversionMode.HtmlToMaui;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _githubUrl = string.Empty;

    [ObservableProperty]
    private string _githubImportStatus = string.Empty;

    // ── Options ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _generateMvvm = true;

    [ObservableProperty]
    private bool _generateComments = true;

    [ObservableProperty]
    private string _appNamespace = "MyApp";

    // ──────────────────────────────────────────────────────────────────────────

    public MainViewModel(
        ConversionEngine engine,
        FileService fileService,
        GitHubImportService gitHubService)
    {
        _engine       = engine;
        _fileService  = fileService;
        _gitHubService = gitHubService;
    }

    public List<ConversionMode> ConversionModes { get; } =
        Enum.GetValues<ConversionMode>().ToList();

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(PrimaryInput))
        {
            StatusMessage = "Please enter some source code.";
            return;
        }

        IsBusy    = true;
        HasErrors = false;

        try
        {
            var opts = new ConversionOptions
            {
                Mode             = SelectedMode,
                GenerateMvvm     = GenerateMvvm,
                GenerateComments = GenerateComments,
                AppNamespace     = AppNamespace,
            };

            var result = _engine.Convert(
                PrimaryInput,
                string.IsNullOrWhiteSpace(SecondaryInput) ? null : SecondaryInput,
                string.IsNullOrWhiteSpace(TertiaryInput)  ? null : TertiaryInput,
                opts);

            if (result.Success)
            {
                OutputFiles  = result.OutputFiles;
                PrimaryOutput = result.PrimaryOutput;

                var allFiles = new System.Text.StringBuilder();
                foreach (var (filename, content) in result.OutputFiles)
                {
                    allFiles.AppendLine($"// ═══ {filename} ═══");
                    allFiles.AppendLine(content);
                    allFiles.AppendLine();
                }
                AllOutputFiles = allFiles.ToString();

                StatusMessage = result.Messages.Count > 0
                    ? $"Converted with {result.Messages.Count} warning(s)."
                    : "Conversion successful!";

                if (result.Messages.Count > 0)
                {
                    HasErrors    = true;
                    ErrorMessage = string.Join("\n", result.Messages);
                }
            }
            else
            {
                HasErrors    = true;
                ErrorMessage = string.Join("\n", result.Messages);
                StatusMessage = "Conversion failed.";
            }
        }
        catch (Exception ex)
        {
            HasErrors    = true;
            ErrorMessage = ex.Message;
            StatusMessage = "Unexpected error during conversion.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (string.IsNullOrEmpty(AllOutputFiles)) return;
        await Clipboard.Default.SetTextAsync(AllOutputFiles);
        StatusMessage = "Copied to clipboard!";
    }

    [RelayCommand]
    private async Task ExportFilesAsync()
    {
        if (OutputFiles.Count == 0)
        {
            StatusMessage = "Nothing to export.";
            return;
        }

        var folder = await _fileService.PickOutputFolderAsync();
        if (string.IsNullOrEmpty(folder))
        {
            StatusMessage = "Export cancelled.";
            return;
        }

        var errors = await _fileService.WriteOutputFilesAsync(folder, OutputFiles);
        StatusMessage = errors.Count == 0
            ? $"Exported {OutputFiles.Count} file(s) to {folder}"
            : $"Export completed with {errors.Count} error(s): {string.Join("; ", errors)}";
    }

    [RelayCommand]
    private async Task LoadFromFileAsync()
    {
        var paths = (await _fileService.PickFilesAsync(
            ".html", ".htm", ".css", ".js",
            ".xaml", ".cs", ".designer.cs")).ToList();

        if (paths.Count == 0) return;

        foreach (var path in paths)
        {
            var ext     = Path.GetExtension(path).ToLowerInvariant();
            var content = await _fileService.ReadFileAsync(path);
            if (content == null) continue;

            if (ext is ".html" or ".htm" || (PrimaryInput.Length == 0))
                PrimaryInput = content;
            else if (ext == ".css" || SecondaryInput.Length == 0)
                SecondaryInput = content;
            else if (ext == ".js")
                TertiaryInput = content;
            else if (ext is ".xaml" or ".cs")
                PrimaryInput = content;
        }

        StatusMessage = $"Loaded {paths.Count} file(s).";
    }

    [RelayCommand]
    private async Task ImportFromGitHubAsync()
    {
        if (string.IsNullOrWhiteSpace(GitHubUrl))
        {
            GitHubImportStatus = "Please enter a GitHub repository URL.";
            return;
        }

        IsBusy = true;
        GitHubImportStatus = "Importing…";

        var progress = new Progress<string>(msg => GitHubImportStatus = msg);

        // Determine which file extensions to look for
        var extensions = SelectedMode switch
        {
            ConversionMode.HtmlToMaui or ConversionMode.HtmlToUnity
                => new[] { ".html", ".htm", ".css", ".js" },
            ConversionMode.MauiToHtml
                => new[] { ".xaml", ".cs" },
            ConversionMode.WinFormsToHtml or ConversionMode.WinFormsToMaui or ConversionMode.WinFormsToUnity
                => new[] { ".cs", ".designer.cs" },
            _ => null,
        };

        var result = await _gitHubService.ImportAsync(GitHubUrl, extensions, progress);

        IsBusy = false;

        if (!result.Success)
        {
            GitHubImportStatus = $"Import failed: {result.Error}";
            return;
        }

        // Distribute imported files
        foreach (var (filePath, content) in result.Files)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext is ".html" or ".htm" && string.IsNullOrWhiteSpace(PrimaryInput))
                PrimaryInput = content;
            else if (ext == ".css" && string.IsNullOrWhiteSpace(SecondaryInput))
                SecondaryInput = content;
            else if (ext == ".js" && string.IsNullOrWhiteSpace(TertiaryInput))
                TertiaryInput = content;
            else if (ext is ".xaml" && string.IsNullOrWhiteSpace(PrimaryInput))
                PrimaryInput = content;
            else if (ext == ".cs" && !filePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(SecondaryInput))
                SecondaryInput = content;
            else if (filePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(PrimaryInput))
                PrimaryInput = content;
        }

        GitHubImportStatus = $"Imported {result.Files.Count} file(s) from GitHub.";
    }

    [RelayCommand]
    private void ClearAll()
    {
        PrimaryInput   = string.Empty;
        SecondaryInput = string.Empty;
        TertiaryInput  = string.Empty;
        PrimaryOutput  = string.Empty;
        AllOutputFiles = string.Empty;
        OutputFiles    = new();
        StatusMessage  = "Cleared.";
        HasErrors      = false;
        ErrorMessage   = string.Empty;
    }

    [RelayCommand]
    private void LoadExample()
    {
        switch (SelectedMode)
        {
            case ConversionMode.HtmlToMaui:
                PrimaryInput   = ExampleCode.HtmlExample;
                SecondaryInput = ExampleCode.CssExample;
                TertiaryInput  = ExampleCode.JsExample;
                break;
            case ConversionMode.MauiToHtml:
                PrimaryInput   = ExampleCode.MauiXamlExample;
                SecondaryInput = ExampleCode.MauiCsExample;
                break;
            case ConversionMode.WinFormsToHtml:
            case ConversionMode.WinFormsToMaui:
            case ConversionMode.WinFormsToUnity:
                PrimaryInput = ExampleCode.WinFormsExample;
                break;
        }
        StatusMessage = "Example loaded.";
    }
}
