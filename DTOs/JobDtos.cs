namespace SnapdragonApi.DTOs;

public class JobDto
{
    public Guid Id { get; set; }
    public int? JobNumber { get; set; }
    public int? Vid { get; set; }
    public int? VisibleOrderId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? ClientId { get; set; }

    // Basic Information
    public DateTime? OrderDate { get; set; }
    public int? OrderStatus { get; set; }
    public string? ClientName { get; set; }
    public string? OrganizationName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? AccountNumber { get; set; }

    // Installation/Delivery Contact
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? ContactInfo { get; set; }

    // Installation/Delivery Address
    public string? AddressTitle { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }

    // Billing Contact
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingPhone { get; set; }
    public string? BillingFax { get; set; }
    public string? BillingEmail { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }
    public bool UseDirectBillAddress { get; set; }

    // Job Schedule
    public DateTime? BeginDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? BeginTime { get; set; }
    public string? EndTime { get; set; }

    // Action Schedule (5 configurable action types)
    public string? Action1Label { get; set; }
    public DateTime? Action1Date { get; set; }
    public string? Action1Time { get; set; }
    public string? Action2Label { get; set; }
    public DateTime? Action2Date { get; set; }
    public string? Action2Time { get; set; }
    public string? Action3Label { get; set; }
    public DateTime? Action3Date { get; set; }
    public string? Action3Time { get; set; }
    public string? Action4Label { get; set; }
    public DateTime? Action4Date { get; set; }
    public string? Action4Time { get; set; }
    public string? Action5Label { get; set; }
    public DateTime? Action5Date { get; set; }
    public string? Action5Time { get; set; }

