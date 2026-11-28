using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

            builder.HasIndex(i => i.OrderId).IsUnique();

            builder.Property(i => i.BillToName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.BillToEmail).IsRequired().HasMaxLength(256);
            builder.Property(i => i.StoragePath).IsRequired().HasMaxLength(300);

            builder.Property(i => i.Subtotal).HasPrecision(18, 2);
            builder.Property(i => i.Tax).HasPrecision(18, 2);
            builder.Property(i => i.Total).HasPrecision(18, 2);

            builder.Ignore(i => i.FileName);

            builder.HasOne(i => i.Order)
                .WithOne()
                .HasForeignKey<Invoice>(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
