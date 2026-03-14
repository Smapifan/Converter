using ConverterApp.Converters.Html2Maui;
using ConverterApp.Converters.Maui2Html;
using ConverterApp.Converters.WinForms2All;
using ConverterApp.Models;

namespace ConverterApp.Services;

/// <summary>
/// Orchestrates all conversion operations, picking the right converter based on
/// <see cref="ConversionOptions.Mode"/>.
/// </summary>
public class ConversionEngine
{
    public ConversionResult Convert(
        string primarySource,
        string? secondarySource,
        string? tertiarySource,
        ConversionOptions options)
    {
        return options.Mode switch
        {
            ConversionMode.HtmlToMaui      => HtmlToMaui(primarySource, secondarySource, tertiarySource, options),
            ConversionMode.MauiToHtml      => MauiToHtml(primarySource, secondarySource, options),
            ConversionMode.WinFormsToMaui  => WinFormsToMaui(primarySource, options),
            ConversionMode.WinFormsToHtml  => WinFormsToHtml(primarySource, options),
            ConversionMode.WinFormsToUnity => WinFormsToUnity(primarySource, options),
            ConversionMode.HtmlToUnity     => HtmlToUnity(primarySource, options),
            _ => ConversionResult.Fail($"Unsupported conversion mode: {options.Mode}"),
        };
    }

    private static ConversionResult HtmlToMaui(
        string html, string? css, string? js, ConversionOptions opts)
    {
        var converter = new HtmlToMauiConverter(opts);
        return converter.Convert(html, css, js);
    }

    private static ConversionResult MauiToHtml(string xaml, string? cs, ConversionOptions opts)
    {
        var converter = new MauiToHtmlConverter(opts);
        return converter.Convert(xaml, cs);
    }

    private static ConversionResult WinFormsToMaui(string cs, ConversionOptions opts)
    {
        var converter = new WinFormsConverter(opts);
        return converter.ToMaui(cs);
    }

    private static ConversionResult WinFormsToHtml(string cs, ConversionOptions opts)
    {
        var converter = new WinFormsConverter(opts);
        return converter.ToHtml(cs);
    }

    private static ConversionResult WinFormsToUnity(string cs, ConversionOptions opts)
    {
        var converter = new WinFormsConverter(opts);
        return converter.ToUnity(cs);
    }

    private static ConversionResult HtmlToUnity(string html, ConversionOptions opts)
    {
        // HTML → MAUI → Unity (two-step)
        var maui = new HtmlToMauiConverter(opts).Convert(html);
        if (!maui.Success) return maui;

        // The MAUI XAML now goes through WinForms→Unity path by treating it as a
        // pseudo-WinForms C# descriptor — for now we generate a stub Unity script
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("""
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Auto-generated Unity UI manager from HTML source.
/// Attach this MonoBehaviour to your Canvas GameObject.
/// </summary>
public class HtmlFormManager : MonoBehaviour
{
    private void Start()
    {
        // TODO: initialize UI controls based on generated XAML structure
    }
}
""");
        // Include the MAUI XAML as a comment for reference
        foreach (var (file, content) in maui.OutputFiles)
        {
            sb.AppendLine($"/* ---- {file} ----");
            sb.AppendLine(content);
            sb.AppendLine("*/");
        }

        return ConversionResult.Ok(
            new Dictionary<string, string> { ["HtmlFormManager.cs"] = sb.ToString() },
            maui.Messages);
    }
}
