using Microsoft.EntityFrameworkCore;

namespace SnapdragonApi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Status> Status => Set<Status>();
    public DbSet<Company> Company => Set<Company>();
    public DbSet<Office> Office => Set<Office>();
    public DbSet<User> User => Set<User>();
    public DbSet<Warehouse> Warehouse => Set<Warehouse>();
    public DbSet<MasterProduct> MasterProduct => Set<MasterProduct>();
    public DbSet<Product> Product => Set<Product>();
    public DbSet<Stock> Stock => Set<Stock>();
    public DbSet<Allocation> Allocation => Set<Allocation>();
    public DbSet<UserGroup> UserGroup => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMember => Set<UserGroupMember>();
    public DbSet<UserOfficeAccess> UserOfficeAccess => Set<UserOfficeAccess>();
    public DbSet<UserWarehouseAccess> UserWarehouseAccess => Set<UserWarehouseAccess>();
    public DbSet<Supplier> Supplier => Set<Supplier>();
    public DbSet<Client> Client => Set<Client>();
    public DbSet<Job> Job => Set<Job>();
    public DbSet<ClientRoom> ClientRoom => Set<ClientRoom>();
    public DbSet<JobRoom> JobRoom => Set<JobRoom>();
    public DbSet<JobRoomProduct> JobRoomProduct => Set<JobRoomProduct>();
    public DbSet<JobLabor> JobLabor => Set<JobLabor>();
    public DbSet<Package> Package => Set<Package>();
    public DbSet<PackageProduct> PackageProduct => Set<PackageProduct>();
    public DbSet<OfficeProduct> OfficeProduct => Set<OfficeProduct>();
    public DbSet<ClientContact> ClientContact => Set<ClientContact>();
    public DbSet<ClientCommission> ClientCommission => Set<ClientCommission>();
    public DbSet<CommissionType> CommissionType => Set<CommissionType>();
    public DbSet<TaxRegion> TaxRegion => Set<TaxRegion>();
    public DbSet<OfficeWarehouse> OfficeWarehouse => Set<OfficeWarehouse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_company_name");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_company_status_id");
        });

        modelBuilder.Entity<Office>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_office_company_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_office_name");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_office_status_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.OfficeId).HasDatabaseName("ix_user_office_id");
            entity.HasIndex(e => e.Email).HasDatabaseName("ix_user_email");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_user_status_id");
            entity.HasIndex(e => e.DefaultWarehouseId).HasDatabaseName("ix_user_default_warehouse_id");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_warehouse_company_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_warehouse_name");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_warehouse_status_id");
        });

        modelBuilder.Entity<MasterProduct>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_master_product_company_id");
            entity.HasIndex(e => e.PartNumber).HasDatabaseName("ix_master_product_part_number");
            entity.HasIndex(e => e.ProductName).HasDatabaseName("ix_master_product_product_name");
            entity.HasIndex(e => e.Category).HasDatabaseName("ix_master_product_category");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_master_product_status_id");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.MasterProductId).HasDatabaseName("ix_product_master_product_id");
            entity.HasIndex(e => e.SerialNumber).HasDatabaseName("ix_product_serial_number");
            entity.HasIndex(e => e.Barcode).HasDatabaseName("ix_product_barcode");
            entity.HasIndex(e => e.IsAvailable).HasDatabaseName("ix_product_is_available");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_product_status_id");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(e => e.ProductId).HasDatabaseName("ix_stock_product_id");
            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("ix_stock_warehouse_id");
            entity.HasIndex(e => e.Location).HasDatabaseName("ix_stock_location");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_stock_status_id");
        });

        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.HasIndex(e => e.StockId).HasDatabaseName("ix_allocation_stock_id");
            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("ix_allocation_warehouse_id");
            entity.HasIndex(e => e.StartDate).HasDatabaseName("ix_allocation_start_date");
            entity.HasIndex(e => e.EndDate).HasDatabaseName("ix_allocation_end_date");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_allocation_status_id");
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_user_group_company_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_user_group_name");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_user_group_status_id");
        });

        modelBuilder.Entity<UserGroupMember>(entity =>
        {
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_user_group_member_user_id");
            entity.HasIndex(e => e.UserGroupId).HasDatabaseName("ix_user_group_member_user_group_id");
            entity.HasIndex(e => new { e.UserId, e.UserGroupId })
                  .IsUnique()
                  .HasDatabaseName("ix_user_group_member_user_id_user_group_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_user_group_member_status_id");
        });

        modelBuilder.Entity<UserOfficeAccess>(entity =>
        {
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_user_office_access_user_id");
            entity.HasIndex(e => e.OfficeId).HasDatabaseName("ix_user_office_access_office_id");
            entity.HasIndex(e => new { e.UserId, e.OfficeId })
                  .IsUnique()
                  .HasDatabaseName("ix_user_office_access_user_id_office_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_user_office_access_status_id");
        });

        modelBuilder.Entity<UserWarehouseAccess>(entity =>
        {
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_user_warehouse_access_user_id");
            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("ix_user_warehouse_access_warehouse_id");
            entity.HasIndex(e => new { e.UserId, e.WarehouseId })
                  .IsUnique()
                  .HasDatabaseName("ix_user_warehouse_access_user_id_warehouse_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_user_warehouse_access_status_id");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_supplier_company_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_supplier_name");
            entity.HasIndex(e => e.Code).HasDatabaseName("ix_supplier_code");
            entity.HasIndex(e => e.Category).HasDatabaseName("ix_supplier_category");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_supplier_status_id");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_client_company_id");
            entity.HasIndex(e => e.OfficeId).HasDatabaseName("ix_client_office_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_client_name");
            entity.HasIndex(e => e.CustomerCode).HasDatabaseName("ix_client_customer_code");
            entity.HasIndex(e => e.ExternalId).HasDatabaseName("ix_client_external_id");
            entity.HasIndex(e => e.PrimaryEmail).HasDatabaseName("ix_client_primary_email");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_client_status_id");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.JobNumber })
                  .IsUnique()
                  .HasFilter("job_number IS NOT NULL")
                  .HasDatabaseName("uq_job_company_job_number");
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_job_company_id");
            entity.HasIndex(e => e.OfficeId).HasDatabaseName("ix_job_office_id");
            entity.HasIndex(e => e.ClientId).HasDatabaseName("ix_job_client_id");
            entity.HasIndex(e => e.Vid).HasDatabaseName("ix_job_vid");
            entity.HasIndex(e => e.VisibleOrderId).HasDatabaseName("ix_job_visible_order_id");
            entity.HasIndex(e => e.OrderDate).HasDatabaseName("ix_job_order_date");
            entity.HasIndex(e => e.OrderStatus).HasDatabaseName("ix_job_order_status");
            entity.HasIndex(e => e.InvoiceId).HasDatabaseName("ix_job_invoice_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_job_status_id");
        });

        modelBuilder.Entity<ClientRoom>(entity =>
        {
            entity.HasIndex(e => e.ClientId).HasDatabaseName("ix_client_room_client_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_client_room_status_id");
        });

        modelBuilder.Entity<JobRoom>(entity =>
        {
            entity.HasIndex(e => e.JobId).HasDatabaseName("ix_job_room_job_id");
            entity.HasIndex(e => e.ClientRoomId).HasDatabaseName("ix_job_room_client_room_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_job_room_status_id");
        });

        modelBuilder.Entity<JobRoomProduct>(entity =>
        {
            entity.HasIndex(e => e.JobRoomId).HasDatabaseName("ix_job_room_product_job_room_id");
            entity.HasIndex(e => e.ParentId).HasDatabaseName("ix_job_room_product_parent_id");
            entity.HasIndex(e => e.PackageId).HasDatabaseName("ix_job_room_product_package_id");
            entity.HasIndex(e => e.MasterProductId).HasDatabaseName("ix_job_room_product_master_product_id");
        });

        modelBuilder.Entity<JobLabor>(entity =>
        {
            entity.HasIndex(e => e.JobId).HasDatabaseName("ix_job_labor_job_id");
            entity.HasIndex(e => e.JobRoomId).HasDatabaseName("ix_job_labor_job_room_id");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_package_company_id");
            entity.HasIndex(e => e.StatusId).HasDatabaseName("ix_package_status_id");
        });

        modelBuilder.Entity<PackageProduct>(entity =>
        {
            entity.HasIndex(e => e.PackageId).HasDatabaseName("ix_package_product_package_id");
            entity.HasIndex(e => e.MasterProductId).HasDatabaseName("ix_package_product_master_product_id");
        });

        modelBuilder.Entity<OfficeProduct>(entity =>
        {
            entity.HasIndex(e => e.OfficeId).HasDatabaseName("ix_office_product_office_id");
            entity.HasIndex(e => e.MasterProductId).HasDatabaseName("ix_office_product_master_product_id");
            entity.HasIndex(e => new { e.OfficeId, e.MasterProductId })
                  .IsUnique()
                  .HasDatabaseName("uq_office_product_office_master");
        });

        modelBuilder.Entity<ClientContact>()
            .HasIndex(e => e.ClientId).HasDatabaseName("ix_client_contact_client_id");
        modelBuilder.Entity<ClientCommission>()
            .HasIndex(e => e.ClientId).HasDatabaseName("ix_client_commission_client_id");
    }
}
