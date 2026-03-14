namespace ConverterApp.Converters.Html2Maui;

/// <summary>
/// CSS property ↔ MAUI property mapping tables.
/// </summary>
public static class StyleMappings
{
    /// <summary>Direct CSS property → MAUI property name mapping.</summary>
    public static readonly Dictionary<string, string> CssToMauiProperty = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Color ────────────────────────────────────────────────
        ["color"]                = "TextColor",
        ["background-color"]     = "BackgroundColor",
        ["background"]           = "BackgroundColor",

        // ── Spacing ──────────────────────────────────────────────
        ["padding"]              = "Padding",
        ["padding-top"]          = "Padding",   // simplified: use Thickness
        ["padding-right"]        = "Padding",
        ["padding-bottom"]       = "Padding",
        ["padding-left"]         = "Padding",
        ["margin"]               = "Margin",
        ["margin-top"]           = "Margin",
        ["margin-right"]         = "Margin",
        ["margin-bottom"]        = "Margin",
        ["margin-left"]          = "Margin",

        // ── Sizing ───────────────────────────────────────────────
        ["width"]                = "WidthRequest",
        ["height"]               = "HeightRequest",
        ["min-width"]            = "MinimumWidthRequest",
        ["min-height"]           = "MinimumHeightRequest",
        ["max-width"]            = "MaximumWidthRequest",
        ["max-height"]           = "MaximumHeightRequest",

        // ── Typography ───────────────────────────────────────────
        ["font-size"]            = "FontSize",
        ["font-family"]          = "FontFamily",
        ["line-height"]          = "LineHeight",
        ["letter-spacing"]       = "CharacterSpacing",
        ["text-decoration"]      = "TextDecorations",
        ["text-transform"]       = "TextTransform",

        // ── Alignment ────────────────────────────────────────────
        ["text-align"]           = "HorizontalTextAlignment",
        ["vertical-align"]       = "VerticalTextAlignment",

        // ── Border ───────────────────────────────────────────────
        ["border-radius"]        = "CornerRadius",
        ["border-width"]         = "BorderWidth",
        ["border-color"]         = "BorderColor",

        // ── Visibility ───────────────────────────────────────────
        ["opacity"]              = "Opacity",
        ["visibility"]           = "IsVisible",

        // ── Layout ───────────────────────────────────────────────
        ["flex-direction"]       = "Orientation",
        ["justify-content"]      = "HorizontalOptions",
        ["align-items"]          = "VerticalOptions",
        ["overflow"]             = "IsClippedToBounds",
        ["z-index"]              = "ZIndex",
    };

    /// <summary>
    /// Value transformations for specific CSS values (property → (cssValue → mauiValue)).
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, string>> ValueTransforms =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["font-weight"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["bold"]    = "FontAttributes=\"Bold\"",
            ["bolder"]  = "FontAttributes=\"Bold\"",
            ["normal"]  = "FontAttributes=\"None\"",
        },
        ["font-style"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["italic"]  = "FontAttributes=\"Italic\"",
            ["normal"]  = "FontAttributes=\"None\"",
        },
        ["display"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["flex"]    = "<!-- StackLayout -->",
            ["grid"]    = "<!-- Grid -->",
            ["none"]    = "IsVisible=\"False\"",
            ["block"]   = "<!-- VerticalStackLayout -->",
            ["inline"]  = "<!-- HorizontalStackLayout -->",
            ["inline-block"] = "<!-- HorizontalStackLayout -->",
        },
        ["text-align"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["left"]    = "Start",
            ["center"]  = "Center",
            ["right"]   = "End",
            ["justify"] = "Justify",
        },
        ["vertical-align"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["top"]     = "Start",
            ["middle"]  = "Center",
            ["bottom"]  = "End",
        },
        ["visibility"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hidden"]  = "False",
            ["visible"] = "True",
        },
        ["flex-direction"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["row"]     = "Horizontal",
            ["column"]  = "Vertical",
        },
        ["overflow"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hidden"]  = "True",
            ["visible"] = "False",
        },
        ["text-decoration"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["underline"]    = "Underline",
            ["line-through"] = "Strikethrough",
            ["none"]         = "None",
        },
        ["text-transform"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["uppercase"] = "Uppercase",
            ["lowercase"] = "Lowercase",
            ["none"]      = "None",
        },
    };

    /// <summary>
    /// Heading tag → FontSize mapping.
    /// </summary>
    public static readonly Dictionary<string, string> HeadingFontSize = new()
    {
        ["h1"] = "Title",
        ["h2"] = "Title",
        ["h3"] = "Subtitle",
        ["h4"] = "Large",
        ["h5"] = "Medium",
        ["h6"] = "Small",
    };

    /// <summary>
    /// Converts a raw CSS pixel/em/rem/% value string into a MAUI-friendly number.
    /// Returns the numeric portion only; MAUI properties are unit-less.
    /// </summary>
    public static string ConvertCssValue(string cssValue)
    {
        cssValue = cssValue.Trim();

        // percentage → leave as-is with a comment (MAUI uses -1 for auto / Star in Grid)
        if (cssValue.EndsWith('%'))
            return cssValue.TrimEnd('%') == "100" ? "-1" : cssValue.TrimEnd('%');

        // px, em, rem, pt, vw, vh → strip unit
        foreach (var unit in new[] { "px", "rem", "em", "pt", "vw", "vh", "dvh", "dvw" })
            if (cssValue.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                return cssValue[..^unit.Length].Trim();

        // Named color: pass through
        return cssValue;
    }

    /// <summary>
    /// Converts a CSS shorthand like "10px 20px" into MAUI Thickness syntax "10,20".
    /// Handles 1, 2, 3 and 4-value shorthands.
    /// </summary>
    public static string ConvertThickness(string cssValue)
    {
        var parts = cssValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nums = parts.Select(ConvertCssValue).ToArray();
        return nums.Length switch
        {
            1 => nums[0],
            2 => $"{nums[1]},{nums[0]},{nums[1]},{nums[0]}",   // MAUI: left,top,right,bottom
            3 => $"{nums[1]},{nums[0]},{nums[1]},{nums[2]}",
            4 => $"{nums[3]},{nums[0]},{nums[1]},{nums[2]}",
            _ => nums[0],
        };
    }
}
