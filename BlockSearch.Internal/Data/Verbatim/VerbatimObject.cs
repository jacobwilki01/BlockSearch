using DocumentFormat.OpenXml.Wordprocessing;

namespace BlockSearch.Internal.Data.Verbatim;

public abstract class VerbatimObject
{
    public List<Paragraph> Paragraphs { get; private set; } = new();
    
    public VerbatimObject? Parent { get; set; }
    
    public List<VerbatimObject> Children { get; private set; } = new();

    public int DatabaseId { get; set; } = -1;
}