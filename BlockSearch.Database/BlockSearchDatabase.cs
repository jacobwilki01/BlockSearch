using BlockSearch.Database.Internal;
using BlockSearch.Internal.Data.Verbatim;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using Document = BlockSearch.Internal.Data.Document;

namespace BlockSearch.Database;

public class BlockSearchDatabase
{
    private const string PARAGRAPHS_TABLE = "Paragraphs";
    private const string CARDS_TABLE = "Cards";
    private const string DOCS_TABLE = "Documents";
    private const string POCKETS_TABLE = "Pockets";
    private const string HATS_TABLE = "Hats";
    private const string BLOCKS_TABLE = "Blocks";
    
    private SqliteConnection _connection;

    private bool _initialized = false;

    /// <summary>
    /// Builds the database and opens the connection.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        
        _connection = new SqliteConnection("DataSource=blockSearch.db");
        _connection.Open();
        
        // Foreign Keys
        SqlForeignKeyColumn documentIdKey = new("DocumentID", "INTEGER", DOCS_TABLE, "ID", true);
        SqlForeignKeyColumn pocketIdKey = new("PocketID", "INTEGER", POCKETS_TABLE, "ID", false);
        SqlForeignKeyColumn hatIdKey = new("HatID", "INTEGER", HATS_TABLE, "ID", false);
        SqlForeignKeyColumn blockIdKey = new("BlockID", "INTEGER", DOCS_TABLE, "ID", false);
        SqlForeignKeyColumn cardIdKey = new("CardID", "INTEGER", CARDS_TABLE, "ID", false);
        
        // Generate Documents Table
        if (!_connection.TableExists(DOCS_TABLE))
        {
            _connection.CreateTable(DOCS_TABLE);
            _connection.AddColumnToTable(DOCS_TABLE, "FileName VARCHAR(100) NOT NULL");
            _connection.AddColumnToTable(DOCS_TABLE, "FilePath VARCHAR(1000) NOT NULL");
            _connection.AddColumnToTable(DOCS_TABLE, "LastModified DATE NOT NULL");
        }
        
