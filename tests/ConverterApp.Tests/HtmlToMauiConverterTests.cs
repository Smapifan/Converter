using ConverterApp.Converters.Html2Maui;
using ConverterApp.Models;
using Xunit;

namespace ConverterApp.Tests;

/// <summary>Tests for the HTML → MAUI converter.</summary>
public class HtmlToMauiConverterTests
{
    private readonly HtmlToMauiConverter _converter = new();

    // ── Basic element mapping ────────────────────────────────────────────────

    [Fact]
    public void Convert_Button_ProducesButtonElement()
    {
        var result = _converter.Convert("<button onclick=\"handleClick()\">Click Me</button>");

        Assert.True(result.Success);
        Assert.Contains("<Button", result.PrimaryOutput);
        Assert.Contains("Text=\"Click Me\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_InputText_ProducesEntry()
    {
        var result = _converter.Convert("<input type=\"text\" placeholder=\"Enter text\" />");

        Assert.True(result.Success);
        Assert.Contains("<Entry", result.PrimaryOutput);
        Assert.Contains("Placeholder=\"Enter text\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_InputNumber_ProducesNumericEntry()
    {
        var result = _converter.Convert("<input type=\"number\" />");

        Assert.True(result.Success);
        Assert.Contains("Keyboard=\"Numeric\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_InputPassword_ProducesPasswordEntry()
    {
        var result = _converter.Convert("<input type=\"password\" />");

        Assert.True(result.Success);
        Assert.Contains("IsPassword=\"True\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Textarea_ProducesEditor()
    {
        var result = _converter.Convert("<textarea></textarea>");

        Assert.True(result.Success);
        Assert.Contains("<Editor", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Label_ProducesLabel()
    {
        var result = _converter.Convert("<label>Hello World</label>");

        Assert.True(result.Success);
        Assert.Contains("<Label", result.PrimaryOutput);
        Assert.Contains("Hello World", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_H1_ProducesLabelWithTitleFontSize()
    {
        var result = _converter.Convert("<h1>My Title</h1>");

        Assert.True(result.Success);
        Assert.Contains("FontSize=\"Title\"", result.PrimaryOutput);
        Assert.Contains("FontAttributes=\"Bold\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Img_ProducesImage()
    {
        var result = _converter.Convert("<img src=\"photo.png\" />");

        Assert.True(result.Success);
        Assert.Contains("<Image", result.PrimaryOutput);
        Assert.Contains("Source=\"photo.png\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Select_ProducesPicker()
    {
        var result = _converter.Convert("<select id=\"myPicker\"></select>");

        Assert.True(result.Success);
        Assert.Contains("<Picker", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Checkbox_ProducesCheckBox()
    {
        var result = _converter.Convert("<input type=\"checkbox\" id=\"chk\" checked />");

        Assert.True(result.Success);
        Assert.Contains("<CheckBox", result.PrimaryOutput);
        Assert.Contains("IsChecked=\"True\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Div_ProducesStackLayout()
    {
        var result = _converter.Convert("<div id=\"container\"><label>Text</label></div>");

        Assert.True(result.Success);
        Assert.Contains("<StackLayout", result.PrimaryOutput);
        Assert.Contains("x:Name=\"container\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Form_ProducesVerticalStackLayout()
    {
        var result = _converter.Convert("<form id=\"loginForm\"></form>");

        Assert.True(result.Success);
        Assert.Contains("<VerticalStackLayout", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Paragraph_ProducesLabelWithWordWrap()
    {
        var result = _converter.Convert("<p>Some text</p>");

        Assert.True(result.Success);
        Assert.Contains("LineBreakMode=\"WordWrap\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Anchor_ProducesLabelWithTapGesture()
    {
        var result = _converter.Convert("<a href=\"https://example.com\">Click here</a>");

        Assert.True(result.Success);
        Assert.Contains("<TapGestureRecognizer", result.PrimaryOutput);
    }

    // ── CSS conversion ───────────────────────────────────────────────────────

    [Fact]
    public void Convert_InlineColorStyle_ProducesTextColor()
    {
        var result = _converter.Convert("<label style=\"color: red;\">Hello</label>");

        Assert.True(result.Success);
        Assert.Contains("TextColor=\"red\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_InlinePaddingStyle_ProducesPadding()
    {
        var result = _converter.Convert("<div style=\"padding: 16px;\"><label>x</label></div>");

        Assert.True(result.Success);
        Assert.Contains("Padding=\"16\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_InlineFontWeightBold_ProducesFontAttributesBold()
    {
        var result = _converter.Convert("<span style=\"font-weight: bold;\">Bold</span>");

        Assert.True(result.Success);
        Assert.Contains("FontAttributes=\"Bold\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_CssFileColorRule_ProducesTextColor()
    {
        const string html = "<label class=\"title\">Hello</label>";
        const string css  = ".title { color: blue; font-size: 18px; }";

        var result = _converter.Convert(html, css);

        Assert.True(result.Success);
        Assert.Contains("TextColor=\"blue\"", result.PrimaryOutput);
        Assert.Contains("FontSize=\"18\"", result.PrimaryOutput);
    }

    [Fact]
    public void StyleMappings_ConvertCssValue_StripsPxUnit()
    {
        Assert.Equal("16", StyleMappings.ConvertCssValue("16px"));
        Assert.Equal("1.5", StyleMappings.ConvertCssValue("1.5rem"));
        Assert.Equal("red", StyleMappings.ConvertCssValue("red"));
    }

    [Fact]
    public void StyleMappings_ConvertThickness_Handles4Values()
    {
        // CSS: top right bottom left → MAUI: left,top,right,bottom
        var result = StyleMappings.ConvertThickness("10px 20px 30px 40px");
        Assert.Equal("40,10,20,30", result);
    }

    [Fact]
    public void StyleMappings_ConvertThickness_Handles2Values()
    {
        var result = StyleMappings.ConvertThickness("10px 20px");
        // 2-value: top/bottom=10, left/right=20 → MAUI left,top,right,bottom = 20,10,20,10
        Assert.Equal("20,10,20,10", result);
    }

    // ── Event conversion ─────────────────────────────────────────────────────

    [Fact]
    public void Convert_OnclickAttribute_ProducesClickedHandler()
    {
        var result = _converter.Convert("<button onclick=\"handleSubmit()\">OK</button>");

        Assert.True(result.Success);
        Assert.Contains("Clicked=\"HandleSubmit\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_JsAddEventListener_ProducesHandlerInCodeBehind()
    {
        const string html = "<button id=\"submitBtn\">Submit</button>";
        const string js   = "document.getElementById('submitBtn').addEventListener('click', onSubmit);";

        var result = _converter.Convert(html, null, js);

        Assert.True(result.Success);
        // Code-behind file should contain a handler stub
        var csContent = result.OutputFiles.GetValueOrDefault("MainPage.xaml.cs", "");
        Assert.Contains("OnSubmitBtn_Clicked", csContent);
    }

    // ── Code-behind generation ───────────────────────────────────────────────

    [Fact]
    public void Convert_ProducesTwoOutputFiles()
    {
        var result = _converter.Convert("<button>OK</button>");

        Assert.True(result.Success);
        Assert.True(result.OutputFiles.ContainsKey("MainPage.xaml"));
        Assert.True(result.OutputFiles.ContainsKey("MainPage.xaml.cs"));
    }

    [Fact]
    public void Convert_XamlContainsContentPageRoot()
    {
        var result = _converter.Convert("<div></div>");

        Assert.True(result.Success);
        var xaml = result.OutputFiles["MainPage.xaml"];
        Assert.Contains("<ContentPage", xaml);
        Assert.Contains("</ContentPage>", xaml);
    }

    [Fact]
    public void Convert_EmptySource_ReturnsFailure()
    {
        var result = _converter.Convert(string.Empty);
        Assert.False(result.Success);
    }

    // ── HTML parser ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseHtmlNodes_ParsesNestedElements()
    {
        var nodes = HtmlToMauiConverter.ParseHtmlNodes(
            "<div><button>OK</button><input type=\"text\" /></div>");

        Assert.Single(nodes);
        Assert.Equal("div", nodes[0].TagName);
        Assert.Equal(2, nodes[0].Children.Count);
    }

    [Fact]
    public void ParseCss_ExtractsRules()
    {
        var rules = HtmlToMauiConverter.ParseCss(".btn { color: red; padding: 8px; }");

        Assert.True(rules.ContainsKey(".btn"));
        Assert.Equal("red",  rules[".btn"]["color"]);
        Assert.Equal("8px",  rules[".btn"]["padding"]);
    }

    [Fact]
    public void ParseJsHandlers_ExtractsAddEventListenerCalls()
    {
        var handlers = HtmlToMauiConverter.ParseJsHandlers(
            "document.getElementById('myBtn').addEventListener('click', myHandler);");

        Assert.True(handlers.ContainsKey("myBtn"));
        Assert.Equal("click", handlers["myBtn"]);
    }
}
