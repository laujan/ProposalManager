using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IAzureKeyVaultService
    {
        Task<string> GetValueFromVaultAsync(string key, string requestId = "");
        Task SetValueInVaultAsync(string key, string value, string requestId = "");
    }
}