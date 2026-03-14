using ConverterApp.Converters.Maui2Html;
using ConverterApp.Models;
using Xunit;

namespace ConverterApp.Tests;

/// <summary>Tests for the MAUI → HTML converter.</summary>
public class MauiToHtmlConverterTests
{
    private readonly MauiToHtmlConverter _converter = new();

    [Fact]
    public void Convert_Button_ProducesButtonElement()
    {
        var result = _converter.Convert(
            "<ContentPage><VerticalStackLayout><Button Text=\"OK\" /></VerticalStackLayout></ContentPage>");

        Assert.True(result.Success);
        Assert.Contains("<button", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Entry_ProducesInputText()
    {
        var result = _converter.Convert(
            "<ContentPage><VerticalStackLayout><Entry Placeholder=\"Enter text\" /></VerticalStackLayout></ContentPage>");

        Assert.True(result.Success);
        Assert.Contains("<input", result.PrimaryOutput);
        Assert.Contains("type=\"text\"", result.PrimaryOutput);
        Assert.Contains("placeholder=\"Enter text\"", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_Label_ProducesSpan()
    {
        var result = _converter.Convert(
            "<ContentPage><VerticalStackLayout><Label Text=\"Hello\" /></VerticalStackLayout></ContentPage>");

        Assert.True(result.Success);
        Assert.Contains("<span", result.PrimaryOutput);
    }

    [Fact]
    public void Convert_ProducesThreeOutputFiles()
    {
        var result = _converter.Convert("<ContentPage><VerticalStackLayout /></ContentPage>");

        Assert.True(result.Success);
        Assert.True(result.OutputFiles.ContainsKey("index.html"));
        Assert.True(result.OutputFiles.ContainsKey("styles.css"));
        Assert.True(result.OutputFiles.ContainsKey("app.js"));
    }

    [Fact]
    public void Convert_XamlWithTextColor_ProducesCssColor()
    {
        var result = _converter.Convert(
            "<ContentPage><Label x:Name=\"lbl\" TextColor=\"red\" Text=\"Hello\" /></ContentPage>");

        Assert.True(result.Success);
        var css = result.OutputFiles["styles.css"];
        Assert.Contains("color: red", css);
    }

    [Fact]
    public void Convert_EmptySource_ReturnsFailure()
    {
        var result = _converter.Convert(string.Empty);
        Assert.False(result.Success);
    }

    [Fact]
    public void Convert_HtmlContainsDoctype()
    {
        var result = _converter.Convert("<ContentPage /></ContentPage>");
        Assert.True(result.Success);
        var html = result.OutputFiles["index.html"];
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [Fact]
    public void ParseXamlNodes_ParsesAttributes()
    {
        var nodes = MauiToHtmlConverter.ParseXamlNodes(
            "<ContentPage><Label Text=\"Hi\" TextColor=\"Blue\" /></ContentPage>");

        Assert.Single(nodes);  // ContentPage
        var label = nodes[0].Children[0];
        Assert.Equal("Label", label.TagName);
        Assert.Equal("Hi",   label.GetAttr("Text"));
        Assert.Equal("Blue", label.GetAttr("TextColor"));
    }
}
