namespace TinyOrm.DataProvider;

public class Specification
{
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public long?[]? DesiredValues { get; set; }
    public Type[]? ExcludedTypes { get; set; }
}