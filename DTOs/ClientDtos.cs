namespace SnapdragonApi.DTOs;

public class ClientDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? OfficeId { get; set; }

    // Basic Information
    public string Name { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? ExternalId { get; set; }
    public int? CategoryId { get; set; }

    // Primary Contact
    public string? PrimaryContactFirstName { get; set; }
    public string? PrimaryContactLastName { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? CellPhone { get; set; }
    public string? PrimaryFax { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? Website { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }

    // Installation Address
    public string? InstallationAddressTitle { get; set; }
    public string? InstallationAddress1 { get; set; }
    public string? InstallationAddress2 { get; set; }
    public string? InstallationCity { get; set; }
    public string? InstallationState { get; set; }
    public string? InstallationZip { get; set; }

    // Financial Settings
    public decimal? CreditLimit { get; set; }
    public string? CreditStatus { get; set; }
    public decimal? EquipmentDiscount { get; set; }
    public decimal? ServicePercent { get; set; }
    public int? SalesPricingLevel { get; set; }
    public int? RentalPricingLevel { get; set; }

    // Account Settings
    public string? AccountManager { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxId { get; set; }
    public int? TaxRegionId { get; set; }
    public bool TaxPayer { get; set; }

    // Insurance Information
    public DateTime? InsuranceExpires { get; set; }
    public string? InsuranceNumber { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public decimal? LossDamageWaiverPercent { get; set; }

    // Billing Configuration
    public int? BillingTypeId { get; set; }
    public int? BillTypeId { get; set; }
    public string? BillBy { get; set; }
    public string? InvoiceFax { get; set; }
    public string? InvoiceEmails { get; set; }
    public string? InvoiceSendBy { get; set; }
    public int? ServiceChargeTarget { get; set; }

    // Preferences and Flags
    public bool PoRequired { get; set; }
    public bool DoNotRent { get; set; }
    public bool HasLatePayHistory { get; set; }
    public bool PayCommissions { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Standard Fields
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateClientRequest
{
    public int? Vid { get; set; }
    public Guid? OfficeId { get; set; }

    // Basic Information
    public string Name { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? ExternalId { get; set; }
    public int? CategoryId { get; set; }

    // Primary Contact
    public string? PrimaryContactFirstName { get; set; }
    public string? PrimaryContactLastName { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? CellPhone { get; set; }
    public string? PrimaryFax { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? Website { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }

    // Installation Address
    public string? InstallationAddressTitle { get; set; }
    public string? InstallationAddress1 { get; set; }
    public string? InstallationAddress2 { get; set; }
    public string? InstallationCity { get; set; }
    public string? InstallationState { get; set; }
    public string? InstallationZip { get; set; }

    // Financial Settings
    public decimal? CreditLimit { get; set; }
    public string? CreditStatus { get; set; }
    public decimal? EquipmentDiscount { get; set; }
    public decimal? ServicePercent { get; set; }
    public int? SalesPricingLevel { get; set; }
    public int? RentalPricingLevel { get; set; }

    // Account Settings
    public string? AccountManager { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxId { get; set; }
    public int? TaxRegionId { get; set; }
    public bool TaxPayer { get; set; } = true;

    // Insurance Information
    public DateTime? InsuranceExpires { get; set; }
    public string? InsuranceNumber { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public decimal? LossDamageWaiverPercent { get; set; }

    // Billing Configuration
    public int? BillingTypeId { get; set; }
    public int? BillTypeId { get; set; }
    public string? BillBy { get; set; }
    public string? InvoiceFax { get; set; }
    public string? InvoiceEmails { get; set; }
    public string? InvoiceSendBy { get; set; }
    public int? ServiceChargeTarget { get; set; }

    // Preferences and Flags
    public bool PoRequired { get; set; } = false;
    public bool DoNotRent { get; set; } = false;
    public bool HasLatePayHistory { get; set; } = false;
    public bool PayCommissions { get; set; } = true;

    // Notes
    public string? Notes { get; set; }
}

public class UpdateClientRequest
{
    public int? Vid { get; set; }
    public Guid? OfficeId { get; set; }

    // Basic Information
    public string? Name { get; set; }
    public string? CustomerCode { get; set; }
    public string? ExternalId { get; set; }
    public int? CategoryId { get; set; }

    // Primary Contact
    public string? PrimaryContactFirstName { get; set; }
    public string? PrimaryContactLastName { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? CellPhone { get; set; }
    public string? PrimaryFax { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? Website { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }

    // Installation Address
    public string? InstallationAddressTitle { get; set; }
    public string? InstallationAddress1 { get; set; }
    public string? InstallationAddress2 { get; set; }
    public string? InstallationCity { get; set; }
    public string? InstallationState { get; set; }
    public string? InstallationZip { get; set; }

    // Financial Settings
    public decimal? CreditLimit { get; set; }
    public string? CreditStatus { get; set; }
    public decimal? EquipmentDiscount { get; set; }
    public decimal? ServicePercent { get; set; }
    public int? SalesPricingLevel { get; set; }
    public int? RentalPricingLevel { get; set; }

    // Account Settings
    public string? AccountManager { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxId { get; set; }
    public int? TaxRegionId { get; set; }
    public bool? TaxPayer { get; set; }

    // Insurance Information
    public DateTime? InsuranceExpires { get; set; }
    public string? InsuranceNumber { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public decimal? LossDamageWaiverPercent { get; set; }

    // Billing Configuration
    public int? BillingTypeId { get; set; }
    public int? BillTypeId { get; set; }
    public string? BillBy { get; set; }
    public string? InvoiceFax { get; set; }
    public string? InvoiceEmails { get; set; }
    public string? InvoiceSendBy { get; set; }
    public int? ServiceChargeTarget { get; set; }

    // Preferences and Flags
    public bool? PoRequired { get; set; }
    public bool? DoNotRent { get; set; }
    public bool? HasLatePayHistory { get; set; }
    public bool? PayCommissions { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Standard Fields
    public int? StatusId { get; set; }
}

public class ClientListResponse
{
    public List<ClientDto> Clients { get; set; } = new();
    public int TotalCount { get; set; }
}
