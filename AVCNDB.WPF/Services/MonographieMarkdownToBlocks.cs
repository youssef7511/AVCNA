using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Parse une monographie Markdown en une séquence de blocs simples,
/// directement consommables par QuestPDF pour le rendu PDF.
///
/// Le sous-ensemble Markdown couvert est volontairement restreint :
/// titres H2/H3, paragraphes, listes à puces, listes numérotées.
/// Le formatage inline (gras, italique) est aplati en texte simple.
/// </summary>
public static class MonographieMarkdownToBlocks
{
    public enum BlockKind
    {
        Paragraph,
        H2,
        H3,
        ListItem,
        OrderedItem
    }

    public record Block(BlockKind Kind, string Text);

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static IEnumerable<Block> Parse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) yield break;

        var doc = Markdown.Parse(markdown, Pipeline);

        foreach (var node in doc)
        {
            switch (node)
            {
                case HeadingBlock h when h.Level == 2:
                    yield return new Block(BlockKind.H2, InlineText(h.Inline));
                    break;
                case HeadingBlock h when h.Level == 3:
                    yield return new Block(BlockKind.H3, InlineText(h.Inline));
                    break;
                case HeadingBlock h:
                    // H1 / H4+ rendered as bold paragraph
                    yield return new Block(BlockKind.H3, InlineText(h.Inline));
                    break;

                case ParagraphBlock p:
                    yield return new Block(BlockKind.Paragraph, InlineText(p.Inline));
                    break;

                case ListBlock list:
                    var ordered = list.IsOrdered;
                    foreach (var li in list)
                    {
                        if (li is ListItemBlock item)
                        {
                            foreach (var sub in item)
                            {
                                if (sub is ParagraphBlock subPara)
                                {
                                    yield return new Block(
                                        ordered ? BlockKind.OrderedItem : BlockKind.ListItem,
                                        InlineText(subPara.Inline));
                                }
                            }
                        }
                    }
                    break;

                case ThematicBreakBlock:
                    // Rendered as an empty paragraph (visual separator)
                    yield return new Block(BlockKind.Paragraph, string.Empty);
                    break;
            }
        }
    }

    private static string InlineText(ContainerInline? container)
    {
        if (container == null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content.ToString());
                    break;
                case LineBreakInline:
                    sb.Append(' ');
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case EmphasisInline em when em.FirstChild is ContainerInline child:
                    sb.Append(InlineText(child));
                    break;
                case LinkInline link:
                    sb.Append(InlineText(link));
                    break;
                case ContainerInline child:
                    sb.Append(InlineText(child));
                    break;
            }
        }
        return sb.ToString().Trim();
    }
}
