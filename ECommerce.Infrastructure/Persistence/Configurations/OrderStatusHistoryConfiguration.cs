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
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.ChangedAt).IsRequired();
            builder.Property(h => h.FromStatus).IsRequired();
            builder.Property(h => h.ToStatus).IsRequired();
            builder.Property(h => h.Notes).HasMaxLength(500);

            builder.HasOne(h => h.Order)
                   .WithMany(o => o.StatusHistory)
                   .HasForeignKey(h => h.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
