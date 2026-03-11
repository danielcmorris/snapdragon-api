namespace SnapdragonApi.DTOs;

public class ClientSetup
{
    public string? Name { get; set; }
    public string? PrimaryContactFirstName { get; set; }
    public string? PrimaryContactLastName { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? CellPhone { get; set; }
    public string? AccountManager { get; set; }
    public string? InstallationAddressTitle { get; set; }
    public string? InstallationAddress1 { get; set; }
    public string? InstallationAddress2 { get; set; }
    public string? InstallationCity { get; set; }
    public string? InstallationState { get; set; }
    public string? InstallationZip { get; set; }
    public string? Notes { get; set; }
}

public class InteractiveClientSetupRequest
{
    public string UserText { get; set; } = string.Empty;
    public ClientSetup? ExistingSetup { get; set; }
    public string? PreviousQuestion { get; set; }
}

public class InteractiveClientSetupResponse
{
    public ClientSetup ClientSetup { get; set; } = new();
    public double CompletenessPercentage { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public string? SuggestedNextQuestion { get; set; }
}

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