        // Generate Pockets Table
        if (!_connection.TableExists(POCKETS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(POCKETS_TABLE, documentIdKey);
            _connection.AddColumnToTable(POCKETS_TABLE, "PocketText VARCHAR(100) NOT NULL");
        }
        
        // Generate Hats Table
        if (!_connection.TableExists(HATS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(HATS_TABLE, documentIdKey, pocketIdKey);
            _connection.AddColumnToTable(HATS_TABLE, "HatText VARCHAR(100) NOT NULL");
        }
        
        // Generate Blocks Table
        if (!_connection.TableExists(BLOCKS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(BLOCKS_TABLE, documentIdKey, pocketIdKey, hatIdKey);
            _connection.AddColumnToTable(BLOCKS_TABLE, "BlockText VARCHAR(100) NOT NULL");
        }
        
        // Generate Cards Table
        if (!_connection.TableExists(CARDS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(CARDS_TABLE, documentIdKey, pocketIdKey, hatIdKey, blockIdKey);
            _connection.AddColumnToTable(CARDS_TABLE, "Tag VARCHAR(1024) NOT NULL");
            _connection.AddColumnToTable(CARDS_TABLE, "Author VARCHAR(100) NOT NULL");
        }
        
        // Generate Paragraphs Table
        if (!_connection.TableExists(PARAGRAPHS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(PARAGRAPHS_TABLE, documentIdKey, cardIdKey);
            _connection.AddColumnToTable(PARAGRAPHS_TABLE, "Paragraph TEXT NOT NULL");
        }
        
        _initialized = true;
    }

    /// <summary>
    /// Disposes of and closes the database connection.
    /// </summary>
    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    /// <summary>
    /// Drops all tables and sets the initialized flag to false.
    /// </summary>
    public void ResetDatabase()
    {
        _connection.DropTable(PARAGRAPHS_TABLE);
        _connection.DropTable(CARDS_TABLE);
        _connection.DropTable(BLOCKS_TABLE);
        _connection.DropTable(HATS_TABLE);
        _connection.DropTable(POCKETS_TABLE);
        _connection.DropTable(DOCS_TABLE);
        
        _initialized = false;
    }


    public void ProcessDocuments(ref List<Document> documents)
    {
        for (int i = 0; i < documents.Count; i++)
        {
            Document document = documents[i];
            
            if (document.Name.Contains("\""))
                throw new Exception("Invalid file name containing double quote: \"");
                
            _connection.InsertData(DOCS_TABLE, "FileName, FilePath, LastModified",
                $"\"{document.Name}\", \"{document.FilePath}\", " +
                $"\"{document.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\"");
            document.DatabaseId =
                _connection.GetObjectId(DOCS_TABLE, "ID", $"FileName = \"{document.Name}\"");
                
            List<VerbatimObject> objects = document.Objects;
            ProcessDocumentObjectTree(ref objects, document.DatabaseId);
                
            Console.WriteLine($"Inserted {document.FilePath} in database.");
            
            // try
            // {
            //     if (document.Name.Contains("\""))
            //         throw new Exception("Invalid file name containing double quote: \"");
            //     
            //     _connection.InsertData(DOCS_TABLE, "FileName, FilePath, LastModified",
            //         $"\"{document.Name}\", \"{document.FilePath}\", " +
            //         $"\"{document.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\"");
            //     document.DatabaseId =
            //         _connection.GetObjectId(DOCS_TABLE, "ID", $"FileName = \"{document.Name}\"");
            //     
            //     List<VerbatimObject> objects = document.Objects;
            //     ProcessDocumentObjectTree(ref objects, document.DatabaseId);
            //     
            //     Console.WriteLine($"Inserted {document.FilePath} in database.");
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"An unexpected error occurred when inserting document into database: {ex.Message}");
            // }
        }
    }

    public void ProcessDocumentObjectTree(ref List<VerbatimObject> objects, int documentId)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            VerbatimObject documentObject = objects[i];

            if (documentObject is VerbatimHeader documentHeader)
            {
                switch (documentHeader.Type)
                {
                    case VerbatimHeaderType.Pocket:
                        InsertPocket(ref documentHeader, documentId);
                        break;
                    case VerbatimHeaderType.Hat:
                        InsertHat(ref documentHeader, documentId);
                        break;
                    case VerbatimHeaderType.Block:
                        InsertBlock(ref documentHeader, documentId);
                        break;
                }
            }
            else if (documentObject is VerbatimCard documentCard)
            {
                InsertCard(ref documentCard, documentId);
            }
        }
    }

    private void InsertPocket(ref VerbatimHeader pocket, int documentId)
    {
        _connection.InsertData(POCKETS_TABLE, "DocumentID, PocketText", $"{documentId}, '{pocket.Text}'");
        pocket.DatabaseId = _connection.GetObjectId(POCKETS_TABLE, "ID", $"PocketText = '{pocket.Text}'");

        List<VerbatimObject> objects = pocket.Children;
        ProcessDocumentObjectTree(ref objects, documentId);
    }

    private void InsertHat(ref VerbatimHeader hat, int documentId)
    {
        string columns = "DocumentID, HatText";
        string values = $"{documentId}, '{hat.Text}'";
        
        VerbatimObject? currentParent = hat.Parent;
        if (currentParent != null)
        {
            columns += ", PocketID";
            values += $", {currentParent.DatabaseId}";
        }
        
        _connection.InsertData(HATS_TABLE, columns, values);
        hat.DatabaseId = _connection.GetObjectId(HATS_TABLE, "ID", $"HatText = '{hat.Text}'");
        
        List<VerbatimObject> objects = hat.Children;
        ProcessDocumentObjectTree(ref objects, documentId);
    }

    private void InsertBlock(ref VerbatimHeader block, int documentId)
    {
        string columns = "DocumentID, BlockText";
        string values = $"{documentId}, '{block.Text}'";
        
        VerbatimObject? currentParent = block.Parent;
        while (currentParent != null)
        {
            if (currentParent is VerbatimHeader parent && parent.Type == VerbatimHeaderType.Pocket)
            {
                columns += ", PocketID";
                values += $", {currentParent.DatabaseId}";
                currentParent = parent.Parent;
            }
            else
            {
                columns += ", HatID";
                values += $", {currentParent.DatabaseId}";
                currentParent = currentParent.Parent;
            }
        }
        
        _connection.InsertData(BLOCKS_TABLE, columns, values);
        block.DatabaseId = _connection.GetObjectId(BLOCKS_TABLE, "ID", $"BlockText = '{block.Text}'");
        
        List<VerbatimObject> objects = block.Children;
        ProcessDocumentObjectTree(ref objects, documentId);
    }

    private void InsertCard(ref VerbatimCard card, int documentId)
    {
        string columns = "DocumentID, Tag, Author";
        string values = $"{documentId}, '{card.Tagline}', '{card.Cite}'";
        
        VerbatimObject? currentParent = card.Parent;
        while (currentParent != null)
        {
            if (currentParent is VerbatimHeader pocket && pocket.Type == VerbatimHeaderType.Pocket)
            {
                columns += ", PocketID";
                values += $", {currentParent.DatabaseId}";
                currentParent = currentParent.Parent;
            }
            else if (currentParent is VerbatimHeader hat && hat.Type == VerbatimHeaderType.Hat)
            {
                columns += ", HatID";
                values += $", {currentParent.DatabaseId}";
                currentParent = currentParent.Parent;
            }
            else
            {
                columns += ", BlockID";
                values += $", {currentParent.DatabaseId}";
                currentParent = currentParent.Parent;
            }
        }
        
        _connection.InsertData(CARDS_TABLE, columns, values);
        card.DatabaseId = _connection.GetObjectId(CARDS_TABLE, "ID", $"Tag = '{card.Tagline}'");

        foreach (Paragraph paragraph in card.Paragraphs)
            InsertParagraph(paragraph, documentId, card.DatabaseId);
    }

    private void InsertParagraph(Paragraph paragraph, int documentId, int cardId)
    {
        _connection.InsertData(PARAGRAPHS_TABLE, "DocumentID, CardID, Paragraph", $"{documentId}, {cardId}, '{paragraph.InnerXml}'");
    }
}