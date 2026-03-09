namespace SnapdragonApi.DTOs;

public class OnboardingRequest
{
    // Company info
    public int? CompanyVid { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }

    // User info
    public int? UserVid { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserPhone { get; set; }
    public string? JobTitle { get; set; }
}

public class OnboardingResponse
{
    public CompanyDto Company { get; set; } = null!;
    public OfficeDto Office { get; set; } = null!;
    public UserDto User { get; set; } = null!;
}
