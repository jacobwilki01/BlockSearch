namespace BlockSearch.Database.Internal;

public class SqlForeignKeyColumn(
    string columnName,
    string columnType,
    string foreignTableName,
    string foreignColumnName,
    bool notNull)
{
    public string ColumnName { get; set; } = columnName;
    public string ColumnType { get; set; } = columnType;
    public string ForeignTableName { get; set; } = foreignTableName;
    public string ForeignColumnName { get; set; } = foreignColumnName;
    public bool NotNull { get; set; } = notNull;
}