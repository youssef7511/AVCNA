using System.Net;
using Markdig;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Convertit la monographie Markdown d'un médicament en document HTML stylé,
/// destiné à être rendu dans la fenêtre d'aperçu (WebView2).
///
/// Le rendu reproduit l'esthétique du client Medwin destiné au médecin :
/// titre en vert, rubriques (H2) en bannière rouge, sous-titres (H3) en gras
/// sombre, paragraphes en gris foncé sur fond blanc.
/// </summary>
public static class MonographieHtmlRenderer
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()  // tables, lists, emphasis, etc.
            .UseAutoLinks()
            .Build();

    public static string Render(string markdown, string medicName)
    {
        var bodyHtml = string.IsNullOrWhiteSpace(markdown)
            ? "<p class=\"empty\"><em>Aucune monographie saisie pour ce médicament.</em></p>"
            : Markdown.ToHtml(markdown, Pipeline);

        var safeTitle = WebUtility.HtmlEncode(medicName ?? string.Empty);

        return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
<meta charset=""utf-8"">
<title>Monographie — {safeTitle}</title>
<style>
  * {{ box-sizing: border-box; }}
  body {{
    font-family: 'Segoe UI', Arial, sans-serif;
    max-width: 820px;
    margin: 0 auto;
    padding: 28px 32px 48px;
    color: #1a1a1a;
    background: #ffffff;
    line-height: 1.55;
    font-size: 14px;
  }}
  h1 {{
    color: #1B5E20;
    border-bottom: 2px solid #1B5E20;
    padding-bottom: 8px;
    margin-top: 0;
    font-size: 22px;
    font-weight: 700;
    letter-spacing: 0.02em;
  }}
  h2 {{
    background: #C62828;
    color: #ffffff;
    padding: 8px 14px;
    margin: 24px 0 12px;
    font-size: 14px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    border-radius: 2px;
  }}
  h3 {{
    color: #424242;
    font-size: 14px;
    margin: 18px 0 6px;
    font-weight: 700;
  }}
  p {{
    margin: 8px 0 12px;
    text-align: justify;
  }}
  ul, ol {{
    margin: 6px 0 12px 8px;
    padding-left: 22px;
  }}
  li {{
    margin: 3px 0;
  }}
  strong, b {{ color: #1B5E20; }}
  em, i {{ color: #5D4037; }}
  blockquote {{
    border-left: 4px solid #C62828;
    background: #FFEBEE;
    margin: 12px 0;
    padding: 8px 14px;
    color: #424242;
    font-style: italic;
  }}
  table {{
    border-collapse: collapse;
    width: 100%;
    margin: 12px 0;
    font-size: 13px;
  }}
  table th, table td {{
    border: 1px solid #cccccc;
    padding: 6px 10px;
    text-align: left;
  }}
  table th {{ background: #f5f5f5; font-weight: 700; }}
  hr {{
    border: 0;
    border-top: 1px solid #e0e0e0;
    margin: 24px 0;
  }}
  .empty {{ color: #9e9e9e; }}
  .footer {{
    margin-top: 36px;
    padding-top: 12px;
    border-top: 1px solid #e0e0e0;
    font-size: 11px;
    color: #757575;
    text-align: center;
  }}
</style>
</head>
<body>
<h1>{safeTitle}</h1>
{bodyHtml}
<div class=""footer"">Aperçu généré par AVCNDB — Rendu destiné au client Medwin (médecin).</div>
</body>
</html>";
    }
}
