namespace ConverterApp.Converters.Html2Maui;

/// <summary>
/// JavaScript ↔ C# event mapping tables.
/// </summary>
public static class EventMappings
{
    /// <summary>
    /// Maps an HTML/JS inline event attribute (or addEventListener name) to the
    /// MAUI C# event name.
    /// </summary>
    public static readonly Dictionary<string, string> JsToCSharpEvent = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Mouse / Pointer ─────────────────────────────────────
        ["onclick"]           = "Clicked",
        ["click"]             = "Clicked",
        ["ondblclick"]        = "Tapped",  // double-tap gesture
        ["dblclick"]          = "Tapped",
        ["onmousedown"]       = "Pressed",
        ["mousedown"]         = "Pressed",
        ["onmouseup"]         = "Released",
        ["mouseup"]           = "Released",
        ["onmouseenter"]      = "PointerEntered",
        ["mouseenter"]        = "PointerEntered",
        ["onmouseleave"]      = "PointerExited",
        ["mouseleave"]        = "PointerExited",
        ["oncontextmenu"]     = "LongPressed",
        ["contextmenu"]       = "LongPressed",

        // ── Keyboard ────────────────────────────────────────────
        ["onkeydown"]         = "TextChanged",
        ["keydown"]           = "TextChanged",
        ["onkeyup"]           = "TextChanged",
        ["keyup"]             = "TextChanged",
        ["onkeypress"]        = "TextChanged",
        ["keypress"]          = "TextChanged",

        // ── Form ────────────────────────────────────────────────
        ["onchange"]          = "TextChanged",
        ["change"]            = "TextChanged",
        ["oninput"]           = "TextChanged",
        ["input"]             = "TextChanged",
        ["onfocus"]           = "Focused",
        ["focus"]             = "Focused",
        ["onblur"]            = "Unfocused",
        ["blur"]              = "Unfocused",
        ["onsubmit"]          = "Command",
        ["submit"]            = "Command",
        ["onreset"]           = "Clicked",
        ["reset"]             = "Clicked",
        ["onselect"]          = "SelectionChanged",
        ["select"]            = "SelectionChanged",

        // ── Page / Window ────────────────────────────────────────
        ["onload"]            = "Loaded",
        ["load"]              = "Loaded",
        ["onunload"]          = "Unloaded",
        ["unload"]            = "Unloaded",
        ["onresize"]          = "SizeChanged",
        ["resize"]            = "SizeChanged",
        ["onscroll"]          = "Scrolled",
        ["scroll"]            = "Scrolled",

        // ── Drag ────────────────────────────────────────────────
        ["ondragstart"]       = "DragStarting",
        ["dragstart"]         = "DragStarting",
        ["ondrop"]            = "Drop",
        ["drop"]              = "Drop",
    };

    /// <summary>
    /// Generates a C# event handler method stub for a given event name.
    /// </summary>
    public static string GenerateHandlerStub(string mauiEvent, string elementId, string handlerBody = "")
    {
        var handlerName = $"On{elementId.ToPascalCase()}_{mauiEvent}";
        var paramSig = mauiEvent switch
        {
            "Clicked" or "Pressed" or "Released" or "LongPressed"
                => "object? sender, EventArgs e",
            "TextChanged"
                => "object? sender, TextChangedEventArgs e",
            "Focused" or "Unfocused"
                => "object? sender, FocusEventArgs e",
            "Loaded" or "Unloaded"
                => "object? sender, EventArgs e",
            "SelectionChanged"
                => "object? sender, SelectionChangedEventArgs e",
            "Scrolled"
                => "object? sender, ScrolledEventArgs e",
            _ => "object? sender, EventArgs e",
        };

        var body = string.IsNullOrWhiteSpace(handlerBody)
            ? "        // TODO: implement handler"
            : $"        {handlerBody}";

        return $"    private void {handlerName}({paramSig})\n    {{\n{body}\n    }}";
    }
}

/// <summary>Helper extension for identifier casing.</summary>
internal static class StringExtensions
{
    public static string ToPascalCase(this string s)
    {
        if (string.IsNullOrEmpty(s)) return "Element";
        return char.ToUpperInvariant(s[0]) + s[1..];
    }
}