    // Financial Information
    public string? PaymentTerms { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? DeliveryCost { get; set; }
    public decimal? Discount { get; set; }
    public decimal? JobDiscount { get; set; }
    public decimal? JobDiscountPercent { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? SalesTaxRate { get; set; }
    public decimal? LaborTaxRate { get; set; }
    public decimal? ServicePercent { get; set; }
    public decimal? ServiceChargeCommission { get; set; }
    public decimal? CommissionBase { get; set; }
    public decimal? PaymentApplied { get; set; }
    public decimal? TotalWeight { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? InvoiceId { get; set; }
    public int? BillingDays { get; set; }
    public bool CreditMemo { get; set; }

    // Configuration IDs
    public int? RateId { get; set; }
    public int? TaxId { get; set; }
    public int? BillTypeId { get; set; }
    public int? PackStatusId { get; set; }
    public int? LaborRateId { get; set; }
    public int? CustomerId { get; set; }

    // Loss Damage Waiver
    public decimal? LossDamageWaiverPercent { get; set; }
    public bool LossDamageWaiverActive { get; set; }

    // Notes and Documents
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? BanquetEventOrder { get; set; }

    // Metadata
    public string? SalesLead { get; set; }
    public DateTime? LastPrinted { get; set; }
    public int? TemporaryId { get; set; }
    public Guid? CopiedFromOrderId { get; set; }

    // Standard Fields
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateJobRequest
{
    public int? Vid { get; set; }
    public int? VisibleOrderId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? ClientId { get; set; }

    // Basic Information
    public DateTime? OrderDate { get; set; }
    public int? OrderStatus { get; set; }
    public string? ClientName { get; set; }
    public string? OrganizationName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? AccountNumber { get; set; }

    // Installation/Delivery Contact
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? ContactInfo { get; set; }

    // Installation/Delivery Address
    public string? AddressTitle { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }

    // Billing Contact
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingPhone { get; set; }
    public string? BillingFax { get; set; }
    public string? BillingEmail { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }
    public bool UseDirectBillAddress { get; set; } = false;

    // Job Schedule
    public DateTime? BeginDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? BeginTime { get; set; }
    public string? EndTime { get; set; }

    // Action Schedule (5 configurable action types)
    public string? Action1Label { get; set; }
    public DateTime? Action1Date { get; set; }
    public string? Action1Time { get; set; }
    public string? Action2Label { get; set; }
    public DateTime? Action2Date { get; set; }
    public string? Action2Time { get; set; }
    public string? Action3Label { get; set; }
    public DateTime? Action3Date { get; set; }
    public string? Action3Time { get; set; }
    public string? Action4Label { get; set; }
    public DateTime? Action4Date { get; set; }
    public string? Action4Time { get; set; }
    public string? Action5Label { get; set; }
    public DateTime? Action5Date { get; set; }
    public string? Action5Time { get; set; }

    // Financial Information
    public string? PaymentTerms { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? DeliveryCost { get; set; }
    public decimal? Discount { get; set; }
    public decimal? JobDiscount { get; set; }
    public decimal? JobDiscountPercent { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? SalesTaxRate { get; set; }
    public decimal? LaborTaxRate { get; set; }
    public decimal? ServicePercent { get; set; }
    public decimal? ServiceChargeCommission { get; set; }
    public decimal? CommissionBase { get; set; }
    public decimal? PaymentApplied { get; set; }
    public decimal? TotalWeight { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? InvoiceId { get; set; }
    public int? BillingDays { get; set; }
    public bool CreditMemo { get; set; } = false;

    // Configuration IDs
    public int? RateId { get; set; }
    public int? TaxId { get; set; }
    public int? BillTypeId { get; set; }
    public int? PackStatusId { get; set; }
    public int? LaborRateId { get; set; }
    public int? CustomerId { get; set; }

    // Loss Damage Waiver
    public decimal? LossDamageWaiverPercent { get; set; }
    public bool LossDamageWaiverActive { get; set; } = false;

    // Notes and Documents
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? BanquetEventOrder { get; set; }

    // Metadata
    public string? SalesLead { get; set; }
    public DateTime? LastPrinted { get; set; }
    public int? TemporaryId { get; set; }
    public Guid? CopiedFromOrderId { get; set; }
}

public class UpdateJobRequest
{
    public int? JobNumber { get; set; }
    public int? Vid { get; set; }
    public int? VisibleOrderId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? ClientId { get; set; }

    // Basic Information
    public DateTime? OrderDate { get; set; }
    public int? OrderStatus { get; set; }
    public string? ClientName { get; set; }
    public string? OrganizationName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? AccountNumber { get; set; }

    // Installation/Delivery Contact
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? ContactInfo { get; set; }

    // Installation/Delivery Address
    public string? AddressTitle { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }

    // Billing Contact
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingPhone { get; set; }
    public string? BillingFax { get; set; }
    public string? BillingEmail { get; set; }

    // Billing Address
    public string? BillingAddressTitle { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZip { get; set; }
    public bool? UseDirectBillAddress { get; set; }

    // Job Schedule
    public DateTime? BeginDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? BeginTime { get; set; }
    public string? EndTime { get; set; }

    // Action Schedule (5 configurable action types)
    public string? Action1Label { get; set; }
    public DateTime? Action1Date { get; set; }
    public string? Action1Time { get; set; }
    public string? Action2Label { get; set; }
    public DateTime? Action2Date { get; set; }
    public string? Action2Time { get; set; }
    public string? Action3Label { get; set; }
    public DateTime? Action3Date { get; set; }
    public string? Action3Time { get; set; }
    public string? Action4Label { get; set; }
    public DateTime? Action4Date { get; set; }
    public string? Action4Time { get; set; }
    public string? Action5Label { get; set; }
    public DateTime? Action5Date { get; set; }
    public string? Action5Time { get; set; }

    // Financial Information
    public string? PaymentTerms { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? DeliveryCost { get; set; }
    public decimal? Discount { get; set; }
    public decimal? JobDiscount { get; set; }
    public decimal? JobDiscountPercent { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? SalesTaxRate { get; set; }
    public decimal? LaborTaxRate { get; set; }
    public decimal? ServicePercent { get; set; }
    public decimal? ServiceChargeCommission { get; set; }
    public decimal? CommissionBase { get; set; }
    public decimal? PaymentApplied { get; set; }
    public decimal? TotalWeight { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? InvoiceId { get; set; }
    public int? BillingDays { get; set; }
    public bool? CreditMemo { get; set; }

    // Configuration IDs
    public int? RateId { get; set; }
    public int? TaxId { get; set; }
    public int? BillTypeId { get; set; }
    public int? PackStatusId { get; set; }
    public int? LaborRateId { get; set; }
    public int? CustomerId { get; set; }

    // Loss Damage Waiver
    public decimal? LossDamageWaiverPercent { get; set; }
    public bool? LossDamageWaiverActive { get; set; }

    // Notes and Documents
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? BanquetEventOrder { get; set; }

    // Metadata
    public string? SalesLead { get; set; }
    public DateTime? LastPrinted { get; set; }
    public int? TemporaryId { get; set; }
    public Guid? CopiedFromOrderId { get; set; }

    // Standard Fields
    public int? StatusId { get; set; }
}

public class JobListResponse
{
    public List<JobDto> Jobs { get; set; } = new();
    public int TotalCount { get; set; }
}
