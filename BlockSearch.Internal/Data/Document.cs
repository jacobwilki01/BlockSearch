using BlockSearch.Internal.Data.Verbatim;

namespace BlockSearch.Internal.Data;

public class Document
{
    public string Name { get; private set; } = string.Empty;
    
    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            Name = Path.GetFileName(value);
        }
    }   
    
    public List<VerbatimObject> Objects { get; private set; } = new();
    
    public DateTime LastWriteTime { get; set; }

    public int DatabaseId { get; set; } = -1;
}