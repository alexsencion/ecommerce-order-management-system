using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void Dispose()
        {
            _scope.Dispose();
            Client.Dispose();
        }
    }
}
