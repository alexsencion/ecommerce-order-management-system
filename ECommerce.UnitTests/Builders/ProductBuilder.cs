using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Builders
{
    public class ProductBuilder
    {
        private string _name = "Test Product";
        private string _sku = "SKU-001";
        private decimal _price = 29.99m;
        private int _stockQuantity = 100;
        private int _reservedQty = 0;
        private bool _isActive = true;
        private Guid _categoryId = Guid.NewGuid();

        public ProductBuilder WithName(string v) { _name = v; return this; }
        public ProductBuilder WithSku(string v) { _sku = v; return this; }
        public ProductBuilder WithPrice(decimal v) { _price = v; return this; }
        public ProductBuilder WithStock(int v) { _stockQuantity = v; return this; }
        public ProductBuilder WithReserved(int v) { _reservedQty = v; return this; }
        public ProductBuilder WithCategoryId(Guid v) { _categoryId = v; return this; }
        public ProductBuilder AsInactive() { _isActive = false; return this; }

        public Product Build() => new()
        {
            Name = _name,
            Sku = _sku,
            Price = _price,
            StockQuantity = _stockQuantity,
            ReservedQuantity = _reservedQty,
            IsActive = _isActive,
            CategoryId = _categoryId
        };

        public static List<Product> BuildMany(int count, Guid? category = null) =>
            Enumerable.Range(1, count).Select(i => new ProductBuilder()
                .WithName($"Product {i}")
                .WithSku($"SKU-{i:D3}")
                .WithCategoryId(category ?? Guid.NewGuid())
                .Build()).ToList();
    }
}
