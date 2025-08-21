using BlockSearch.Internal.Data.Verbatim;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Document = BlockSearch.Internal.Data.Document;

namespace BlockSearch.Internal.Util;

public static class FileProcessor
{
    private const string DocxType = "*.docx";
    private const string Pocket = "Heading1";
    private const string Hat = "Heading2";
    private const string Block = "Heading3";
    private const string Tag = "Heading4";
    private const string Cite = "Style13ptBold";
    
    public static List<Document> ProcessDirectory(string directory)
    {
        List<Document> documents = new();
        
        foreach (string filePath in Directory.EnumerateFiles(directory, DocxType, SearchOption.AllDirectories))
        {
            Console.WriteLine($"Processing file: {filePath}");

            try
            {
                Document document = new() { FilePath = filePath };
                ProcessDocument(ref document);
                documents.Add(document);
                
                Console.WriteLine($"Processed file: {filePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access denied to a directory. {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Error: Directory not found. {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }
        
        return documents;
    }
    
    public static void ProcessDocument(ref Document document)
    {
        FileInfo fileInfo = new(document.FilePath);
        if (fileInfo.Length <= 0)
            return;
        
        document.LastWriteTime = fileInfo.LastWriteTime;
        
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(document.FilePath, false))
        {
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart == null)
                return;
            
            List<Paragraph> paragraphs = mainPart.Document.Descendants<Paragraph>().ToList();
            VerbatimHeader? currentHeader = null;
            int nextParagraphIndex = 0;
            
            foreach (Paragraph paragraph in paragraphs)
            {
                if (paragraphs.IndexOf(paragraph) != nextParagraphIndex)
                    continue;
                
                VerbatimHeaderType? paragraphType = GetParagraphType(paragraph);
                
                // Initial Phase
                if (currentHeader == null && paragraphType != null && paragraphType != VerbatimHeaderType.Tag)
                {
                    VerbatimHeader header = ProcessHeader(paragraph, paragraphType.Value);
                    
                    document.Objects.Add(header);
                    currentHeader = header;
                }
                // Cards
                else if (paragraphType != null && paragraphType == VerbatimHeaderType.Tag)
                {
                    VerbatimCard? card = ProcessCard(paragraph, paragraphs, nextParagraphIndex, out nextParagraphIndex);

                    if (card == null)
                    {
                        nextParagraphIndex++;
                        continue;
                    }
                    
                    card.Parent = currentHeader;
                    if (currentHeader != null)
                        currentHeader.Children.Add(card);
                    else
                        document.Objects.Add(card);
                }
                // Child headers
                else if (currentHeader != null && paragraphType != null && paragraphType > currentHeader.Type)
                {
                    VerbatimHeader header = ProcessHeader(paragraph, paragraphType.Value);
                    
                    currentHeader.Children.Add(header);
                    header.Parent = currentHeader;
                    currentHeader = header;
                }
                // Sibling headers
                else if (currentHeader != null && paragraphType != null && paragraphType == currentHeader.Type)
                {
                    VerbatimHeader header = ProcessHeader(paragraph, paragraphType.Value);
                    
                    if (currentHeader.Parent != null)
                        currentHeader.Parent.Children.Add(header);
                    else
                        document.Objects.Add(header);
                    header.Parent = currentHeader.Parent;
                    currentHeader = header;
                }
                // Next branch header
                else if (currentHeader != null && paragraphType != null && paragraphType < currentHeader.Type)
                {
                    VerbatimHeader header = ProcessHeader(paragraph, paragraphType.Value);

                    VerbatimObject? grandparent = currentHeader.Parent?.Parent;
                    if (grandparent != null)
                        grandparent.Children.Add(header);
                    else
                        document.Objects.Add(header);
                    
                    header.Parent = grandparent;
                    currentHeader = header;
                }
                
                // Clean up any mistakes
                while (nextParagraphIndex <= paragraphs.IndexOf(paragraph))
                    nextParagraphIndex++;
            }
        }
    }

    private static VerbatimHeader ProcessHeader(Paragraph paragraph, VerbatimHeaderType paragraphType)
    {
        VerbatimHeader header = new VerbatimHeader(paragraphType, paragraph);
                    
        List<Run> runs = paragraph.Descendants<Run>().ToList();
        if (runs.Any())
        {
            Run run = runs.First();
            header.Text = run.InnerText;
        }
        
        return header;
    }

    private static VerbatimCard? ProcessCard(Paragraph paragraph, List<Paragraph> paragraphs, int currentParagraphIndex, out int nextParagraphIndex)
    {
        // Get the tagline.
        string tagline = string.Empty;
        List<Run> runs = paragraph.Descendants<Run>().ToList();
        foreach (Run run in runs)
            tagline += run.InnerText;
        
        int nextIndex = currentParagraphIndex + 1;
        if (nextIndex >= paragraphs.Count)
        {
            nextParagraphIndex = currentParagraphIndex;
            return null;
        }
        
        Paragraph nextParagraph = paragraphs[currentParagraphIndex + 1];
        
        // Reject analytics
        if (IsTag(nextParagraph))
        {
            nextParagraphIndex = currentParagraphIndex;
            return null;
        }
        
        // Get the author
        // REQUIRED: Author must be a Verbatim "cite".
        string author = string.Empty;
        List<Run> nextRuns = nextParagraph.Descendants<Run>().ToList();
        foreach (Run run in nextParagraph.Descendants<Run>())
        {
            RunProperties? runProperties = run.RunProperties;
            if (runProperties is null)
                continue;

            object? styleProperty = runProperties.FirstOrDefault();
            if (styleProperty is not null && styleProperty is RunStyle runStyleId && runStyleId.Val!.ToString()!.Equals(Cite))
                author += run.InnerText;
        }
        
        // Reject failed author fetches.
        if (author.Equals(string.Empty))
        {
            nextParagraphIndex = currentParagraphIndex;
            return null;
        }
        
        VerbatimCard card = new(tagline, author);
        card.Paragraphs.Add(paragraph);
        card.Paragraphs.Add(nextParagraph);
        
        nextParagraphIndex = currentParagraphIndex + 2;
        while (nextParagraphIndex < paragraphs.Count && !IsHeader(paragraphs[nextParagraphIndex]))
        {
            card.Paragraphs.Add(paragraphs[nextParagraphIndex]);
            nextParagraphIndex++;
        }

        return card;
    }

    public static VerbatimHeaderType? GetParagraphType(Paragraph paragraph)
    {
        ParagraphProperties? properties = paragraph.ParagraphProperties;
        if (properties is null)
            return null;
        
        object? property = properties.FirstOrDefault();
        if (property is not null && property is ParagraphStyleId paraStyleId)
        {
            if (paraStyleId.Val!.ToString()!.Equals(Pocket))
                return VerbatimHeaderType.Pocket;
            if (paraStyleId.Val!.ToString()!.Equals(Hat))
                return VerbatimHeaderType.Hat;
            if (paraStyleId.Val!.ToString()!.Equals(Block))
                return VerbatimHeaderType.Block;
            if (paraStyleId.Val!.ToString()!.Equals(Tag))
                return VerbatimHeaderType.Tag;
        }

        return null;
    }
    
    private static bool IsHeader(Paragraph paragraph)
    {
        ParagraphProperties? properties = paragraph.ParagraphProperties;
        if (properties is null)
            return false;
    
        object? property = properties.FirstOrDefault();
        if (property is not null && property is ParagraphStyleId paraStyleId)
        {
            return paraStyleId.Val!.ToString()!.Equals(Pocket) ||
                   paraStyleId.Val!.ToString()!.Equals(Hat) ||
                   paraStyleId.Val!.ToString()!.Equals(Block) ||
                   paraStyleId.Val!.ToString()!.Equals(Tag);
        }
    
        return false;
    }
    
    private static bool IsTag(Paragraph paragraph)
    {
        ParagraphProperties? properties = paragraph.ParagraphProperties;
        return properties is not null && properties.FirstOrDefault() is ParagraphStyleId paraStyleId && paraStyleId.Val!.ToString()!.Equals(Tag);
    }
}