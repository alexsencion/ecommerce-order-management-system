using ECommerce.Application.Common;
using ECommerce.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Infrastructure
{
    public class LocalFileStorageTests : IDisposable
    {
        private readonly string _root =
            Path.Combine(Path.GetTempPath(), $"storage-tests-{Guid.NewGuid():N}");
        private readonly LocalFileStorage _sut;

        public LocalFileStorageTests()
        {
            _sut = new LocalFileStorage(Options.Create(
                new FileStorageSettings { RootPath = _root }));
        }

        [Fact]
        public async Task SaveThenRead_RoundTripsBytes()
        {
            var bytes = new byte[] { 1, 2, 3 };

            await _sut.SaveAsync("invoices/2026/10/test.pdf", bytes);
            var read = await _sut.ReadAsync("invoices/2026/10/test.pdf");

            read.Should().Equal(bytes);
        }

        [Fact]
        public async Task Read_MissingFile_ReturnsNull()
        {
            (await _sut.ReadAsync("missing.pdf")).Should().BeNull();
        }

        [Fact]
        public async Task Delete_RemovesFile()
        {
            await _sut.SaveAsync("a.pdf", new byte[] { 1 });

            await _sut.DeleteAsync("a.pdf");

            (await _sut.ReadAsync("a.pdf")).Should().BeNull();
        }

        [Fact]
        public async Task Save_PathTraversal_Throws()
        {
            var act = () => _sut.SaveAsync("../outside.pdf", new byte[] { 1 });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }
}
