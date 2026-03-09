namespace SnapdragonApi.DTOs;

public class BigQueryRequest
{
    public string Sql { get; set; } = string.Empty;
    public List<BigQueryParameterDto>? Parameters { get; set; }
    public bool? UseCache { get; set; }
}

public class BigQueryParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "STRING";
    public object? Value { get; set; }
}

public class BigQueryResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public ulong? TotalRows { get; set; }
}
