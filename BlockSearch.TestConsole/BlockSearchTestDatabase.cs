using Microsoft.Data.Sqlite;

namespace BlockSearchTestConsole;

public class BlockSearchTestDatabase
{
    private SqliteConnection _connection;

    public BlockSearchTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=testDatbase.db");
    }

    public void InitializeDatabase()
    {
        _connection.Open();
        
        bool tablesCreated = false;

        if (!tablesCreated)
        {
            SqliteCommand command = _connection.CreateCommand();
            command.CommandText +=
                "CREATE TABLE Cards ( CardID INTEGER PRIMARY KEY AUTOINCREMENT, Tag VARCHAR(100), Author VARCHAR(100), FullText TEXT\n);";
            command.ExecuteNonQuery();
        }

        SqliteCommand describe = _connection.CreateCommand();
        describe.CommandText = "DESCRIBE Cards";

        using (SqliteDataReader reader = describe.ExecuteReader())
        {
            Console.WriteLine(reader.GetSchemaTable());
        }
    }
}