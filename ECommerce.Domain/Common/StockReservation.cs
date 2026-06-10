using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Common
{
    public static class StockReservation
    {
        public static void Reserve(Product product, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            if (product.AvailableStock < quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. " +
                    $"Requested: {quantity} available: {product.AvailableStock}.");

            product.ReservedQuantity += quantity;
        }

        public static void Confirm(Product product, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - quantity);
            product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
        }

        public static void Release(Product product, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - quantity);
        }
    }
}
