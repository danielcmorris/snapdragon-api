using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("client")]
public class Client
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    [Column("office_id")]
    public Guid? OfficeId { get; set; }

    [ForeignKey(nameof(OfficeId))]
    public Office? Office { get; set; }

    // Basic Information
    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("customer_code")]
    public string? CustomerCode { get; set; }

    [MaxLength(100)]
    [Column("external_id")]
    public string? ExternalId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    // Primary Contact
    [MaxLength(100)]
    [Column("primary_contact_first_name")]
    public string? PrimaryContactFirstName { get; set; }

    [MaxLength(100)]
    [Column("primary_contact_last_name")]
    public string? PrimaryContactLastName { get; set; }

    [MaxLength(50)]
    [Column("primary_phone")]
    public string? PrimaryPhone { get; set; }

    [MaxLength(50)]
    [Column("cell_phone")]
    public string? CellPhone { get; set; }

    [MaxLength(50)]
    [Column("primary_fax")]
    public string? PrimaryFax { get; set; }

    [MaxLength(255)]
    [Column("primary_email")]
    public string? PrimaryEmail { get; set; }

    [MaxLength(255)]
    [Column("website")]
    public string? Website { get; set; }

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

    // Installation Address
    [MaxLength(100)]
    [Column("installation_address_title")]
    public string? InstallationAddressTitle { get; set; }

    [MaxLength(255)]
    [Column("installation_address1")]
    public string? InstallationAddress1 { get; set; }

    [MaxLength(255)]
    [Column("installation_address2")]
    public string? InstallationAddress2 { get; set; }

    [MaxLength(100)]
    [Column("installation_city")]
    public string? InstallationCity { get; set; }

    [MaxLength(50)]
    [Column("installation_state")]
    public string? InstallationState { get; set; }

    [MaxLength(20)]
    [Column("installation_zip")]
    public string? InstallationZip { get; set; }

    // Financial Settings
    [Column("credit_limit", TypeName = "decimal(18,2)")]
    public decimal? CreditLimit { get; set; }

    [MaxLength(50)]
    [Column("credit_status")]
    public string? CreditStatus { get; set; }

    [Column("equipment_discount", TypeName = "decimal(5,2)")]
    public decimal? EquipmentDiscount { get; set; }

    [Column("service_percent", TypeName = "decimal(5,2)")]
    public decimal? ServicePercent { get; set; }

    [Column("sales_pricing_level")]
    public int? SalesPricingLevel { get; set; }

    [Column("rental_pricing_level")]
    public int? RentalPricingLevel { get; set; }

    // Account Settings
    [MaxLength(100)]
    [Column("account_manager")]
    public string? AccountManager { get; set; }

    [MaxLength(100)]
    [Column("payment_terms")]
    public string? PaymentTerms { get; set; }

    [MaxLength(50)]
    [Column("tax_id")]
    public string? TaxId { get; set; }

    [Column("tax_region_id")]
    public int? TaxRegionId { get; set; }

    [Column("tax_payer")]
    public bool TaxPayer { get; set; } = true;

    // Insurance Information
    [Column("insurance_expires")]
    public DateTime? InsuranceExpires { get; set; }

    [MaxLength(100)]
    [Column("insurance_number")]
    public string? InsuranceNumber { get; set; }

    [Column("insurance_amount", TypeName = "decimal(18,2)")]
    public decimal? InsuranceAmount { get; set; }

    [Column("loss_damage_waiver_percent", TypeName = "decimal(5,2)")]
    public decimal? LossDamageWaiverPercent { get; set; }

    // Billing Configuration
    [Column("billing_type_id")]
    public int? BillingTypeId { get; set; }

    [Column("bill_type_id")]
    public int? BillTypeId { get; set; }

    [MaxLength(50)]
    [Column("bill_by")]
    public string? BillBy { get; set; }

    [MaxLength(50)]
    [Column("invoice_fax")]
    public string? InvoiceFax { get; set; }

    [MaxLength(500)]
    [Column("invoice_emails")]
    public string? InvoiceEmails { get; set; }

    [MaxLength(50)]
    [Column("invoice_send_by")]
    public string? InvoiceSendBy { get; set; }

    [Column("service_charge_target")]
    public int? ServiceChargeTarget { get; set; }

    // Preferences and Flags
    [Column("po_required")]
    public bool PoRequired { get; set; } = false;

    [Column("do_not_rent")]
    public bool DoNotRent { get; set; } = false;

    [Column("has_late_pay_history")]
    public bool HasLatePayHistory { get; set; } = false;

    [Column("pay_commissions")]
    public bool PayCommissions { get; set; } = true;

    // Notes
    [Column("notes")]
    public string? Notes { get; set; }

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
