using System.Data;
using BlockSearch.Database.Internal;
using Microsoft.Data.Sqlite;

namespace BlockSearch.Database;

public static class DatabaseExtensions
{
    /// <summary>
    /// Creates a SQLite table of a given name.
    /// </summary>
    public static void CreateTable(this SqliteConnection connection, string name)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"CREATE TABLE " + name + "(ID INTEGER PRIMARY KEY AUTOINCREMENT);";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Generates a table with a foreign key constraint.
    /// </summary>
    public static void CreateTableWithForeignKeys(this SqliteConnection connection, string tableName,
        params SqlForeignKeyColumn[] foreignKeys)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        string referenceColumns = string.Empty;
        string foreignKeyConstraints = string.Empty;
        int foreignKeyCount = foreignKeys.Length;
        int curForeignKey = 0;

        foreach (SqlForeignKeyColumn column in foreignKeys)
        {
            referenceColumns += column.ColumnName + " " + column.ColumnType + (column.NotNull ? " NOT NULL" : "");
            foreignKeyConstraints += "FOREIGN KEY (" + column.ColumnName + ") REFERENCES " + column.ForeignTableName 
                                     + " (" + column.ForeignColumnName + ")";

            if (curForeignKey < foreignKeyCount - 1)
            {
                referenceColumns += ", ";
                foreignKeyConstraints += ", ";
            }
            
            curForeignKey++;
        }
        
        connection.CreateTableWithForeignKey(tableName, referenceColumns + ", " + foreignKeyConstraints);
    }
    
    /// <summary>
    /// Generates a table with a foreign key constraint
    /// </summary>
    private static void CreateTableWithForeignKey(this SqliteConnection connection, string tableName,
        string foreignKeyText)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"CREATE TABLE " + tableName + "(ID INTEGER PRIMARY KEY AUTOINCREMENT," 
                              + foreignKeyText +");";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Adds a column to a given SQLite table.
    /// </summary>
    public static void AddColumnToTable(this SqliteConnection connection, string tableName, string column)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"ALTER TABLE " + tableName + " ADD COLUMN " + column + ";";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Drops the specified table.
    /// </summary>
    public static void DropTable(this SqliteConnection connection, string tableName)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"DROP TABLE " + tableName + ";";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    public static bool TableExists(this SqliteConnection connection, string tableName)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName +"';";
        SqliteDataReader reader = command.ExecuteReader();
        
        if (reader.HasRows)
        {
            reader.Close();
            return true;
        }
        
        reader.Close();
        return false;
    }


    public static void InsertData(this SqliteConnection connection, string tableName, string columns, string values)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO " + tableName + " (" + columns + ") VALUES (" + values + ")";
        command.ExecuteNonQuery();
    }


    public static int GetObjectId(this SqliteConnection connection, string tableName, string columnName,
        string condition)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new ArgumentException("Data.ConnectionState must be open");
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"SELECT " + columnName + " FROM " + tableName + " WHERE " + condition;
        SqliteDataReader reader = command.ExecuteReader();

        int id = -1;
        
        while (reader.Read())
            id = reader.GetInt32(0);
        
        reader.Close();
        
        return id;
    }
}