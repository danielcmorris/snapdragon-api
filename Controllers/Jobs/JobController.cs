using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobController> _logger;
    private readonly IUserSessionService _session;

    public JobController(AppDbContext db, ILogger<JobController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<JobListResponse>> GetJobs(
        [FromQuery] int? statusId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? officeId = null,
        [FromQuery] int? orderStatus = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var query = _db.Job.Where(j => j.CompanyId == ctx.CompanyId);

        if (statusId.HasValue)
            query = query.Where(j => j.StatusId == statusId.Value);
        if (clientId.HasValue)
            query = query.Where(j => j.ClientId == clientId.Value);
        if (officeId.HasValue)
            query = query.Where(j => j.OfficeId == officeId.Value);
        if (orderStatus.HasValue)
            query = query.Where(j => j.OrderStatus == orderStatus.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(j =>
                (j.ClientName != null && j.ClientName.Contains(search)) ||
                (j.OrganizationName != null && j.OrganizationName.Contains(search)) ||
                (j.PurchaseOrderNumber != null && j.PurchaseOrderNumber.Contains(search)) ||
                (j.VisibleOrderId.HasValue && j.VisibleOrderId.Value.ToString().Contains(search)));

        var totalCount = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.OrderDate)
            .ThenByDescending(j => j.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(j => ToDto(j))
            .ToListAsync();

        return Ok(new JobListResponse { Jobs = jobs, TotalCount = totalCount });
    }

    [HttpGet("number/{jobNumber:int}")]
    public async Task<ActionResult<JobDto>> GetJobByNumber(int jobNumber)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var job = await _db.Job.FirstOrDefaultAsync(j => j.CompanyId == ctx.CompanyId && j.JobNumber == jobNumber);
        if (job == null)
            return NotFound();

        return Ok(ToDto(job));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> GetJob(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var job = await _db.Job.FindAsync(id);
        if (job == null || job.CompanyId != ctx.CompanyId)
            return NotFound();

        return Ok(ToDto(job));
    }

    [HttpPost]
    public async Task<ActionResult<JobDto>> CreateJob([FromBody] CreateJobRequest request)
    {
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

        // Verify client belongs to company if specified
        if (request.ClientId.HasValue)
        {
            var client = await _db.Client.FindAsync(request.ClientId.Value);
            if (client == null || client.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid client selection" });
        }

        // Auto-assign job number: max(company.NextJobNumber, maxExisting + 1)
        var company = await _db.Company.FindAsync(ctx.CompanyId);
        if (company == null)
            return BadRequest(new { error = "Company not found" });

        var maxJobNumber = await _db.Job
            .Where(j => j.CompanyId == ctx.CompanyId && j.JobNumber.HasValue)
            .MaxAsync(j => (int?)j.JobNumber);

        var assignedNumber = Math.Max(company.NextJobNumber, (maxJobNumber ?? 0) + 1);
        company.NextJobNumber = assignedNumber + 1;

        var now = DateTime.UtcNow;
        var job = new Job
        {
            Id = Guid.NewGuid(),
            JobNumber = assignedNumber,
            Vid = request.Vid,
            VisibleOrderId = request.VisibleOrderId,
            CompanyId = ctx.CompanyId,
            OfficeId = request.OfficeId,
            ClientId = request.ClientId,
            OrderDate = Utc(request.OrderDate),
            OrderStatus = request.OrderStatus,
            ClientName = request.ClientName,
            OrganizationName = request.OrganizationName,
            PurchaseOrderNumber = request.PurchaseOrderNumber,
            AccountNumber = request.AccountNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Fax = request.Fax,
            Email = request.Email,
            ContactInfo = request.ContactInfo,
            AddressTitle = request.AddressTitle,
            Address1 = request.Address1,
            Address2 = request.Address2,
            City = request.City,
            State = request.State,
            Zip = request.Zip,
            BillingFirstName = request.BillingFirstName,
            BillingLastName = request.BillingLastName,
            BillingPhone = request.BillingPhone,
            BillingFax = request.BillingFax,
            BillingEmail = request.BillingEmail,
            BillingAddressTitle = request.BillingAddressTitle,
            BillingAddress1 = request.BillingAddress1,
            BillingAddress2 = request.BillingAddress2,
            BillingCity = request.BillingCity,
            BillingState = request.BillingState,
            BillingZip = request.BillingZip,
            UseDirectBillAddress = request.UseDirectBillAddress,
            BeginDate = Utc(request.BeginDate),
            EndDate = Utc(request.EndDate),
            BeginTime = request.BeginTime,
            EndTime = request.EndTime,
            Action1Label = request.Action1Label,
            Action1Date = Utc(request.Action1Date),
            Action1Time = request.Action1Time,
            Action2Label = request.Action2Label,
            Action2Date = Utc(request.Action2Date),
            Action2Time = request.Action2Time,
            Action3Label = request.Action3Label,
            Action3Date = Utc(request.Action3Date),
            Action3Time = request.Action3Time,
            Action4Label = request.Action4Label,
            Action4Date = Utc(request.Action4Date),
            Action4Time = request.Action4Time,
            Action5Label = request.Action5Label,
            Action5Date = Utc(request.Action5Date),
            Action5Time = request.Action5Time,
            PaymentTerms = request.PaymentTerms,
            ShippingCost = request.ShippingCost,
            DeliveryCost = request.DeliveryCost,
            Discount = request.Discount,
            JobDiscount = request.JobDiscount,
            JobDiscountPercent = request.JobDiscountPercent,
            TaxRate = request.TaxRate,
            SalesTaxRate = request.SalesTaxRate,
            LaborTaxRate = request.LaborTaxRate,
            ServicePercent = request.ServicePercent,
            ServiceChargeCommission = request.ServiceChargeCommission,
            CommissionBase = request.CommissionBase,
            PaymentApplied = request.PaymentApplied,
            TotalWeight = request.TotalWeight,
            InvoiceDate = Utc(request.InvoiceDate),
            InvoiceId = request.InvoiceId,
            BillingDays = request.BillingDays,
            CreditMemo = request.CreditMemo,
            RateId = request.RateId,
            TaxId = request.TaxId,
            BillTypeId = request.BillTypeId,
            PackStatusId = request.PackStatusId,
            LaborRateId = request.LaborRateId,
            CustomerId = request.CustomerId,
            LossDamageWaiverPercent = request.LossDamageWaiverPercent,
            LossDamageWaiverActive = request.LossDamageWaiverActive,
            Notes = request.Notes,
            InternalNotes = request.InternalNotes,
            BanquetEventOrder = request.BanquetEventOrder,
            SalesLead = request.SalesLead,
            LastPrinted = Utc(request.LastPrinted),
            TemporaryId = request.TemporaryId,
            CopiedFromOrderId = request.CopiedFromOrderId,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Job.Add(job);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created job {JobId} - Order #{VisibleOrderId}", job.Id, job.VisibleOrderId);

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, ToDto(job));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobDto>> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var job = await _db.Job.FindAsync(id);
        if (job == null || job.CompanyId != ctx.CompanyId)
            return NotFound();

        // Verify office belongs to company if being changed
        if (request.OfficeId.HasValue)
        {
            var office = await _db.Office.FindAsync(request.OfficeId.Value);
            if (office == null || office.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid office selection" });
            job.OfficeId = request.OfficeId;
        }

        // Verify client belongs to company if being changed
        if (request.ClientId.HasValue)
        {
            var client = await _db.Client.FindAsync(request.ClientId.Value);
            if (client == null || client.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid client selection" });
            job.ClientId = request.ClientId;
        }

        if (request.JobNumber.HasValue) job.JobNumber = request.JobNumber;
        if (request.Vid.HasValue) job.Vid = request.Vid;
        if (request.VisibleOrderId.HasValue) job.VisibleOrderId = request.VisibleOrderId;
        if (request.OrderDate.HasValue) job.OrderDate = Utc(request.OrderDate);
        if (request.OrderStatus.HasValue) job.OrderStatus = request.OrderStatus;
        if (request.ClientName != null) job.ClientName = request.ClientName;
        if (request.OrganizationName != null) job.OrganizationName = request.OrganizationName;
        if (request.PurchaseOrderNumber != null) job.PurchaseOrderNumber = request.PurchaseOrderNumber;
        if (request.AccountNumber != null) job.AccountNumber = request.AccountNumber;
        if (request.FirstName != null) job.FirstName = request.FirstName;
        if (request.LastName != null) job.LastName = request.LastName;
        if (request.Phone != null) job.Phone = request.Phone;
        if (request.Fax != null) job.Fax = request.Fax;
        if (request.Email != null) job.Email = request.Email;
        if (request.ContactInfo != null) job.ContactInfo = request.ContactInfo;
        if (request.AddressTitle != null) job.AddressTitle = request.AddressTitle;
        if (request.Address1 != null) job.Address1 = request.Address1;
        if (request.Address2 != null) job.Address2 = request.Address2;
        if (request.City != null) job.City = request.City;
        if (request.State != null) job.State = request.State;
        if (request.Zip != null) job.Zip = request.Zip;
        if (request.BillingFirstName != null) job.BillingFirstName = request.BillingFirstName;
        if (request.BillingLastName != null) job.BillingLastName = request.BillingLastName;
        if (request.BillingPhone != null) job.BillingPhone = request.BillingPhone;
        if (request.BillingFax != null) job.BillingFax = request.BillingFax;
        if (request.BillingEmail != null) job.BillingEmail = request.BillingEmail;
        if (request.BillingAddressTitle != null) job.BillingAddressTitle = request.BillingAddressTitle;
        if (request.BillingAddress1 != null) job.BillingAddress1 = request.BillingAddress1;
        if (request.BillingAddress2 != null) job.BillingAddress2 = request.BillingAddress2;
        if (request.BillingCity != null) job.BillingCity = request.BillingCity;
        if (request.BillingState != null) job.BillingState = request.BillingState;
        if (request.BillingZip != null) job.BillingZip = request.BillingZip;
        if (request.UseDirectBillAddress.HasValue) job.UseDirectBillAddress = request.UseDirectBillAddress.Value;
        if (request.BeginDate.HasValue) job.BeginDate = Utc(request.BeginDate);
        if (request.EndDate.HasValue) job.EndDate = Utc(request.EndDate);
        if (request.BeginTime != null) job.BeginTime = request.BeginTime;
        if (request.EndTime != null) job.EndTime = request.EndTime;
        if (request.Action1Label != null) job.Action1Label = request.Action1Label;
        if (request.Action1Date.HasValue) job.Action1Date = Utc(request.Action1Date);
        if (request.Action1Time != null) job.Action1Time = request.Action1Time;
        if (request.Action2Label != null) job.Action2Label = request.Action2Label;
        if (request.Action2Date.HasValue) job.Action2Date = Utc(request.Action2Date);
        if (request.Action2Time != null) job.Action2Time = request.Action2Time;
        if (request.Action3Label != null) job.Action3Label = request.Action3Label;
        if (request.Action3Date.HasValue) job.Action3Date = Utc(request.Action3Date);
        if (request.Action3Time != null) job.Action3Time = request.Action3Time;
        if (request.Action4Label != null) job.Action4Label = request.Action4Label;
        if (request.Action4Date.HasValue) job.Action4Date = Utc(request.Action4Date);
        if (request.Action4Time != null) job.Action4Time = request.Action4Time;
        if (request.Action5Label != null) job.Action5Label = request.Action5Label;
        if (request.Action5Date.HasValue) job.Action5Date = Utc(request.Action5Date);
        if (request.Action5Time != null) job.Action5Time = request.Action5Time;
        if (request.PaymentTerms != null) job.PaymentTerms = request.PaymentTerms;
        if (request.ShippingCost.HasValue) job.ShippingCost = request.ShippingCost;
        if (request.DeliveryCost.HasValue) job.DeliveryCost = request.DeliveryCost;
        if (request.Discount.HasValue) job.Discount = request.Discount;
        if (request.JobDiscount.HasValue) job.JobDiscount = request.JobDiscount;
        if (request.JobDiscountPercent.HasValue) job.JobDiscountPercent = request.JobDiscountPercent;
        if (request.TaxRate.HasValue) job.TaxRate = request.TaxRate;
        if (request.SalesTaxRate.HasValue) job.SalesTaxRate = request.SalesTaxRate;
        if (request.LaborTaxRate.HasValue) job.LaborTaxRate = request.LaborTaxRate;
        if (request.ServicePercent.HasValue) job.ServicePercent = request.ServicePercent;
        if (request.ServiceChargeCommission.HasValue) job.ServiceChargeCommission = request.ServiceChargeCommission;
        if (request.CommissionBase.HasValue) job.CommissionBase = request.CommissionBase;
        if (request.PaymentApplied.HasValue) job.PaymentApplied = request.PaymentApplied;
        if (request.TotalWeight.HasValue) job.TotalWeight = request.TotalWeight;
        if (request.InvoiceDate.HasValue) job.InvoiceDate = Utc(request.InvoiceDate);
        if (request.InvoiceId.HasValue) job.InvoiceId = request.InvoiceId;
        if (request.BillingDays.HasValue) job.BillingDays = request.BillingDays;
        if (request.CreditMemo.HasValue) job.CreditMemo = request.CreditMemo.Value;
        if (request.RateId.HasValue) job.RateId = request.RateId;
        if (request.TaxId.HasValue) job.TaxId = request.TaxId;
        if (request.BillTypeId.HasValue) job.BillTypeId = request.BillTypeId;
        if (request.PackStatusId.HasValue) job.PackStatusId = request.PackStatusId;
        if (request.LaborRateId.HasValue) job.LaborRateId = request.LaborRateId;
        if (request.CustomerId.HasValue) job.CustomerId = request.CustomerId;
        if (request.LossDamageWaiverPercent.HasValue) job.LossDamageWaiverPercent = request.LossDamageWaiverPercent;
        if (request.LossDamageWaiverActive.HasValue) job.LossDamageWaiverActive = request.LossDamageWaiverActive.Value;
        if (request.Notes != null) job.Notes = request.Notes;
        if (request.InternalNotes != null) job.InternalNotes = request.InternalNotes;
        if (request.BanquetEventOrder != null) job.BanquetEventOrder = request.BanquetEventOrder;
        if (request.SalesLead != null) job.SalesLead = request.SalesLead;
        if (request.LastPrinted.HasValue) job.LastPrinted = Utc(request.LastPrinted);
        if (request.TemporaryId.HasValue) job.TemporaryId = request.TemporaryId;
        if (request.CopiedFromOrderId.HasValue) job.CopiedFromOrderId = request.CopiedFromOrderId;
        if (request.StatusId.HasValue) job.StatusId = request.StatusId.Value;

        job.UpdatedById = ctx.UserId;
        job.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated job {JobId}", job.Id);

        return Ok(ToDto(job));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var job = await _db.Job.FindAsync(id);
        if (job == null || job.CompanyId != ctx.CompanyId)
            return NotFound();

        _db.Job.Remove(job);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted job {JobId}", id);

        return NoContent();
    }

    private static DateTime? Utc(DateTime? dt) =>
        dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static JobDto ToDto(Job j) => new()
    {
        Id = j.Id,
        JobNumber = j.JobNumber,
        Vid = j.Vid,
        VisibleOrderId = j.VisibleOrderId,
        CompanyId = j.CompanyId,
        OfficeId = j.OfficeId,
        ClientId = j.ClientId,
        OrderDate = j.OrderDate,
        OrderStatus = j.OrderStatus,
        ClientName = j.ClientName,
        OrganizationName = j.OrganizationName,
        PurchaseOrderNumber = j.PurchaseOrderNumber,
        AccountNumber = j.AccountNumber,
        FirstName = j.FirstName,
        LastName = j.LastName,
        Phone = j.Phone,
        Fax = j.Fax,
        Email = j.Email,
        ContactInfo = j.ContactInfo,
        AddressTitle = j.AddressTitle,
        Address1 = j.Address1,
        Address2 = j.Address2,
        City = j.City,
        State = j.State,
        Zip = j.Zip,
        BillingFirstName = j.BillingFirstName,
        BillingLastName = j.BillingLastName,
        BillingPhone = j.BillingPhone,
        BillingFax = j.BillingFax,
        BillingEmail = j.BillingEmail,
        BillingAddressTitle = j.BillingAddressTitle,
        BillingAddress1 = j.BillingAddress1,
        BillingAddress2 = j.BillingAddress2,
        BillingCity = j.BillingCity,
        BillingState = j.BillingState,
        BillingZip = j.BillingZip,
        UseDirectBillAddress = j.UseDirectBillAddress,
        BeginDate = j.BeginDate,
        EndDate = j.EndDate,
        BeginTime = j.BeginTime,
        EndTime = j.EndTime,
        Action1Label = j.Action1Label,
        Action1Date = j.Action1Date,
        Action1Time = j.Action1Time,
        Action2Label = j.Action2Label,
        Action2Date = j.Action2Date,
        Action2Time = j.Action2Time,
        Action3Label = j.Action3Label,
        Action3Date = j.Action3Date,
        Action3Time = j.Action3Time,
        Action4Label = j.Action4Label,
        Action4Date = j.Action4Date,
        Action4Time = j.Action4Time,
        Action5Label = j.Action5Label,
        Action5Date = j.Action5Date,
        Action5Time = j.Action5Time,
        PaymentTerms = j.PaymentTerms,
        ShippingCost = j.ShippingCost,
        DeliveryCost = j.DeliveryCost,
        Discount = j.Discount,
        JobDiscount = j.JobDiscount,
        JobDiscountPercent = j.JobDiscountPercent,
        TaxRate = j.TaxRate,
        SalesTaxRate = j.SalesTaxRate,
        LaborTaxRate = j.LaborTaxRate,
        ServicePercent = j.ServicePercent,
        ServiceChargeCommission = j.ServiceChargeCommission,
        CommissionBase = j.CommissionBase,
        PaymentApplied = j.PaymentApplied,
        TotalWeight = j.TotalWeight,
        InvoiceDate = j.InvoiceDate,
        InvoiceId = j.InvoiceId,
        BillingDays = j.BillingDays,
        CreditMemo = j.CreditMemo,
        RateId = j.RateId,
        TaxId = j.TaxId,
        BillTypeId = j.BillTypeId,
        PackStatusId = j.PackStatusId,
        LaborRateId = j.LaborRateId,
        CustomerId = j.CustomerId,
        LossDamageWaiverPercent = j.LossDamageWaiverPercent,
        LossDamageWaiverActive = j.LossDamageWaiverActive,
        Notes = j.Notes,
        InternalNotes = j.InternalNotes,
        BanquetEventOrder = j.BanquetEventOrder,
        SalesLead = j.SalesLead,
        LastPrinted = j.LastPrinted,
        TemporaryId = j.TemporaryId,
        CopiedFromOrderId = j.CopiedFromOrderId,
        StatusId = j.StatusId,
        CreatedById = j.CreatedById,
        CreatedDate = j.CreatedDate,
        UpdatedById = j.UpdatedById,
        UpdatedDate = j.UpdatedDate
    };
}
