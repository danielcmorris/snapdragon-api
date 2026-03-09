namespace SnapdragonApi.DTOs;

public class EventSetup
{
    public string? ClientName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientCompany { get; set; }
    public string? EventName { get; set; }
    public string? EventType { get; set; }
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public int? EstimatedAttendees { get; set; }
    public DateTime? EventDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? SetupTime { get; set; }
    public string? TeardownTime { get; set; }
    public bool? IndoorOutdoor { get; set; }
    public string? BudgetRange { get; set; }
    public List<string>? ServicesRequested { get; set; }
    public string? SpecialRequirements { get; set; }
    public string? Notes { get; set; }
}

public class InteractiveSetupRequest
{
    public string UserText { get; set; } = string.Empty;
    public EventSetup? ExistingSetup { get; set; }
    public string? PreviousQuestion { get; set; }
}

public class InteractiveSetupResponse
{
    public EventSetup EventSetup { get; set; } = new();
    public double CompletenessPercentage { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public string? SuggestedNextQuestion { get; set; }
}
