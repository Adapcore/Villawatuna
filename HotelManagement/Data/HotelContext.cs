using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.SymbolStore;

namespace HotelManagement.Data
{
	public class HotelContext : DbContext
	{
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HotelContext(DbContextOptions<HotelContext> options,
            IHttpContextAccessor httpContextAccessor) : base(options)
		{
            _httpContextAccessor = httpContextAccessor;
        }

		public DbSet<Employee> Employees { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Payment> Payments { get; set; }


        public override int SaveChanges()
        {
            AddAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private int GetCurrentUser()
        {
            string strUserId = _httpContextAccessor.HttpContext?.User?.Identity?.GetUserId() ?? "0";

			int userId = 0;
			int.TryParse(strUserId, out userId);
			return userId;
        }

        private void AddAuditInfo()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                var now = DateTime.UtcNow;
                var currentUser = GetCurrentUser();

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = now;
                    entry.Entity.CreatedBy = currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModifiedDate = now;
                    entry.Entity.LastModifiedBy = currentUser;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Employee
			modelBuilder.Entity<Employee>(entity =>
			{
				entity.ToTable("Employees");
				entity.HasIndex(e => e.Email).IsUnique();
			});

			// Customer
			modelBuilder.Entity<Customer>(entity =>
			{
				entity.ToTable("Customers");
				entity.HasIndex(c => c.Email).IsUnique();
			});

			// Order
			modelBuilder.Entity<Order>(entity =>
			{
				entity.ToTable("Orders");
				entity.HasKey(o => o.OrderNo);
				entity.HasOne(o => o.Customer)
					  .WithMany()
					  .HasForeignKey(o => o.CustomerId)
					  .OnDelete(DeleteBehavior.Restrict);
				entity.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
				entity.Property(o => o.ServiceCharge).HasColumnType("decimal(18,2)");
				entity.Property(o => o.GrossAmount).HasColumnType("decimal(18,2)");
			});

			// OrderItem
			modelBuilder.Entity<OrderItem>(entity =>
			{
				entity.ToTable("OrderItems");
				entity.HasOne(oi => oi.Order)
					  .WithMany(o => o.OrderItems)
					  .HasForeignKey(oi => oi.OrderNo)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
				entity.Property(oi => oi.Amount).HasColumnType("decimal(18,2)");
			});

			// Invoice
			modelBuilder.Entity<Invoice>(entity =>
			{
				entity.ToTable("Invoices");
				entity.HasKey(i => i.InvoiceNo);			

				entity.HasOne(i => i.Customer)
					  .WithMany()
					  .HasForeignKey(i => i.CustomerId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
				entity.Property(i => i.ServiceCharge).HasColumnType("decimal(18,2)");
				entity.Property(i => i.GrossAmount).HasColumnType("decimal(18,2)");
				entity.Property(i => i.Paid).HasColumnType("decimal(18,2)");
				entity.Property(i => i.Balance).HasColumnType("decimal(18,2)");
			});

			// InvoiceDetail
			modelBuilder.Entity<InvoiceDetail>(entity =>
			{
				entity.ToTable("InvoiceDetails");

				entity.HasOne(d => d.Invoice)
					  .WithMany(i => i.InvoiceDetails)
					  .HasForeignKey(d => d.InvoiceNo)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
				entity.Property(d => d.Amount).HasColumnType("decimal(18,2)");
			});
		}
	}
}
