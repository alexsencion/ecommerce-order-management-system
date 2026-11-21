using ECommerce.Application.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Common
{
    public class InMemoryFileStorage : IFileStorage
    {

        private readonly ConcurrentDictionary<string, byte[]> _files = new();

        public IReadOnlyDictionary<string, byte[]> Files => _files;

        public Task DeleteAsync(string path, CancellationToken ct = default)
        {
            _files.TryRemove(path, out _);
            return Task.CompletedTask;
        }

        public Task<byte[]?> ReadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult(_files.TryGetValue(path, out var content) ? content: null);

        public Task SaveAsync(string path, byte[] content, CancellationToken ct = default)
        {
            _files[path] = content;
            return Task.CompletedTask;
        }
    }
}
