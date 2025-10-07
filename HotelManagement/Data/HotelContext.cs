using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Data
{
	public class HotelContext : DbContext
	{
		public HotelContext(DbContextOptions<HotelContext> options) : base(options)
		{
		}

		public DbSet<Employee> Employees { get; set; }

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

				entity.HasOne(i => i.Order)
					  .WithMany()
					  .HasForeignKey(i => i.OrderNo)
					  .OnDelete(DeleteBehavior.Restrict);

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
