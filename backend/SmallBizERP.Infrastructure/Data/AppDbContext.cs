using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;

namespace SmallBizERP.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==============================
        // Business
        // ==============================

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("KES");

            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("Kenya");
        });

        // ==============================
        // Category
        // ==============================

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(e => new { e.BusinessId, e.Name })
                .IsUnique();
        });

        // ==============================
        // Product
        // ==============================

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.SKU)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.BuyingPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.SellingPrice)
                .HasPrecision(18, 2);

            entity.HasIndex(e => new { e.BusinessId, e.SKU })
                .IsUnique();

            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Supplier
        // ==============================

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Phone)
                .HasMaxLength(30);

            entity.Property(e => e.Email)
                .HasMaxLength(150);
        });

        // ==============================
        // Customer
        // ==============================

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Phone)
                .HasMaxLength(30);

            entity.Property(e => e.Email)
                .HasMaxLength(150);
        });

        // ==============================
        // Employee
        // ==============================

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
        });

        // ==============================
        // Purchase
        // ==============================

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PurchaseNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.Discount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Tax)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.AmountPaid)
                .HasPrecision(18, 2);

            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);

            entity.HasIndex(e => new { e.BusinessId, e.PurchaseNumber })
                .IsUnique();

            entity.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Purchase Item
        // ==============================

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.Total)
                .HasPrecision(18, 2);

            entity.HasOne<Purchase>()
                .WithMany()
                .HasForeignKey(e => e.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Sale
        // ==============================

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SaleNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.Discount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Tax)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.AmountPaid)
                .HasPrecision(18, 2);

            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);

            entity.HasIndex(e => new { e.BusinessId, e.SaleNumber })
                .IsUnique();

            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Sale Item
        // ==============================

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.UnitCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.Discount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Total)
                .HasPrecision(18, 2);

            entity.HasOne<Sale>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Payment
        // ==============================

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(30);
        });

        // ==============================
        // Expense
        // ==============================

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Category)
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });

        // ==============================
        // Stock Transaction
        // ==============================

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Audit Log
        // ==============================

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(50);
        });
    }
}