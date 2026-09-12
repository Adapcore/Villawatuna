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
		public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }
		public DbSet<PettyCashBook> PettyCashBooks { get; set; }
        public DbSet<CurrencyExchange> CurrencyExchanges { get; set; }


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
				
				//entity.HasOne<Employee>(o => o.CreatedByEmployee)
				//	  .WithMany()
				//	  .HasForeignKey(o => o.CreatedBy)
				//	  .OnDelete(DeleteBehavior.Restrict)
				//	  .IsRequired(false);
					  
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
				entity.Property(i => i.TotalPaid).HasColumnType("decimal(18,2)");
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

            // Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Invoice)
                      .WithMany(o => o.Payments)
                      .HasForeignKey(p => p.InvoiceNo);
            });

			// EmployeeLeave
			modelBuilder.Entity<EmployeeLeave>(entity =>
			{
				entity.ToTable("EmployeeLeaves");

				entity.HasOne(l => l.Employee)
					  .WithMany()
					  .HasForeignKey(l => l.EmployeeId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasIndex(l => new { l.EmployeeId, l.FromDate });
				entity.HasIndex(l => new { l.Status, l.RequestDate });

				entity.Property(l => l.NoOfDays).HasColumnType("decimal(3,1)");
			});
            
            // Expense - no additional configuration needed as CreatedByMember is [NotMapped]

			// PettyCashBook — JSON columns for line items and per-currency income totals
			modelBuilder.Entity<PettyCashBook>(entity =>
			{
				entity.ToTable("PettyCashBook");
				entity.HasKey(e => e.Id);
				entity.HasIndex(e => e.Date).IsUnique();

				entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
				entity.Property(e => e.PettyCashRefillOpening).HasColumnType("decimal(18,2)");
				entity.Property(e => e.ClosingBalance)
					.HasColumnType("decimal(18,2)")
					.HasColumnName("CloseBalance");
				entity.Property(e => e.PettyCashRefillClosing).HasColumnType("decimal(18,2)");
				entity.Property(e => e.TotalIncomeBaseCurry).HasColumnType("decimal(18,2)");
				entity.Property(e => e.TotalExpences).HasColumnType("decimal(18,2)");
				entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
				entity.Property(e => e.HandedOver).HasColumnType("decimal(18,2)");
				entity.Property(e => e.Status)
					.HasConversion<int>()
					.HasDefaultValue(HotelManagement.Enums.PettyCashStatus.Open);

				entity.OwnsMany(p => p.Income, b => b.ToJson("Income"));
				entity.OwnsMany(p => p.Expences, b => b.ToJson("Expences"));
				entity.OwnsMany(p => p.TotalIncome, b => b.ToJson("TotalIncome"));
				entity.OwnsMany(p => p.Refills, b => b.ToJson("Refills"));
			});

            modelBuilder.Entity<CurrencyExchange>(entity =>
            {
                entity.ToTable("CurrencyExchanges");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Invoice)
                      .WithMany()
                      .HasForeignKey(e => e.InvoiceNo)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Expense)
                      .WithMany()
                      .HasForeignKey(e => e.ExpenseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18,4)");
                entity.Property(e => e.ForeignAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LKRAmount).HasColumnType("decimal(18,2)");
            });
        }
	}
}
