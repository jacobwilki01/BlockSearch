using DocumentFormat.OpenXml.Wordprocessing;

namespace BlockSearch.Internal.Data.Verbatim;

public class VerbatimHeader(VerbatimHeaderType type, Paragraph paragraph) : VerbatimObject
{
    public VerbatimHeaderType Type { get; private set; } = type;
    
    public string Text { get; set; } = string.Empty;
    
    public Paragraph Paragraph { get; set; } = paragraph;
}