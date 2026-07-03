using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Common
{
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        protected readonly HttpClient Client;
        protected readonly CustomWebApplicationFactory Factory;
        private readonly IServiceScope _scope;
        protected readonly AppDbContext DbContext;

        protected IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
            _scope = factory.Services.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        protected async Task<T> SeedAsync<T>(T entity) where T : class
        {
            DbContext.Set<T>().Add(entity);
            await DbContext.SaveChangesAsync();
            return entity;
        }

        protected void ClearTable<T>() where T : class
        {
            DbContext.Set<T>().RemoveRange(DbContext.Set<T>());
            DbContext.SaveChanges();
        }

        protected async Task<(Customer customer, Product product, Order order)>
            SeedOrderAsync(
                int stockQuantity = 100,
                int reservedQuantity = 2,
                OrderStatus status = OrderStatus.Pending)
        {
            var category = new Category { Name = "Test", Slug = $"test-{Guid.NewGuid():N}" };
            DbContext.Categories.Add(category);

            var customer = new Customer
            {
                FirstName = "Test",
                LastName = "Customer",
                Email = $"test_{Guid.NewGuid():N}@example.com",
                IsActive = true
            };
            DbContext.Customers.Add(customer);

            var product = new Product
            {
                CategoryId = category.Id,
                Name = "Test",
                Sku = $"TST-{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant(),
                Price = 50m,
                StockQuantity = stockQuantity,
                ReservedQuantity = reservedQuantity,
                IsActive = true
            };
            DbContext.Products.Add(product);

            var order = new Order
            {
                CustomerId = customer.Id,
                Status = status,
                Subtotal = 100m,
                Tax = 18m,
                ShippingAddressJson = JsonSerializer.Serialize(new
                {
                    street = "123 Main St",
                    city = "Santo Domingo",
                    state = "DN",
                    zipCode = "10101",
                    country = "DO"
                }),
                Items = new List<OrderItem>
                {
                    new() { ProductId = product.Id, Quantity = 2, UnitPrice = 50m },
                }
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            return (customer, product, order);
        }

        protected async Task<T?> ReloadAsync<T>(Guid id) where T : class
        {
            var tracked = DbContext.ChangeTracker
                .Entries<T>()
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;

            return await DbContext.Set<T>().FindAsync(id);
        }

        public void Dispose()
        {
            _scope.Dispose();
            Client.Dispose();
        }
    }
}
