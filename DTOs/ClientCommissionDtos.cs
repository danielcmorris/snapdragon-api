namespace SnapdragonApi.DTOs;

public class ClientCommissionDto
{
    public int CommissionTypeId { get; set; }
    public string Name { get; set; } = "";
    public decimal DefaultPercent { get; set; }
    public decimal Rate { get; set; }
}

public class UpdateClientCommissionRequest
{
    public decimal Rate { get; set; }
}

public class BulkUpdateClientCommissionsRequest
{
    public decimal Rate { get; set; }
}
