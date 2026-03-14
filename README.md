# Converter

Bidirectional code converter between **HTML/CSS/JS**, **.NET MAUI XAML/C#**, **WinForms C#**, and **Unity C#**.

Implemented as a standalone **.NET MAUI Desktop application** (Windows/macOS/Linux) — no backend required, fully offline.

---

## Features

| Source | Target | Notes |
|--------|--------|-------|
| HTML + CSS + JS | MAUI XAML + C# | Full element, style and event mapping |
| MAUI XAML + C# code-behind | HTML + CSS + JS | Reverse conversion |
| WinForms Designer C# | HTML + CSS + JS | Absolute positioning preserved |
| WinForms Designer C# | MAUI XAML + C# | AbsoluteLayout |
| WinForms Designer C# | Unity C# MonoBehaviour | UGUI mapping |
| HTML | Unity C# stub | Via MAUI intermediate |

All conversions run **locally** — no network calls needed.

**GitHub Import** downloads source files from any public GitHub repository to populate the input editor.

---

## Element Mappings (HTML ↔ MAUI)

| HTML | MAUI |
|------|------|
| `<div>` | `<StackLayout>` / `<Grid>` |
| `<button>` | `<Button>` |
| `<input type="text">` | `<Entry>` |
| `<input type="number">` | `<Entry Keyboard="Numeric">` |
| `<textarea>` | `<Editor>` |
| `<label>` | `<Label>` |
| `<img>` | `<Image>` |
| `<a>` | `<Label>` with `<TapGestureRecognizer>` |
| `<h1>`–`<h6>` | `<Label FontSize="Title" FontAttributes="Bold">` |
| `<span>` | `<Label>` |
| `<p>` | `<Label LineBreakMode="WordWrap">` |
| `<select>` | `<Picker>` |
| `<input type="checkbox">` | `<CheckBox>` |
| `<form>` | `<VerticalStackLayout>` |

## Style Mappings (CSS ↔ MAUI)

| CSS | MAUI |
|-----|------|
| `color` | `TextColor` |
| `background-color` | `BackgroundColor` |
| `padding` | `Padding` |
| `margin` | `Margin` |
| `width` | `WidthRequest` |
| `height` | `HeightRequest` |
| `font-size` | `FontSize` |
| `font-weight: bold` | `FontAttributes="Bold"` |
| `text-align` | `HorizontalTextAlignment` |
| `border` | `BorderWidth` + `BorderColor` |
| `display: flex` | `StackLayout` |
| `display: grid` | `Grid` |
| `opacity` | `Opacity` |
| `border-radius` | `CornerRadius` |

## Event Mappings (JS ↔ C#)

| JavaScript | MAUI C# |
|------------|---------|
| `onclick` | `Clicked` |
| `onchange` | `PropertyChanged` |
| `onload` | `Loaded` |
| `onsubmit` | `Command` |
| `addEventListener('click', fn)` | `Clicked += fn` |
| `removeEventListener` | `-= fn` |

---

## Project Structure

```
Converter/
├── src/
│   └── ConverterApp/
│       ├── Converters/
│       │   ├── Html2Maui/          # HTML→MAUI logic
│       │   │   ├── ElementMappings.cs
│       │   │   ├── StyleMappings.cs
│       │   │   ├── EventMappings.cs
│       │   │   └── HtmlToMauiConverter.cs
│       │   ├── Maui2Html/          # MAUI→HTML logic
│       │   │   └── MauiToHtmlConverter.cs
│       │   └── WinForms2All/       # WinForms→HTML/MAUI/Unity logic
│       │       └── WinFormsConverter.cs
│       ├── Models/                 # ConversionResult, ConversionOptions
│       ├── Pages/                  # MainPage.xaml, SettingsPage.xaml
│       ├── Services/               # ConversionEngine, FileService, GitHubImportService
│       ├── ViewModels/             # MainViewModel (MVVM)
│       └── Resources/              # Styles, fonts, images
└── tests/
    └── ConverterApp.Tests/         # 61 xUnit tests
```

---

## Building

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- .NET MAUI workload: `dotnet workload install maui`
- OpenSans fonts in `src/ConverterApp/Resources/Fonts/`:
  - `OpenSans-Regular.ttf`
  - `OpenSans-Semibold.ttf`
  - (Download from [Google Fonts](https://fonts.google.com/specimen/Open+Sans))

### Run tests (no MAUI SDK required)

```bash
dotnet test tests/ConverterApp.Tests
```

### Build for Windows

```bash
dotnet build src/ConverterApp -f net9.0-windows10.0.19041.0
```

### Publish as single-file exe (Windows)

```bash
dotnet publish src/ConverterApp -f net9.0-windows10.0.19041.0 -c Release \
  -p:WindowsPackageType=None \
  -p:PublishSingleFile=true --self-contained true
```

---

## Usage

1. Launch the app
2. Select a **Conversion Mode** from the dropdown
3. Paste or load source code (HTML / XAML / WinForms C#)
4. Optionally paste CSS or C# code-behind in the secondary panel
5. Click **▶ Convert**
6. Review the generated output on the right panel
7. Click **💾 Export** to save files to disk or **📋 Copy** to copy to clipboard

### GitHub Import

Enter a public GitHub repository URL in the toolbar (e.g. `https://github.com/owner/repo`) and click **⬇ Import from GitHub** to automatically download and populate source files.

---

## License

MIT
