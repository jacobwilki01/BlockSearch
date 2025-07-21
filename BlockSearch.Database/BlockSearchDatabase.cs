using BlockSearch.Database.Internal;
using Microsoft.Data.Sqlite;

namespace BlockSearch.Database;

public class BlockSearchDatabase
{
    private const string CARDS_TABLE = "Cards";
    private const string DOCS_TABLE = "Documents";
    private const string POCKETS_TABLE = "Pockets";
    private const string HATS_TABLE = "Hats";
    private const string BLOCKS_TABLE = "Blocks";
    
    private SqliteConnection _connection;

    private bool _initialized = false;

    public void Initialize()
    {
        if (_initialized) return;
        
        _connection = new SqliteConnection("DataSource=blockSearch.db");
        _connection.Open();
        
        // Foreign Keys
        SqlForeignKeyColumn DocumentIdKey = new("DocumentID", "INTEGER", DOCS_TABLE, "ID", true);
        SqlForeignKeyColumn PocketIdKey = new("PocketID", "INTEGER", POCKETS_TABLE, "ID", false);
        SqlForeignKeyColumn HatIdKey = new("HatID", "INTEGER", HATS_TABLE, "ID", false);
        SqlForeignKeyColumn BlockIdKey = new("BlockID", "INTEGER", DOCS_TABLE, "ID", false);
        
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
            _connection.CreateTableWithForeignKeys(POCKETS_TABLE, DocumentIdKey);
            _connection.AddColumnToTable(POCKETS_TABLE, "PocketText VARCHAR(100) NOT NULL");
        }
        
        // Generate Hats Table
        if (!_connection.TableExists(HATS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(HATS_TABLE, DocumentIdKey, PocketIdKey);
            _connection.AddColumnToTable(HATS_TABLE, "HatText VARCHAR(100) NOT NULL");
        }
        
        // Generate Blocks Table
        if (!_connection.TableExists(BLOCKS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(BLOCKS_TABLE, DocumentIdKey, PocketIdKey, HatIdKey);
            _connection.AddColumnToTable(BLOCKS_TABLE, "BlockText VARCHAR(100) NOT NULL");
        }
        
        // Generate Cards Table
        if (!_connection.TableExists(CARDS_TABLE))
        {
            _connection.CreateTableWithForeignKeys(CARDS_TABLE, DocumentIdKey, PocketIdKey, HatIdKey, BlockIdKey);
            _connection.AddColumnToTable(CARDS_TABLE, "Tag VARCHAR(255) NOT NULL");
            _connection.AddColumnToTable(CARDS_TABLE, "Author VARCHAR(100) NOT NULL");
            _connection.AddColumnToTable(CARDS_TABLE, "FullText TEXT");
        }
        
        _initialized = true;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}