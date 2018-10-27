using System.Net.Http;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
	public interface IProposalManagerClientFactory
	{
		Task<HttpClient> GetProposalManagerClientAsync();
	}
}