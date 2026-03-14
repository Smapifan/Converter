namespace ConverterApp.Converters.Html2Maui;

/// <summary>
/// Static dictionaries that define HTML ↔ MAUI element mappings.
/// </summary>
public static class ElementMappings
{
    /// <summary>
    /// Maps an HTML tag name (lower-case) to the MAUI XAML element name.
    /// Some mappings require additional attributes – those are expressed as
    /// (MauiTag, ExtraAttributes?) pairs.
    /// </summary>
    public static readonly Dictionary<string, MauiElementInfo> HtmlToMaui = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Layout ──────────────────────────────────────────────
        ["div"]      = new("StackLayout"),
        ["section"]  = new("StackLayout"),
        ["article"]  = new("StackLayout"),
        ["aside"]    = new("StackLayout"),
        ["header"]   = new("StackLayout"),
        ["footer"]   = new("StackLayout"),
        ["main"]     = new("VerticalStackLayout"),
        ["nav"]      = new("HorizontalStackLayout"),
        ["ul"]       = new("VerticalStackLayout"),
        ["ol"]       = new("VerticalStackLayout"),
        ["li"]       = new("Label"),
        ["form"]     = new("VerticalStackLayout"),
        ["fieldset"] = new("Border"),

        // ── Text ────────────────────────────────────────────────
        ["p"]        = new("Label",    new() { ["LineBreakMode"] = "WordWrap" }),
        ["span"]     = new("Label"),
        ["label"]    = new("Label"),
        ["strong"]   = new("Label",    new() { ["FontAttributes"] = "Bold" }),
        ["em"]       = new("Label",    new() { ["FontAttributes"] = "Italic" }),
        ["small"]    = new("Label",    new() { ["FontSize"] = "Small" }),
        ["h1"]       = new("Label",    new() { ["FontSize"] = "Title",    ["FontAttributes"] = "Bold" }),
        ["h2"]       = new("Label",    new() { ["FontSize"] = "Title",    ["FontAttributes"] = "Bold" }),
        ["h3"]       = new("Label",    new() { ["FontSize"] = "Subtitle", ["FontAttributes"] = "Bold" }),
        ["h4"]       = new("Label",    new() { ["FontSize"] = "Large",    ["FontAttributes"] = "Bold" }),
        ["h5"]       = new("Label",    new() { ["FontSize"] = "Medium",   ["FontAttributes"] = "Bold" }),
        ["h6"]       = new("Label",    new() { ["FontSize"] = "Small",    ["FontAttributes"] = "Bold" }),
        ["a"]        = new("Label",    new() { ["TextColor"] = "{StaticResource Primary}" }, HasTapGesture: true),
        ["code"]     = new("Label",    new() { ["FontFamily"] = "Monospace" }),
        ["pre"]      = new("Label",    new() { ["FontFamily"] = "Monospace", ["LineBreakMode"] = "NoWrap" }),

        // ── Form Controls ───────────────────────────────────────
        ["button"]   = new("Button"),
        ["textarea"] = new("Editor"),
        ["select"]   = new("Picker"),
        ["option"]   = new("Label"),         // child of Picker – handled specially

        // ── Media ───────────────────────────────────────────────
        ["img"]      = new("Image"),
        ["video"]    = new("MediaElement"),  // requires CommunityToolkit.Maui
        ["audio"]    = new("MediaElement"),

        // ── Table ───────────────────────────────────────────────
        ["table"]    = new("Grid"),
        ["thead"]    = new("Grid"),
        ["tbody"]    = new("Grid"),
        ["tr"]       = new("Grid"),
        ["td"]       = new("Label"),
        ["th"]       = new("Label",    new() { ["FontAttributes"] = "Bold" }),

        // ── Misc ────────────────────────────────────────────────
        ["hr"]       = new("BoxView",  new() { ["HeightRequest"] = "1", ["Color"] = "Gray" }),
        ["br"]       = new("Label",    new() { ["Text"] = "" }),
    };

    /// <summary>
    /// Maps an HTML &lt;input type&gt; attribute value to a MAUI control.
    /// </summary>
    public static readonly Dictionary<string, MauiElementInfo> InputTypeToMaui = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"]     = new("Entry"),
        ["password"] = new("Entry",    new() { ["IsPassword"] = "True" }),
        ["number"]   = new("Entry",    new() { ["Keyboard"] = "Numeric" }),
        ["email"]    = new("Entry",    new() { ["Keyboard"] = "Email" }),
        ["tel"]      = new("Entry",    new() { ["Keyboard"] = "Telephone" }),
        ["url"]      = new("Entry",    new() { ["Keyboard"] = "Url" }),
        ["search"]   = new("SearchBar"),
        ["checkbox"] = new("CheckBox"),
        ["radio"]    = new("RadioButton"),
        ["range"]    = new("Slider"),
        ["date"]     = new("DatePicker"),
        ["time"]     = new("TimePicker"),
        ["file"]     = new("Button",   new() { ["Text"] = "Choose File" }),
        ["submit"]   = new("Button"),
        ["reset"]    = new("Button"),
        ["button"]   = new("Button"),
        ["hidden"]   = new(string.Empty),  // No MAUI equivalent – skipped
        ["color"]    = new("Entry"),       // No native color picker
    };

    /// <summary>
    /// Reverse mapping: MAUI tag → primary HTML element.
    /// </summary>
    public static readonly Dictionary<string, string> MauiToHtml = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StackLayout"]          = "div",
        ["VerticalStackLayout"]  = "div",
        ["HorizontalStackLayout"]= "div",
        ["Grid"]                 = "div",
        ["FlexLayout"]           = "div",
        ["AbsoluteLayout"]       = "div",
        ["RelativeLayout"]       = "div",
        ["ScrollView"]           = "div",
        ["ContentView"]          = "div",
        ["Frame"]                = "div",
        ["Border"]               = "div",
        ["Label"]                = "span",
        ["Button"]               = "button",
        ["Entry"]                = "input",
        ["Editor"]               = "textarea",
        ["CheckBox"]             = "input",
        ["RadioButton"]          = "input",
        ["Slider"]               = "input",
        ["Switch"]               = "input",
        ["Stepper"]              = "input",
        ["Picker"]               = "select",
        ["DatePicker"]           = "input",
        ["TimePicker"]           = "input",
        ["SearchBar"]            = "input",
        ["Image"]                = "img",
        ["ImageButton"]          = "button",
        ["BoxView"]              = "div",
        ["WebView"]              = "iframe",
        ["ActivityIndicator"]    = "div",
        ["ProgressBar"]          = "progress",
        ["CollectionView"]       = "ul",
        ["ListView"]             = "ul",
        ["TableView"]            = "table",
        ["MediaElement"]         = "video",
    };
}

/// <summary>Describes a MAUI element and optional pre-set attributes.</summary>
public record MauiElementInfo(
    string Tag,
    Dictionary<string, string>? Attributes = null,
    bool HasTapGesture = false)
{
    public MauiElementInfo(string tag) : this(tag, null, false) { }
}
