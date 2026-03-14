namespace ConverterApp.Services;

/// <summary>
/// Handles reading and writing source/output files locally (no network).
/// </summary>
public class FileService
{
    /// <summary>Reads a text file. Returns null on error.</summary>
    public async Task<string?> ReadFileAsync(string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FileService] ReadFile failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Writes text content to a file. Returns error message or null on success.</summary>
    public async Task<string?> WriteFileAsync(string path, string content)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, content);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Writes multiple output files to a directory.</summary>
    public async Task<List<string>> WriteOutputFilesAsync(
        string outputDirectory,
        Dictionary<string, string> files)
    {
        var errors = new List<string>();
        foreach (var (filename, content) in files)
        {
            var path = Path.Combine(outputDirectory, filename);
            var error = await WriteFileAsync(path, content);
            if (error != null)
                errors.Add($"Failed to write {filename}: {error}");
        }
        return errors;
    }

    /// <summary>Opens a file picker and returns selected file paths.</summary>
    public async Task<IEnumerable<string>> PickFilesAsync(params string[] extensions)
    {
        try
        {
            var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI,       extensions },
                { DevicePlatform.macOS,       extensions },
                { DevicePlatform.iOS,         extensions },
                { DevicePlatform.Android,     extensions },
            });
            var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                FileTypes = types,
            });
            return result?.Select(r => r.FullPath) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Opens a folder picker and returns the selected path.</summary>
    public async Task<string?> PickOutputFolderAsync()
    {
        try
        {
#if WINDOWS
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            // Associate with the current window handle
            var hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
#else
            // Fallback for non-Windows: use a common folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
        }
        catch
        {
            return null;
        }
    }
}
