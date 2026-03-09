using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("job")]
public class Job
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("job_number")]
    public int? JobNumber { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Column("visible_order_id")]
    public int? VisibleOrderId { get; set; }

    [Required]
    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    [Column("office_id")]
    public Guid? OfficeId { get; set; }

    [ForeignKey(nameof(OfficeId))]
    public Office? Office { get; set; }

    [Column("client_id")]
    public Guid? ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }

    // Basic Information
    [Column("order_date")]
    public DateTime? OrderDate { get; set; }

    [Column("order_status")]
    public int? OrderStatus { get; set; }

    [MaxLength(255)]
    [Column("client_name")]
    public string? ClientName { get; set; }

    [MaxLength(255)]
    [Column("organization_name")]
    public string? OrganizationName { get; set; }

    [MaxLength(100)]
    [Column("purchase_order_number")]
    public string? PurchaseOrderNumber { get; set; }

    [MaxLength(100)]
    [Column("account_number")]
    public string? AccountNumber { get; set; }

    // Installation/Delivery Contact
    [MaxLength(100)]
    [Column("first_name")]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    [Column("last_name")]
    public string? LastName { get; set; }

    [MaxLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(50)]
    [Column("fax")]
    public string? Fax { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(500)]
    [Column("contact_info")]
    public string? ContactInfo { get; set; }

    // Installation/Delivery Address
    [MaxLength(100)]
    [Column("address_title")]
    public string? AddressTitle { get; set; }

    [MaxLength(255)]
    [Column("address1")]
    public string? Address1 { get; set; }

    [MaxLength(255)]
    [Column("address2")]
    public string? Address2 { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(50)]
    [Column("state")]
    public string? State { get; set; }

    [MaxLength(20)]
    [Column("zip")]
    public string? Zip { get; set; }

    // Billing Contact
    [MaxLength(100)]
    [Column("billing_first_name")]
    public string? BillingFirstName { get; set; }

    [MaxLength(100)]
    [Column("billing_last_name")]
    public string? BillingLastName { get; set; }

    [MaxLength(50)]
    [Column("billing_phone")]
    public string? BillingPhone { get; set; }

    [MaxLength(50)]
    [Column("billing_fax")]
    public string? BillingFax { get; set; }

    [MaxLength(255)]
    [Column("billing_email")]
    public string? BillingEmail { get; set; }

    // Billing Address
    [MaxLength(100)]
    [Column("billing_address_title")]
    public string? BillingAddressTitle { get; set; }

    [MaxLength(255)]
    [Column("billing_address1")]
    public string? BillingAddress1 { get; set; }

    [MaxLength(255)]
    [Column("billing_address2")]
    public string? BillingAddress2 { get; set; }

    [MaxLength(100)]
    [Column("billing_city")]
    public string? BillingCity { get; set; }

    [MaxLength(50)]
    [Column("billing_state")]
    public string? BillingState { get; set; }

    [MaxLength(20)]
    [Column("billing_zip")]
    public string? BillingZip { get; set; }

    [Column("use_direct_bill_address")]
    public bool UseDirectBillAddress { get; set; } = false;

    // Job Schedule
    [Column("begin_date")]
    public DateTime? BeginDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    [Column("begin_time")]
    public string? BeginTime { get; set; }

    [MaxLength(50)]
    [Column("end_time")]
    public string? EndTime { get; set; }

    // Action Schedule (5 configurable action types)
    [MaxLength(100)]
    [Column("action1_label")]
    public string? Action1Label { get; set; }

    [Column("action1_date")]
    public DateTime? Action1Date { get; set; }

    [MaxLength(50)]
    [Column("action1_time")]
    public string? Action1Time { get; set; }

    [MaxLength(100)]
    [Column("action2_label")]
    public string? Action2Label { get; set; }

    [Column("action2_date")]
    public DateTime? Action2Date { get; set; }

    [MaxLength(50)]
    [Column("action2_time")]
    public string? Action2Time { get; set; }

    [MaxLength(100)]
    [Column("action3_label")]
    public string? Action3Label { get; set; }

    [Column("action3_date")]
    public DateTime? Action3Date { get; set; }

    [MaxLength(50)]
    [Column("action3_time")]
    public string? Action3Time { get; set; }

    [MaxLength(100)]
    [Column("action4_label")]
    public string? Action4Label { get; set; }

    [Column("action4_date")]
    public DateTime? Action4Date { get; set; }

    [MaxLength(50)]
    [Column("action4_time")]
    public string? Action4Time { get; set; }

    [MaxLength(100)]
    [Column("action5_label")]
    public string? Action5Label { get; set; }

    [Column("action5_date")]
    public DateTime? Action5Date { get; set; }

    [MaxLength(50)]
    [Column("action5_time")]
    public string? Action5Time { get; set; }

    // Financial Information
    [MaxLength(100)]
    [Column("payment_terms")]
    public string? PaymentTerms { get; set; }

    [Column("shipping_cost", TypeName = "decimal(18,2)")]
    public decimal? ShippingCost { get; set; }

    [Column("delivery_cost", TypeName = "decimal(18,2)")]
    public decimal? DeliveryCost { get; set; }

    [Column("discount", TypeName = "decimal(18,2)")]
    public decimal? Discount { get; set; }

    [Column("job_discount", TypeName = "decimal(18,2)")]
    public decimal? JobDiscount { get; set; }

    [Column("job_discount_percent", TypeName = "decimal(5,2)")]
    public decimal? JobDiscountPercent { get; set; }

    [Column("tax_rate", TypeName = "decimal(5,2)")]
    public decimal? TaxRate { get; set; }

    [Column("sales_tax_rate", TypeName = "decimal(5,2)")]
    public decimal? SalesTaxRate { get; set; }

    [Column("labor_tax_rate", TypeName = "decimal(5,2)")]
    public decimal? LaborTaxRate { get; set; }

    [Column("service_percent", TypeName = "decimal(5,2)")]
    public decimal? ServicePercent { get; set; }

    [Column("service_charge_commission", TypeName = "decimal(18,2)")]
    public decimal? ServiceChargeCommission { get; set; }

    [Column("commission_base", TypeName = "decimal(18,2)")]
    public decimal? CommissionBase { get; set; }

    [Column("payment_applied", TypeName = "decimal(18,2)")]
    public decimal? PaymentApplied { get; set; }

    [Column("total_weight", TypeName = "decimal(10,2)")]
    public decimal? TotalWeight { get; set; }

    [Column("invoice_date")]
    public DateTime? InvoiceDate { get; set; }

    [Column("invoice_id")]
    public int? InvoiceId { get; set; }

    [Column("billing_days")]
    public int? BillingDays { get; set; }

    [Column("credit_memo")]
    public bool CreditMemo { get; set; } = false;

    // Configuration IDs
    [Column("rate_id")]
    public int? RateId { get; set; }

    [Column("tax_id")]
    public int? TaxId { get; set; }

    [Column("bill_type_id")]
    public int? BillTypeId { get; set; }

    [Column("pack_status_id")]
    public int? PackStatusId { get; set; }

    [Column("labor_rate_id")]
    public int? LaborRateId { get; set; }

    [Column("customer_id")]
    public int? CustomerId { get; set; }

    // Loss Damage Waiver
    [Column("loss_damage_waiver_percent", TypeName = "decimal(5,2)")]
    public decimal? LossDamageWaiverPercent { get; set; }

    [Column("loss_damage_waiver_active")]
    public bool LossDamageWaiverActive { get; set; } = false;

    // Notes and Documents
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("internal_notes")]
    public string? InternalNotes { get; set; }

    [Column("banquet_event_order")]
    public string? BanquetEventOrder { get; set; }

    // Metadata
    [MaxLength(100)]
    [Column("sales_lead")]
    public string? SalesLead { get; set; }

    [Column("last_printed")]
    public DateTime? LastPrinted { get; set; }

    [Column("temporary_id")]
    public int? TemporaryId { get; set; }

    [Column("copied_from_order_id")]
    public Guid? CopiedFromOrderId { get; set; }

    // Standard Fields
    [Column("status_id")]
    public int StatusId { get; set; } = 1;

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("updated_by_id")]
    public Guid? UpdatedById { get; set; }

    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
