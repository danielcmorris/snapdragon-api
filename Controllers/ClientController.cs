using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientController> _logger;
    private readonly IUserSessionService _session;

    public ClientController(AppDbContext db, ILogger<ClientController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<ClientListResponse>> GetClients(
        [FromQuery] int? statusId = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var query = _db.Client.Where(c => c.CompanyId == ctx.CompanyId);

        if (statusId.HasValue)
            query = query.Where(c => c.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) ||
                (c.CustomerCode != null && c.CustomerCode.Contains(search)) ||
                (c.PrimaryEmail != null && c.PrimaryEmail.Contains(search)));

        var totalCount = await query.CountAsync();
        var clients = await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(new ClientListResponse { Clients = clients, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetClient(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var client = await _db.Client.FindAsync(id);
        if (client == null || client.CompanyId != ctx.CompanyId)
            return NotFound();

        return Ok(ToDto(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Client name is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        // Verify office belongs to company if specified
        if (request.OfficeId.HasValue)
        {
            var office = await _db.Office.FindAsync(request.OfficeId.Value);
            if (office == null || office.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid office selection" });
        }

        var now = DateTime.UtcNow;
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            CompanyId = ctx.CompanyId,
            OfficeId = request.OfficeId,
            Name = request.Name,
            CustomerCode = request.CustomerCode,
            ExternalId = request.ExternalId,
            CategoryId = request.CategoryId,
            PrimaryContactFirstName = request.PrimaryContactFirstName,
            PrimaryContactLastName = request.PrimaryContactLastName,
            PrimaryPhone = request.PrimaryPhone,
            CellPhone = request.CellPhone,
            PrimaryFax = request.PrimaryFax,
            PrimaryEmail = request.PrimaryEmail,
            Website = request.Website,
            BillingAddressTitle = request.BillingAddressTitle,
            BillingAddress1 = request.BillingAddress1,
            BillingAddress2 = request.BillingAddress2,
            BillingCity = request.BillingCity,
            BillingState = request.BillingState,
            BillingZip = request.BillingZip,
            InstallationAddressTitle = request.InstallationAddressTitle,
            InstallationAddress1 = request.InstallationAddress1,
            InstallationAddress2 = request.InstallationAddress2,
            InstallationCity = request.InstallationCity,
            InstallationState = request.InstallationState,
            InstallationZip = request.InstallationZip,
            CreditLimit = request.CreditLimit,
            CreditStatus = request.CreditStatus,
            EquipmentDiscount = request.EquipmentDiscount,
            ServicePercent = request.ServicePercent,
            SalesPricingLevel = request.SalesPricingLevel,
            RentalPricingLevel = request.RentalPricingLevel,
            AccountManager = request.AccountManager,
            PaymentTerms = request.PaymentTerms,
            TaxId = request.TaxId,
            TaxRegionId = request.TaxRegionId,
            TaxPayer = request.TaxPayer,
            InsuranceExpires = request.InsuranceExpires,
            InsuranceNumber = request.InsuranceNumber,
            InsuranceAmount = request.InsuranceAmount,
            LossDamageWaiverPercent = request.LossDamageWaiverPercent,
            BillingTypeId = request.BillingTypeId,
            BillTypeId = request.BillTypeId,
            BillBy = request.BillBy,
            InvoiceFax = request.InvoiceFax,
            InvoiceEmails = request.InvoiceEmails,
            InvoiceSendBy = request.InvoiceSendBy,
            ServiceChargeTarget = request.ServiceChargeTarget,
            PoRequired = request.PoRequired,
            DoNotRent = request.DoNotRent,
            HasLatePayHistory = request.HasLatePayHistory,
            PayCommissions = request.PayCommissions,
            Notes = request.Notes,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Client.Add(client);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created client {ClientId} - {ClientName}", client.Id, client.Name);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, ToDto(client));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientDto>> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var client = await _db.Client.FindAsync(id);
        if (client == null || client.CompanyId != ctx.CompanyId)
            return NotFound();

        // Verify office belongs to company if being changed
        if (request.OfficeId.HasValue)
        {
            var office = await _db.Office.FindAsync(request.OfficeId.Value);
            if (office == null || office.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid office selection" });
            client.OfficeId = request.OfficeId;
        }

        if (request.Vid.HasValue) client.Vid = request.Vid;
        if (request.Name != null) client.Name = request.Name;
        if (request.CustomerCode != null) client.CustomerCode = request.CustomerCode;
        if (request.ExternalId != null) client.ExternalId = request.ExternalId;
        if (request.CategoryId.HasValue) client.CategoryId = request.CategoryId;
        if (request.PrimaryContactFirstName != null) client.PrimaryContactFirstName = request.PrimaryContactFirstName;
        if (request.PrimaryContactLastName != null) client.PrimaryContactLastName = request.PrimaryContactLastName;
        if (request.PrimaryPhone != null) client.PrimaryPhone = request.PrimaryPhone;
        if (request.CellPhone != null) client.CellPhone = request.CellPhone;
        if (request.PrimaryFax != null) client.PrimaryFax = request.PrimaryFax;
        if (request.PrimaryEmail != null) client.PrimaryEmail = request.PrimaryEmail;
        if (request.Website != null) client.Website = request.Website;
        if (request.BillingAddressTitle != null) client.BillingAddressTitle = request.BillingAddressTitle;
        if (request.BillingAddress1 != null) client.BillingAddress1 = request.BillingAddress1;
        if (request.BillingAddress2 != null) client.BillingAddress2 = request.BillingAddress2;
        if (request.BillingCity != null) client.BillingCity = request.BillingCity;
        if (request.BillingState != null) client.BillingState = request.BillingState;
        if (request.BillingZip != null) client.BillingZip = request.BillingZip;
        if (request.InstallationAddressTitle != null) client.InstallationAddressTitle = request.InstallationAddressTitle;
        if (request.InstallationAddress1 != null) client.InstallationAddress1 = request.InstallationAddress1;
        if (request.InstallationAddress2 != null) client.InstallationAddress2 = request.InstallationAddress2;
        if (request.InstallationCity != null) client.InstallationCity = request.InstallationCity;
        if (request.InstallationState != null) client.InstallationState = request.InstallationState;
        if (request.InstallationZip != null) client.InstallationZip = request.InstallationZip;
        if (request.CreditLimit.HasValue) client.CreditLimit = request.CreditLimit;
        if (request.CreditStatus != null) client.CreditStatus = request.CreditStatus;
        if (request.EquipmentDiscount.HasValue) client.EquipmentDiscount = request.EquipmentDiscount;
        if (request.ServicePercent.HasValue) client.ServicePercent = request.ServicePercent;
        if (request.SalesPricingLevel.HasValue) client.SalesPricingLevel = request.SalesPricingLevel;
        if (request.RentalPricingLevel.HasValue) client.RentalPricingLevel = request.RentalPricingLevel;
        if (request.AccountManager != null) client.AccountManager = request.AccountManager;
        if (request.PaymentTerms != null) client.PaymentTerms = request.PaymentTerms;
        if (request.TaxId != null) client.TaxId = request.TaxId;
        if (request.TaxRegionId.HasValue) client.TaxRegionId = request.TaxRegionId;
        if (request.TaxPayer.HasValue) client.TaxPayer = request.TaxPayer.Value;
        if (request.InsuranceExpires.HasValue) client.InsuranceExpires = request.InsuranceExpires;
        if (request.InsuranceNumber != null) client.InsuranceNumber = request.InsuranceNumber;
        if (request.InsuranceAmount.HasValue) client.InsuranceAmount = request.InsuranceAmount;
        if (request.LossDamageWaiverPercent.HasValue) client.LossDamageWaiverPercent = request.LossDamageWaiverPercent;
        if (request.BillingTypeId.HasValue) client.BillingTypeId = request.BillingTypeId;
        if (request.BillTypeId.HasValue) client.BillTypeId = request.BillTypeId;
        if (request.BillBy != null) client.BillBy = request.BillBy;
        if (request.InvoiceFax != null) client.InvoiceFax = request.InvoiceFax;
        if (request.InvoiceEmails != null) client.InvoiceEmails = request.InvoiceEmails;
        if (request.InvoiceSendBy != null) client.InvoiceSendBy = request.InvoiceSendBy;
        if (request.ServiceChargeTarget.HasValue) client.ServiceChargeTarget = request.ServiceChargeTarget;
        if (request.PoRequired.HasValue) client.PoRequired = request.PoRequired.Value;
        if (request.DoNotRent.HasValue) client.DoNotRent = request.DoNotRent.Value;
        if (request.HasLatePayHistory.HasValue) client.HasLatePayHistory = request.HasLatePayHistory.Value;
        if (request.PayCommissions.HasValue) client.PayCommissions = request.PayCommissions.Value;
        if (request.Notes != null) client.Notes = request.Notes;
        if (request.StatusId.HasValue) client.StatusId = request.StatusId.Value;

        client.UpdatedById = ctx.UserId;
        client.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated client {ClientId}", client.Id);

        return Ok(ToDto(client));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var client = await _db.Client.FindAsync(id);
        if (client == null || client.CompanyId != ctx.CompanyId)
            return NotFound();

        _db.Client.Remove(client);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted client {ClientId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static ClientDto ToDto(Client c) => new()
    {
        Id = c.Id,
        Vid = c.Vid,
        CompanyId = c.CompanyId,
        OfficeId = c.OfficeId,
        Name = c.Name,
        CustomerCode = c.CustomerCode,
        ExternalId = c.ExternalId,
        CategoryId = c.CategoryId,
        PrimaryContactFirstName = c.PrimaryContactFirstName,
        PrimaryContactLastName = c.PrimaryContactLastName,
        PrimaryPhone = c.PrimaryPhone,
        CellPhone = c.CellPhone,
        PrimaryFax = c.PrimaryFax,
        PrimaryEmail = c.PrimaryEmail,
        Website = c.Website,
        BillingAddressTitle = c.BillingAddressTitle,
        BillingAddress1 = c.BillingAddress1,
        BillingAddress2 = c.BillingAddress2,
        BillingCity = c.BillingCity,
        BillingState = c.BillingState,
        BillingZip = c.BillingZip,
        InstallationAddressTitle = c.InstallationAddressTitle,
        InstallationAddress1 = c.InstallationAddress1,
        InstallationAddress2 = c.InstallationAddress2,
        InstallationCity = c.InstallationCity,
        InstallationState = c.InstallationState,
        InstallationZip = c.InstallationZip,
        CreditLimit = c.CreditLimit,
        CreditStatus = c.CreditStatus,
        EquipmentDiscount = c.EquipmentDiscount,
        ServicePercent = c.ServicePercent,
        SalesPricingLevel = c.SalesPricingLevel,
        RentalPricingLevel = c.RentalPricingLevel,
        AccountManager = c.AccountManager,
        PaymentTerms = c.PaymentTerms,
        TaxId = c.TaxId,
        TaxRegionId = c.TaxRegionId,
        TaxPayer = c.TaxPayer,
        InsuranceExpires = c.InsuranceExpires,
        InsuranceNumber = c.InsuranceNumber,
        InsuranceAmount = c.InsuranceAmount,
        LossDamageWaiverPercent = c.LossDamageWaiverPercent,
        BillingTypeId = c.BillingTypeId,
        BillTypeId = c.BillTypeId,
        BillBy = c.BillBy,
        InvoiceFax = c.InvoiceFax,
        InvoiceEmails = c.InvoiceEmails,
        InvoiceSendBy = c.InvoiceSendBy,
        ServiceChargeTarget = c.ServiceChargeTarget,
        PoRequired = c.PoRequired,
        DoNotRent = c.DoNotRent,
        HasLatePayHistory = c.HasLatePayHistory,
        PayCommissions = c.PayCommissions,
        Notes = c.Notes,
        StatusId = c.StatusId,
        CreatedById = c.CreatedById,
        CreatedDate = c.CreatedDate,
        UpdatedById = c.UpdatedById,
        UpdatedDate = c.UpdatedDate
    };
}
