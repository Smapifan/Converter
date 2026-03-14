using System.Text;
using System.Text.RegularExpressions;
using ConverterApp.Models;

namespace ConverterApp.Converters.Html2Maui;

/// <summary>
/// Converts HTML (+ optional inline CSS / JS) into .NET MAUI XAML and C# code-behind.
/// </summary>
public partial class HtmlToMauiConverter
{
    private readonly ConversionOptions _opts;

    public HtmlToMauiConverter(ConversionOptions? options = null)
        => _opts = options ?? new ConversionOptions();

    // ──────────────────────────────────────────────────────────────────────────
    //  Public entry point
    // ──────────────────────────────────────────────────────────────────────────

    public ConversionResult Convert(string htmlSource, string? cssSource = null, string? jsSource = null)
    {
        if (string.IsNullOrWhiteSpace(htmlSource))
            return ConversionResult.Fail("HTML source is empty.");

        var warnings = new List<string>();

        // 1. Collect CSS classes from <style> tags AND an optional separate CSS file
        var cssRules = ParseCss(ExtractStyleBlocks(htmlSource) + "\n" + (cssSource ?? string.Empty));

        // 2. Collect JS event registrations from <script> tags AND optional JS file
        var jsHandlers = ParseJsHandlers(ExtractScriptBlocks(htmlSource) + "\n" + (jsSource ?? string.Empty));

        // 3. Parse HTML into a lightweight node tree
        var bodyContent = ExtractBodyContent(htmlSource);
        var nodes = ParseHtmlNodes(bodyContent);

        // 4. Generate XAML
        var xamlSb = new StringBuilder();
        WriteXamlHeader(xamlSb);
        WriteXamlNodes(xamlSb, nodes, cssRules, jsHandlers, indent: 2, warnings);
        WriteXamlFooter(xamlSb);

        // 5. Generate C# code-behind
        var csSb = new StringBuilder();
        WriteCodeBehind(csSb, jsHandlers, warnings);

        return ConversionResult.Ok(
            new Dictionary<string, string>
            {
                ["MainPage.xaml"]    = xamlSb.ToString(),
                ["MainPage.xaml.cs"] = csSb.ToString(),
            },
            warnings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  XAML generation
    // ──────────────────────────────────────────────────────────────────────────

    private void WriteXamlHeader(StringBuilder sb)
    {
        sb.AppendLine($"""
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="{_opts.MauiXmlns}"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="{_opts.AppNamespace}.MainPage"
             Title="Main Page">
    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="8">
""");
    }

    private static void WriteXamlFooter(StringBuilder sb)
    {
        sb.AppendLine("""
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
""");
    }

    private void WriteXamlNodes(
        StringBuilder sb,
        IEnumerable<HtmlNode> nodes,
        Dictionary<string, Dictionary<string, string>> cssRules,
        Dictionary<string, string> jsHandlers,
        int indent,
        List<string> warnings)
    {
        var pad = new string(' ', indent * 2);

        foreach (var node in nodes)
        {
            if (node.IsText)
            {
                var text = node.TextContent.Trim();
                if (!string.IsNullOrEmpty(text))
                    sb.AppendLine($"{pad}<Label Text=\"{XmlEscape(text)}\" />");
                continue;
            }

            var tag = node.TagName.ToLowerInvariant();

            // Skip non-visual tags
            if (tag is "script" or "style" or "head" or "meta" or "link" or "title")
                continue;

            // Resolve the MAUI element info
            MauiElementInfo mauiInfo;
            if (tag == "input")
            {
                var type = node.GetAttr("type", "text");
                if (!ElementMappings.InputTypeToMaui.TryGetValue(type, out mauiInfo!))
                    mauiInfo = new MauiElementInfo("Entry");
            }
            else if (!ElementMappings.HtmlToMaui.TryGetValue(tag, out mauiInfo!))
            {
                warnings.Add($"Unknown element <{tag}> – mapped to Label.");
                mauiInfo = new MauiElementInfo("Label");
            }

            if (string.IsNullOrEmpty(mauiInfo.Tag))
                continue; // intentionally skipped

            // Build attribute list
            var attrs = BuildAttributes(node, mauiInfo, cssRules, jsHandlers, tag, warnings);

            bool isContainer = IsContainerElement(tag) && node.Children.Count > 0;

            if (isContainer)
            {
                sb.AppendLine($"{pad}<{mauiInfo.Tag}{FormatAttrs(attrs)}>");
                if (mauiInfo.HasTapGesture)
                    WriteTapGesture(sb, node, jsHandlers, pad + "  ");
                WriteXamlNodes(sb, node.Children, cssRules, jsHandlers, indent + 1, warnings);
                sb.AppendLine($"{pad}</{mauiInfo.Tag}>");
            }
            else
            {
                if (mauiInfo.HasTapGesture)
                {
                    sb.AppendLine($"{pad}<{mauiInfo.Tag}{FormatAttrs(attrs)}>");
                    WriteTapGesture(sb, node, jsHandlers, pad + "  ");
                    sb.AppendLine($"{pad}</{mauiInfo.Tag}>");
                }
                else
                {
                    sb.AppendLine($"{pad}<{mauiInfo.Tag}{FormatAttrs(attrs)} />");
                }
            }
        }
    }

    private Dictionary<string, string> BuildAttributes(
        HtmlNode node,
        MauiElementInfo mauiInfo,
        Dictionary<string, Dictionary<string, string>> cssRules,
        Dictionary<string, string> jsHandlers,
        string tag,
        List<string> warnings)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. Pre-set attributes from mapping
        if (mauiInfo.Attributes != null)
            foreach (var kv in mauiInfo.Attributes)
                attrs[kv.Key] = kv.Value;

        // 2. Common HTML attributes → MAUI attributes
        MapCommonAttributes(node, attrs, tag);

        // 3. CSS styles (inline style= first, then class=)
        ApplyStyles(node, attrs, cssRules, warnings);

        // 4. JS events
        ApplyEvents(node, attrs, jsHandlers, mauiInfo.Tag);

        return attrs;
    }

    private static void MapCommonAttributes(HtmlNode node, Dictionary<string, string> attrs, string tag)
    {
        // id → x:Name
        var id = node.GetAttr("id");
        if (!string.IsNullOrEmpty(id))
            attrs["x:Name"] = id;

        // placeholder / value
        var placeholder = node.GetAttr("placeholder");
        if (!string.IsNullOrEmpty(placeholder))
            attrs["Placeholder"] = placeholder;

        var value = node.GetAttr("value");
        if (!string.IsNullOrEmpty(value))
        {
            if (tag == "button" || tag == "input" && node.GetAttr("type") is "submit" or "reset" or "button")
                attrs["Text"] = value;
            else
                attrs["Text"] = value;
        }

        // text content as Text attribute for leaf elements
        if (tag is "button" or "label" or "span" or "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "a" or "li" or "td" or "th")
        {
            var innerText = node.GetDirectText().Trim();
            if (!string.IsNullOrEmpty(innerText))
                attrs["Text"] = innerText;
        }

        // disabled
        if (node.HasAttr("disabled"))
            attrs["IsEnabled"] = "False";

        // readonly
        if (node.HasAttr("readonly"))
            attrs["IsReadOnly"] = "True";

        // src → Source (img)
        var src = node.GetAttr("src");
        if (!string.IsNullOrEmpty(src))
            attrs["Source"] = src;

        // href → leave TapGesture label Text
        var href = node.GetAttr("href");
        if (!string.IsNullOrEmpty(href))
            attrs["CommandParameter"] = href;

        // rows/cols for textarea → HeightRequest/WidthRequest
        var rows = node.GetAttr("rows");
        if (!string.IsNullOrEmpty(rows) && int.TryParse(rows, out var r))
            attrs["HeightRequest"] = (r * 20).ToString();

        // maxlength
        var max = node.GetAttr("maxlength");
        if (!string.IsNullOrEmpty(max) && int.TryParse(max, out var m))
            attrs["MaxLength"] = m.ToString();

        // checked
        if (node.HasAttr("checked"))
            attrs["IsChecked"] = "True";
    }

    private static void ApplyStyles(
        HtmlNode node,
        Dictionary<string, string> attrs,
        Dictionary<string, Dictionary<string, string>> cssRules,
        List<string> warnings)
    {
        // Class-based styles
        var cls = node.GetAttr("class");
        if (!string.IsNullOrEmpty(cls))
        {
            foreach (var c in cls.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (cssRules.TryGetValue("." + c, out var rule))
                    MergeStyles(rule, attrs, warnings);
                if (cssRules.TryGetValue(c, out var rule2))
                    MergeStyles(rule2, attrs, warnings);
            }
        }

        // Inline styles (highest priority)
        var style = node.GetAttr("style");
        if (!string.IsNullOrEmpty(style))
        {
            var inlineRule = ParseInlineStyle(style);
            MergeStyles(inlineRule, attrs, warnings);
        }
    }

    private static void MergeStyles(
        Dictionary<string, string> cssProps,
        Dictionary<string, string> attrs,
        List<string> warnings)
    {
        foreach (var (cssProp, cssVal) in cssProps)
        {
            // Special composite handling
            if (cssProp.Equals("font-weight", StringComparison.OrdinalIgnoreCase) ||
                cssProp.Equals("font-style", StringComparison.OrdinalIgnoreCase) ||
                cssProp.Equals("display", StringComparison.OrdinalIgnoreCase))
            {
                if (StyleMappings.ValueTransforms.TryGetValue(cssProp, out var transforms) &&
                    transforms.TryGetValue(cssVal, out var transformed))
                {
                    // Transformed value is a full attribute assignment
                    var parts = transformed.Split('=', 2);
                    if (parts.Length == 2)
                        attrs[parts[0].Trim().Trim('<', '!')] = parts[1].Trim().Trim('"');
                    else
                        warnings.Add($"Unhandled CSS transform for {cssProp}:{cssVal}");
                }
                continue;
            }

            if (!StyleMappings.CssToMauiProperty.TryGetValue(cssProp, out var mauiProp))
            {
                warnings.Add($"No MAUI mapping for CSS property '{cssProp}' – skipped.");
                continue;
            }

            string mauiVal;
            // Check for value transform
            if (StyleMappings.ValueTransforms.TryGetValue(cssProp, out var vt) &&
                vt.TryGetValue(cssVal, out var transformed2))
            {
                mauiVal = transformed2;
            }
            else if (mauiProp is "Padding" or "Margin")
            {
                mauiVal = StyleMappings.ConvertThickness(cssVal);
            }
            else
            {
                mauiVal = StyleMappings.ConvertCssValue(cssVal);
            }

            attrs[mauiProp] = mauiVal;
        }
    }

    private static void ApplyEvents(
        HtmlNode node,
        Dictionary<string, string> attrs,
        Dictionary<string, string> jsHandlers,
        string mauiTag)
    {
        foreach (var (attr, val) in node.Attributes)
        {
            if (!attr.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!EventMappings.JsToCSharpEvent.TryGetValue(attr, out var mauiEvent))
                continue;

            // val is either a JS function call ("myFunc()") or inline code
            var handlerRef = DeriveHandlerName(val, attr, node.GetAttr("id"));
            attrs[mauiEvent] = handlerRef;
        }

        // Also handle any addEventListener-derived handlers
        var id = node.GetAttr("id");
        if (!string.IsNullOrEmpty(id) && jsHandlers.TryGetValue(id, out var h))
        {
            if (EventMappings.JsToCSharpEvent.TryGetValue(h, out var ev2))
                attrs[ev2] = $"On{id.ToPascalCase()}_{ev2}";
        }
    }

    private static string DeriveHandlerName(string jsExpression, string eventAttr, string? elementId)
    {
        var jsClean = FunctionCallRegex().Match(jsExpression);
        if (jsClean.Success)
        {
            var name = jsClean.Groups[1].Value.ToPascalCase();
            return name;
        }
        var mauiEvent = EventMappings.JsToCSharpEvent.TryGetValue(eventAttr, out var ev) ? ev : eventAttr;
        return $"On{(elementId ?? "Element").ToPascalCase()}_{mauiEvent}";
    }

    private static void WriteTapGesture(
        StringBuilder sb,
        HtmlNode node,
        Dictionary<string, string> jsHandlers,
        string pad)
    {
        var id = node.GetAttr("id");
        var href = node.GetAttr("href");
        var onclick = node.GetAttr("onclick");
        var handlerName = !string.IsNullOrEmpty(onclick)
            ? DeriveHandlerName(onclick, "onclick", id)
            : $"On{(id ?? "Link").ToPascalCase()}_Tapped";

        sb.AppendLine($"{pad}<Label.GestureRecognizers>");
        sb.AppendLine($"{pad}  <TapGestureRecognizer Tapped=\"{handlerName}\"");
        if (!string.IsNullOrEmpty(href))
            sb.AppendLine($"{pad}                       CommandParameter=\"{XmlEscape(href)}\" />");
        else
            sb.AppendLine($"{pad}  />");
        sb.AppendLine($"{pad}</Label.GestureRecognizers>");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  C# code-behind generation
    // ──────────────────────────────────────────────────────────────────────────

    private void WriteCodeBehind(
        StringBuilder sb,
        Dictionary<string, string> jsHandlers,
        List<string> warnings)
    {
        sb.AppendLine($$"""
using Microsoft.Maui.Controls;

namespace {{_opts.AppNamespace}};

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

""");

        // Generate handler stubs from JS event registrations
        foreach (var (elementId, jsEventName) in jsHandlers)
        {
            if (!EventMappings.JsToCSharpEvent.TryGetValue(jsEventName, out var mauiEvent))
            {
                warnings.Add($"No C# mapping for JS event '{jsEventName}' on #{elementId} – skipped.");
                continue;
            }
            sb.AppendLine(EventMappings.GenerateHandlerStub(mauiEvent, elementId));
            sb.AppendLine();
        }

        sb.AppendLine("}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HTML parsing (lightweight, regex-based)
    // ──────────────────────────────────────────────────────────────────────────

    private static string ExtractBodyContent(string html)
    {
        var bodyMatch = BodyTagRegex().Match(html);
        return bodyMatch.Success ? bodyMatch.Groups[1].Value : html;
    }

    private static string ExtractStyleBlocks(string html)
    {
        var sb = new StringBuilder();
        foreach (Match m in StyleBlockRegex().Matches(html))
            sb.AppendLine(m.Groups[1].Value);
        return sb.ToString();
    }

    private static string ExtractScriptBlocks(string html)
    {
        var sb = new StringBuilder();
        foreach (Match m in ScriptBlockRegex().Matches(html))
            sb.AppendLine(m.Groups[1].Value);
        return sb.ToString();
    }

    internal static List<HtmlNode> ParseHtmlNodes(string html)
    {
        var tokens = TokenizeHtml(html);
        var root = new HtmlNode { TagName = "__root__" };
        var stack = new Stack<HtmlNode>();
        stack.Push(root);

        foreach (var token in tokens)
        {
            if (token.Type == HtmlTokenType.Text)
            {
                if (!string.IsNullOrWhiteSpace(token.Value))
                    stack.Peek().Children.Add(new HtmlNode { IsText = true, TextContent = token.Value });
            }
            else if (token.Type == HtmlTokenType.OpenTag)
            {
                var node = ParseTag(token.Value);
                stack.Peek().Children.Add(node);
                if (!IsSelfClosing(node.TagName))
                    stack.Push(node);
            }
            else if (token.Type == HtmlTokenType.CloseTag)
            {
                var tagName = token.Value.Trim().ToLowerInvariant();
                if (stack.Count > 1 &&
                    stack.Peek().TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                    stack.Pop();
            }
            else if (token.Type == HtmlTokenType.SelfClosing)
            {
                var node = ParseTag(token.Value);
                stack.Peek().Children.Add(node);
            }
        }

        return root.Children;
    }

    private static List<HtmlToken> TokenizeHtml(string html)
    {
        var tokens = new List<HtmlToken>();
        int pos = 0;
        while (pos < html.Length)
        {
            int lt = html.IndexOf('<', pos);
            if (lt < 0)
            {
                tokens.Add(new HtmlToken(HtmlTokenType.Text, html[pos..]));
                break;
            }
            if (lt > pos)
                tokens.Add(new HtmlToken(HtmlTokenType.Text, html[pos..lt]));

            int gt = html.IndexOf('>', lt);
            if (gt < 0) break;

            var inner = html[(lt + 1)..gt];
            if (inner.StartsWith('/'))
                tokens.Add(new HtmlToken(HtmlTokenType.CloseTag, inner[1..]));
            else if (inner.EndsWith('/') || IsSelfClosingTag(inner.Split(' ')[0].ToLowerInvariant()))
                tokens.Add(new HtmlToken(HtmlTokenType.SelfClosing, inner.TrimEnd('/')));
            else if (inner.StartsWith('!') || inner.StartsWith('?'))
                tokens.Add(new HtmlToken(HtmlTokenType.Comment, inner));
            else
                tokens.Add(new HtmlToken(HtmlTokenType.OpenTag, inner));

            pos = gt + 1;
        }
        return tokens;
    }

    private static HtmlNode ParseTag(string inner)
    {
        var node = new HtmlNode();
        var m = TagNameRegex().Match(inner);
        node.TagName = m.Success ? m.Groups[1].Value.ToLowerInvariant() : "span";

        foreach (Match am in AttrRegex().Matches(inner))
        {
            var key = am.Groups[1].Value.ToLowerInvariant();
            var val = am.Groups[2].Value.Trim('"', '\'');
            node.Attributes[key] = val;
        }

        return node;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CSS parsing
    // ──────────────────────────────────────────────────────────────────────────

    internal static Dictionary<string, Dictionary<string, string>> ParseCss(string css)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in CssRuleRegex().Matches(css))
        {
            var selectors = m.Groups[1].Value.Split(',').Select(s => s.Trim());
            var declarations = m.Groups[2].Value;
            var props = ParseInlineStyle(declarations);
            foreach (var sel in selectors)
            {
                if (!result.ContainsKey(sel))
                    result[sel] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in props)
                    result[sel][kv.Key] = kv.Value;
            }
        }
        return result;
    }

    internal static Dictionary<string, string> ParseInlineStyle(string style)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var decl in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = decl.Split(':', 2);
            if (parts.Length == 2)
                result[parts[0].Trim().ToLowerInvariant()] = parts[1].Trim();
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  JS event handler discovery
    // ──────────────────────────────────────────────────────────────────────────

    internal static Dictionary<string, string> ParseJsHandlers(string js)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Match: document.getElementById('id').addEventListener('event', handler)
        foreach (Match m in AddEventListenerRegex().Matches(js))
        {
            var elementId = m.Groups[1].Value;
            var eventName = m.Groups[2].Value;
            result[elementId] = eventName;
        }
        // Match: element.onclick = function/handler
        foreach (Match m in AssignEventRegex().Matches(js))
        {
            var elementId = m.Groups[1].Value;
            var eventName = m.Groups[2].Value.TrimStart('.');
            result[elementId] = eventName;
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static bool IsContainerElement(string tag) =>
        tag is "div" or "section" or "article" or "aside" or "header" or "footer"
            or "main" or "nav" or "ul" or "ol" or "form" or "fieldset"
            or "table" or "thead" or "tbody" or "tr";

    private static bool IsSelfClosing(string tag) =>
        tag is "input" or "img" or "br" or "hr" or "meta" or "link" or "col";

    private static bool IsSelfClosingTag(string tag) => IsSelfClosing(tag);

    private static string FormatAttrs(Dictionary<string, string> attrs)
    {
        if (attrs.Count == 0) return string.Empty;
        var parts = attrs
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $" {kv.Key}=\"{XmlEscape(kv.Value)}\"");
        return string.Concat(parts);
    }

    private static string XmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ──────────────────────────────────────────────────────────────────────────
    //  Compiled regexes
    // ──────────────────────────────────────────────────────────────────────────

    [GeneratedRegex(@"<body[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BodyTagRegex();

    [GeneratedRegex(@"<style[^>]*>(.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StyleBlockRegex();

    [GeneratedRegex(@"<script[^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptBlockRegex();

    [GeneratedRegex(@"^(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex TagNameRegex();

    [GeneratedRegex(@"([\w:\-]+)(?:=(?:""([^""]*)""|'([^']*)'|(\S+)))?")]
    private static partial Regex AttrRegex();

    [GeneratedRegex(@"([^{}]+)\{([^}]*)\}", RegexOptions.Singleline)]
    private static partial Regex CssRuleRegex();

    [GeneratedRegex(@"getElementById\(['""](\w+)['""]\)\s*\.addEventListener\(['""](\w+)['""]")]
    private static partial Regex AddEventListenerRegex();

    [GeneratedRegex(@"getElementById\(['""](\w+)['""]\)\s*(\.\w+)\s*=")]
    private static partial Regex AssignEventRegex();

    [GeneratedRegex(@"^(\w+)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionCallRegex();
}

// ──────────────────────────────────────────────────────────────────────────
//  HTML lightweight node model
// ──────────────────────────────────────────────────────────────────────────

internal class HtmlNode
{
    public string TagName { get; set; } = string.Empty;
    public bool IsText { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<HtmlNode> Children { get; } = new();

    public string GetAttr(string name, string defaultValue = "") =>
        Attributes.TryGetValue(name, out var v) ? v : defaultValue;

    public bool HasAttr(string name) => Attributes.ContainsKey(name);

    public string GetDirectText() =>
        string.Concat(Children.Where(c => c.IsText).Select(c => c.TextContent));
}

internal enum HtmlTokenType { Text, OpenTag, CloseTag, SelfClosing, Comment }

internal record HtmlToken(HtmlTokenType Type, string Value);
