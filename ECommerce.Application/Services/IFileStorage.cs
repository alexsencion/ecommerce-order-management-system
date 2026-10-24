using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IFileStorage
    {
        Task SaveAsync(string path, byte[] content, CancellationToken ct = default);
        Task<byte[]?> ReadAsync(string path, CancellationToken ct = default);
        Task DeleteAsync(string path, CancellationToken ct = default);
    }
}
