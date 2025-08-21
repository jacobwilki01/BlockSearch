using DocumentFormat.OpenXml.Wordprocessing;

namespace BlockSearch.Internal.Data.Verbatim;

public class VerbatimCard(string tagline, string cite) : VerbatimObject
{
    public string Tagline { get; private set; } = tagline;
    
    public string Cite { get; private set; } = cite;
    
    public List<Paragraph> Paragraphs { get; private set; } = new();
}