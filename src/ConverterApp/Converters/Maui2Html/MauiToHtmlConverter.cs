using System.Text;
using System.Text.RegularExpressions;
using ConverterApp.Converters.Html2Maui;
using ConverterApp.Models;

namespace ConverterApp.Converters.Maui2Html;

/// <summary>
/// Converts .NET MAUI XAML (and optional C# code-behind) back to HTML + CSS + JS.
/// </summary>
public partial class MauiToHtmlConverter
{
    private readonly ConversionOptions _opts;

    public MauiToHtmlConverter(ConversionOptions? options = null)
        => _opts = options ?? new ConversionOptions();

    // ──────────────────────────────────────────────────────────────────────────
    //  Public entry point
    // ──────────────────────────────────────────────────────────────────────────

    public ConversionResult Convert(string xamlSource, string? csSource = null)
    {
        if (string.IsNullOrWhiteSpace(xamlSource))
            return ConversionResult.Fail("XAML source is empty.");

        var warnings = new List<string>();
        var cssClasses = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var jsEvents = new List<string>();

        var nodes = ParseXamlNodes(xamlSource);

        // Find root content (inside ContentPage / Page / etc.)
        var content = FindContent(nodes);

        var htmlBody = new StringBuilder();
        WriteHtmlNodes(htmlBody, content, cssClasses, jsEvents, indent: 2, warnings);

        var css = BuildCss(cssClasses);
        var js = BuildJs(jsEvents, csSource);
        var html = BuildFullHtml(htmlBody.ToString(), css);

        return ConversionResult.Ok(
            new Dictionary<string, string>
            {
                ["index.html"] = html,
                ["styles.css"] = css,
                ["app.js"]     = js,
            },
            warnings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HTML generation
    // ──────────────────────────────────────────────────────────────────────────

    private void WriteHtmlNodes(
        StringBuilder sb,
        IEnumerable<XamlNode> nodes,
        Dictionary<string, Dictionary<string, string>> cssClasses,
        List<string> jsEvents,
        int indent,
        List<string> warnings)
    {
        var pad = new string(' ', indent * 2);

        foreach (var node in nodes)
        {
            if (node.IsText)
            {
                sb.AppendLine($"{pad}{HtmlEncode(node.TextContent)}");
                continue;
            }

            var mauiTag = node.TagName;
            if (!ElementMappings.MauiToHtml.TryGetValue(mauiTag, out var htmlTag))
            {
                warnings.Add($"No HTML mapping for MAUI element <{mauiTag}> – using <div>.");
                htmlTag = "div";
            }

            var (htmlAttrs, cssProps) = MapAttributes(node, mauiTag, warnings);

            // Register CSS class if we have style properties
            string? cssClass = null;
            if (cssProps.Count > 0)
            {
                var id = node.GetAttr("x:Name");
                cssClass = string.IsNullOrEmpty(id) ? $"maui-{mauiTag.ToLowerInvariant()}-{cssClasses.Count}" : id;
                cssClasses[cssClass] = cssProps;
                if (htmlAttrs.ContainsKey("class"))
                    htmlAttrs["class"] += " " + cssClass;
                else
                    htmlAttrs["class"] = cssClass;
            }

            // JS events
            var jsEvent = MapMauiEventToJs(node, mauiTag, jsEvents);
            if (jsEvent.HasValue)
                htmlAttrs[jsEvent.Value.Key] = jsEvent.Value.Value;

            // Special element adjustments
            AdjustHtmlTag(ref htmlTag, ref htmlAttrs, node, mauiTag, warnings);

            bool hasChildren = node.Children.Count > 0;
            var attrStr = FormatHtmlAttrs(htmlAttrs);

            if (hasChildren)
            {
                sb.AppendLine($"{pad}<{htmlTag}{attrStr}>");
                WriteHtmlNodes(sb, node.Children, cssClasses, jsEvents, indent + 1, warnings);
                sb.AppendLine($"{pad}</{htmlTag}>");
            }
            else
            {
                var textContent = GetTextContent(node, mauiTag);
                if (IsSelfClosingHtml(htmlTag))
                    sb.AppendLine($"{pad}<{htmlTag}{attrStr} />");
                else if (!string.IsNullOrEmpty(textContent))
                    sb.AppendLine($"{pad}<{htmlTag}{attrStr}>{HtmlEncode(textContent)}</{htmlTag}>");
                else
                    sb.AppendLine($"{pad}<{htmlTag}{attrStr}></{htmlTag}>");
            }
        }
    }

    private static (Dictionary<string, string> HtmlAttrs, Dictionary<string, string> CssProps)
        MapAttributes(XamlNode node, string mauiTag, List<string> warnings)
    {
        var html = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var css = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (mauiAttr, val) in node.Attributes)
        {
            switch (mauiAttr.ToLowerInvariant())
            {
                case "x:name":
                    html["id"] = val;
                    html["name"] = val;
                    break;
                case "text":
                    // handled via GetTextContent
                    break;
                case "placeholder":
                    html["placeholder"] = val;
                    break;
                case "issecure" or "ispassword":
                    html["type"] = "password";
                    break;
                case "isenabled" when val == "False":
                    html["disabled"] = "disabled";
                    break;
                case "isreadonly" when val == "True":
                    html["readonly"] = "readonly";
                    break;
                case "ischecked" when val == "True":
                    html["checked"] = "checked";
                    break;
                case "isvisible" when val == "False":
                    css["display"] = "none";
                    break;
                case "keyboard":
                    html["inputmode"] = val.ToLowerInvariant();
                    break;
                case "source":
                    html["src"] = val;
                    break;
                case "commandparameter":
                    html["href"] = val;
                    break;
                case "maxlength":
                    html["maxlength"] = val;
                    break;
                // CSS properties
                case "textcolor":
                    css["color"] = val;
                    break;
                case "backgroundcolor":
                    css["background-color"] = val;
                    break;
                case "padding":
                    css["padding"] = ConvertThicknessToCSS(val);
                    break;
                case "margin":
                    css["margin"] = ConvertThicknessToCSS(val);
                    break;
                case "widthrequest":
                    css["width"] = val + "px";
                    break;
                case "heightrequest":
                    css["height"] = val + "px";
                    break;
                case "fontsize":
                    css["font-size"] = MapFontSize(val);
                    break;
                case "fontattributes":
                    if (val.Contains("Bold", StringComparison.OrdinalIgnoreCase))
                        css["font-weight"] = "bold";
                    if (val.Contains("Italic", StringComparison.OrdinalIgnoreCase))
                        css["font-style"] = "italic";
                    break;
                case "fontfamily":
                    css["font-family"] = val;
                    break;
                case "horizontaltextalignment":
                    css["text-align"] = val.ToLowerInvariant() switch
                    {
                        "start" => "left",
                        "center" => "center",
                        "end" => "right",
                        _ => val.ToLowerInvariant()
                    };
                    break;
                case "opacity":
                    css["opacity"] = val;
                    break;
                case "cornerradius":
                    css["border-radius"] = val + "px";
                    break;
                case "borderwidth":
                    css["border-width"] = val + "px";
                    break;
                case "bordercolor":
                    css["border-color"] = val;
                    break;
                case "linebreakmode":
                    if (val.Equals("WordWrap", StringComparison.OrdinalIgnoreCase))
                        css["word-wrap"] = "break-word";
                    break;
                case "orientation":
                    css["flex-direction"] = val.Equals("Horizontal", StringComparison.OrdinalIgnoreCase)
                        ? "row" : "column";
                    css["display"] = "flex";
                    break;
                // Skip XAML-only attributes
                case "xmlns" or "x:class" or "shell.navbarisvisible" or "title":
                    break;
                default:
                    warnings.Add($"Unmapped MAUI attribute '{mauiAttr}' on <{mauiTag}> – skipped.");
                    break;
            }
        }

        return (html, css);
    }

    private static KeyValuePair<string, string>? MapMauiEventToJs(XamlNode node, string mauiTag, List<string> jsEvents)
    {
        var evMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clicked"]    = "onclick",
            ["TextChanged"] = "oninput",
            ["Focused"]    = "onfocus",
            ["Unfocused"]  = "onblur",
            ["Loaded"]     = "onload",
            ["Tapped"]     = "onclick",
        };

        foreach (var (mauiAttr, val) in node.Attributes)
        {
            if (evMap.TryGetValue(mauiAttr, out var jsEvent))
            {
                var handlerName = val;
                jsEvents.Add($"// Handler: {handlerName}");
                return new KeyValuePair<string, string>(jsEvent, $"{handlerName}(event)");
            }
        }
        return null;
    }

