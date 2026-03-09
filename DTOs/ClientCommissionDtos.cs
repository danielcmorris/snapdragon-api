namespace SnapdragonApi.DTOs;

public class ClientCommissionDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public int SortOrder { get; set; }
}

public class ClientCommissionListResponse
{
    public List<ClientCommissionDto> Items { get; set; } = new();
}

public class UpdateClientCommissionRequest
{
    public decimal Rate { get; set; }
}

public class BulkUpdateClientCommissionsRequest
{
    public decimal Rate { get; set; }
}
