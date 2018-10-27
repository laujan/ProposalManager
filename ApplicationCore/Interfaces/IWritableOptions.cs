using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IWritableOptions<out T> : IOptionsSnapshot<T> where T : class, new()
    {
        Task<StatusCodes> UpdateAsync(string key, string value, string requestId = "");
    }
}
