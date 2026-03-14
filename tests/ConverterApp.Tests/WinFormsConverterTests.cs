using ConverterApp.Converters.WinForms2All;
using ConverterApp.Models;
using Xunit;

namespace ConverterApp.Tests;

/// <summary>Tests for the WinForms → * converters.</summary>
public class WinFormsConverterTests
{
    private readonly WinFormsConverter _converter = new();

    private const string SampleWinForms = """
        namespace MyApp
        {
            partial class Form1
            {
                private System.Windows.Forms.Button btnOk;
                private System.Windows.Forms.TextBox txtName;
                private System.Windows.Forms.Label lblName;

                private void InitializeComponent()
                {
                    this.Text = "My Form";
                    this.ClientSize = new System.Drawing.Size(400, 300);

                    this.lblName.Text = "Name:";
                    this.lblName.Location = new System.Drawing.Point(16, 20);
                    this.lblName.Size = new System.Drawing.Size(80, 24);

                    this.txtName.Text = "";
                    this.txtName.Location = new System.Drawing.Point(100, 20);
                    this.txtName.Size = new System.Drawing.Size(280, 30);

                    this.btnOk.Text = "OK";
                    this.btnOk.Location = new System.Drawing.Point(16, 60);
                    this.btnOk.Size = new System.Drawing.Size(364, 40);

                    this.btnOk.Click += new System.EventHandler(btnOk_Click);
                }
            }
        }
        """;

    // ── WinForms → HTML ──────────────────────────────────────────────────────

    [Fact]
    public void ToHtml_ProducesIndexHtml()
    {
        var result = _converter.ToHtml(SampleWinForms);
        Assert.True(result.Success);
        Assert.True(result.OutputFiles.ContainsKey("index.html"));
    }

    [Fact]
    public void ToHtml_Button_ProducesButtonElement()
    {
        var result = _converter.ToHtml(SampleWinForms);
        Assert.True(result.Success);
        var html = result.OutputFiles["index.html"];
        Assert.Contains("<button", html);
        Assert.Contains("OK", html);
    }

    [Fact]
    public void ToHtml_TextBox_ProducesInputElement()
    {
        var result = _converter.ToHtml(SampleWinForms);
        Assert.True(result.Success);
        var html = result.OutputFiles["index.html"];
        Assert.Contains("<input", html);
    }

    [Fact]
    public void ToHtml_ProducesStylesCss()
    {
        var result = _converter.ToHtml(SampleWinForms);
        Assert.True(result.Success);
        var css = result.OutputFiles["styles.css"];
        Assert.Contains("position: absolute", css);
    }

    [Fact]
    public void ToHtml_EventHandlers_ProducedInJs()
    {
        var result = _converter.ToHtml(SampleWinForms);
        Assert.True(result.Success);
        var js = result.OutputFiles["app.js"];
        Assert.Contains("onclick", js);
    }

    // ── WinForms → MAUI ──────────────────────────────────────────────────────

    [Fact]
    public void ToMaui_ProducesXaml()
    {
        var result = _converter.ToMaui(SampleWinForms);
        Assert.True(result.Success);
        Assert.True(result.OutputFiles.ContainsKey("MainPage.xaml"));
    }

    [Fact]
    public void ToMaui_Button_ProducesButtonElement()
    {
        var result = _converter.ToMaui(SampleWinForms);
        Assert.True(result.Success);
        var xaml = result.OutputFiles["MainPage.xaml"];
        Assert.Contains("<Button", xaml);
        Assert.Contains("Text=\"OK\"", xaml);
    }

    [Fact]
    public void ToMaui_TextBox_ProducesEntry()
    {
        var result = _converter.ToMaui(SampleWinForms);
        Assert.True(result.Success);
        var xaml = result.OutputFiles["MainPage.xaml"];
        Assert.Contains("<Entry", xaml);
    }

    [Fact]
    public void ToMaui_ProducesCodeBehindWithEventHandler()
    {
        var result = _converter.ToMaui(SampleWinForms);
        Assert.True(result.Success);
        var cs = result.OutputFiles["MainPage.xaml.cs"];
        Assert.Contains("OnBtnOk_Clicked", cs);
    }

    // ── WinForms → Unity ─────────────────────────────────────────────────────

    [Fact]
    public void ToUnity_ProducesCSharpMonoBehaviour()
    {
        var result = _converter.ToUnity(SampleWinForms);
        Assert.True(result.Success);
        Assert.True(result.OutputFiles.ContainsKey("FormManager.cs"));
    }

    [Fact]
    public void ToUnity_ContainsMonoBehaviourClass()
    {
        var result = _converter.ToUnity(SampleWinForms);
        Assert.True(result.Success);
        var cs = result.OutputFiles["FormManager.cs"];
        Assert.Contains("MonoBehaviour", cs);
    }

    [Fact]
    public void ToUnity_Button_MapsToUnityButtonField()
    {
        var result = _converter.ToUnity(SampleWinForms);
        Assert.True(result.Success);
        var cs = result.OutputFiles["FormManager.cs"];
        Assert.Contains("Button", cs);
    }

    // ── Empty input ──────────────────────────────────────────────────────────

    [Fact]
    public void ToHtml_EmptyInput_ReturnsFailure()
    {
        var result = _converter.ToHtml(string.Empty);
        Assert.False(result.Success);
    }

    [Fact]
    public void ToMaui_EmptyInput_ReturnsFailure()
    {
        var result = _converter.ToMaui(string.Empty);
        Assert.False(result.Success);
    }
}
