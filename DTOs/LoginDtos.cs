namespace SnapdragonApi.DTOs;

public class LoginResponse
{
    public bool Found { get; set; }
    public string Status { get; set; } = string.Empty;
    public CompanyDto? Company { get; set; }
    public OfficeDto? Office { get; set; }
    public UserDto? User { get; set; }
    public UserSessionContext? Session { get; set; }
}