    private static void AdjustHtmlTag(
        ref string htmlTag,
        ref Dictionary<string, string> htmlAttrs,
        XamlNode node,
        string mauiTag,
        List<string> warnings)
    {
        // Entry → input type
        if (mauiTag == "Entry")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "text";
            if (htmlAttrs.TryGetValue("inputmode", out var km))
            {
                htmlAttrs["type"] = km switch
                {
                    "numeric" => "number",
                    "email"   => "email",
                    "telephone" => "tel",
                    "url"     => "url",
                    _ => "text",
                };
            }
        }
        else if (mauiTag == "CheckBox")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "checkbox";
        }
        else if (mauiTag == "RadioButton")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "radio";
        }
        else if (mauiTag == "Slider")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "range";
        }
        else if (mauiTag == "DatePicker")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "date";
        }
        else if (mauiTag == "TimePicker")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "time";
        }
        else if (mauiTag == "SearchBar")
        {
            htmlTag = "input";
            htmlAttrs["type"] = "search";
        }
        else if (mauiTag == "HorizontalStackLayout")
        {
            // handled via CSS flex in attributes
        }
        else if (mauiTag is "VerticalStackLayout" or "StackLayout")
        {
            // default div is fine
        }
    }

    private static string GetTextContent(XamlNode node, string mauiTag)
    {
        if (node.Attributes.TryGetValue("Text", out var text))
            return text;
        return string.Empty;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CSS / JS output builders
    // ──────────────────────────────────────────────────────────────────────────

    private static string BuildCss(Dictionary<string, Dictionary<string, string>> cssClasses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* Generated by Converter – MAUI to HTML */");
        sb.AppendLine();
        sb.AppendLine("*, *::before, *::after { box-sizing: border-box; }");
        sb.AppendLine("body { font-family: sans-serif; margin: 0; padding: 0; }");
        sb.AppendLine();

        foreach (var (cls, props) in cssClasses)
        {
            sb.AppendLine($".{cls} {{");
            foreach (var (prop, val) in props)
                sb.AppendLine($"  {prop}: {val};");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildJs(List<string> jsEvents, string? csSource)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Generated by Converter – MAUI C# to JavaScript");
        sb.AppendLine();

        foreach (var ev in jsEvents)
            sb.AppendLine(ev);

        if (!string.IsNullOrWhiteSpace(csSource))
        {
            sb.AppendLine();
            sb.AppendLine("// ---- Converted from C# code-behind ----");
            sb.AppendLine(ConvertCSharpToJs(csSource));
        }

        return sb.ToString();
    }

    private static string BuildFullHtml(string body, string css)
    {
        return $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Converted Page</title>
  <link rel="stylesheet" href="styles.css" />
</head>
<body>
{body}
  <script src="app.js"></script>
</body>
</html>
""";
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  C# → JS (basic event handler conversion)
    // ──────────────────────────────────────────────────────────────────────────

    private static string ConvertCSharpToJs(string cs)
    {
        var sb = new StringBuilder();
        // Match private/public void MethodName(... EventArgs ...)
        foreach (Match m in EventHandlerRegex().Matches(cs))
        {
            var methodName = m.Groups[1].Value;
            var body = m.Groups[2].Value.Trim();
            var jsBody = TranslateBasicCSharpToJs(body);
            sb.AppendLine($"function {methodName}(event) {{");
            sb.AppendLine($"  {jsBody}");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string TranslateBasicCSharpToJs(string csharp)
    {
        return csharp
            .Replace("Console.WriteLine", "console.log")
            .Replace("MessageBox.Show", "alert")
            .Replace("Navigation.PushAsync", "window.location.assign")
            .Replace("await ", "await ")
            .Replace("var ", "let ")
            .Replace("string ", "let ")
            .Replace("int ", "let ")
            .Replace("bool ", "let ")
            .Replace("true", "true")
            .Replace("false", "false")
            .Replace("null", "null")
            .Replace("//", "//");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  XAML parsing
    // ──────────────────────────────────────────────────────────────────────────

    internal static List<XamlNode> ParseXamlNodes(string xaml)
    {
        var root = new XamlNode { TagName = "__root__" };
        var stack = new Stack<XamlNode>();
        stack.Push(root);

        foreach (Match m in XamlTokenRegex().Matches(xaml))
        {
            var full = m.Value;
            if (full.StartsWith("</"))
            {
                if (stack.Count > 1) stack.Pop();
            }
            else if (full.StartsWith('<') && !full.StartsWith("<?") && !full.StartsWith("<!"))
            {
                var isSelfClose = full.EndsWith("/>");
                var inner = full.TrimStart('<').TrimEnd('>').TrimEnd('/').Trim();
                var node = ParseXamlTag(inner);
                stack.Peek().Children.Add(node);
                if (!isSelfClose)
                    stack.Push(node);
            }
        }

        return root.Children;
    }

    private static XamlNode ParseXamlTag(string inner)
    {
        var node = new XamlNode();
        var nameMatch = XamlTagNameRegex().Match(inner);
        node.TagName = nameMatch.Success ? nameMatch.Groups[1].Value : "div";

        foreach (Match am in XamlAttrRegex().Matches(inner))
        {
            var key = am.Groups[1].Value;
            var val = am.Groups[2].Value;
            node.Attributes[key] = val;
        }

        return node;
    }

    private static List<XamlNode> FindContent(List<XamlNode> nodes)
    {
        foreach (var n in nodes)
        {
            var tag = n.TagName;
            if (tag.EndsWith("Page", StringComparison.OrdinalIgnoreCase) ||
                tag == "ContentPage" || tag == "Shell")
                return FindContent(n.Children);

            if (tag is "ScrollView" or "ContentView" or "Grid" or "StackLayout"
                or "VerticalStackLayout" or "HorizontalStackLayout" or "Frame" or "Border")
                return n.Children;
        }
        return nodes;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static string ConvertThicknessToCSS(string thickness)
    {
        var parts = thickness.Split(',');
        return parts.Length switch
        {
            1 => parts[0].Trim() + "px",
            // MAUI order: left,top,right,bottom → CSS: top right bottom left
            4 => $"{parts[1].Trim()}px {parts[2].Trim()}px {parts[3].Trim()}px {parts[0].Trim()}px",
            2 => $"{parts[1].Trim()}px {parts[0].Trim()}px",
            _ => thickness + "px",
        };
    }

    private static string MapFontSize(string maui) =>
        maui.ToLowerInvariant() switch
        {
            "title"    => "32px",
            "subtitle" => "24px",
            "large"    => "20px",
            "medium"   => "17px",
            "small"    => "14px",
            "micro"    => "12px",
            _          => double.TryParse(maui, out var d) ? $"{d}px" : maui,
        };

    private static bool IsSelfClosingHtml(string tag) =>
        tag is "input" or "img" or "br" or "hr" or "meta" or "link";

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FormatHtmlAttrs(Dictionary<string, string> attrs)
    {
        if (attrs.Count == 0) return string.Empty;
        return " " + string.Join(" ", attrs
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{kv.Key}=\"{kv.Value}\""));
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Compiled regexes
    // ──────────────────────────────────────────────────────────────────────────

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex XamlTokenRegex();

    [GeneratedRegex(@"^([\w.:]+)")]
    private static partial Regex XamlTagNameRegex();

    [GeneratedRegex(@"([\w:.\-]+)=""([^""]*)""")]
    private static partial Regex XamlAttrRegex();

    [GeneratedRegex(@"(?:private|public|protected)\s+(?:async\s+)?(?:void|Task)\s+(\w+)\s*\([^)]*EventArgs[^)]*\)\s*\{([^}]*)\}", RegexOptions.Singleline)]
    private static partial Regex EventHandlerRegex();
}

// ──────────────────────────────────────────────────────────────────────────
//  XAML node model
// ──────────────────────────────────────────────────────────────────────────

internal class XamlNode
{
    public string TagName { get; set; } = string.Empty;
    public bool IsText { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<XamlNode> Children { get; } = new();

    public string GetAttr(string name, string def = "") =>
        Attributes.TryGetValue(name, out var v) ? v : def;
}
