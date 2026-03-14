using System.Threading;
using System.Threading.Tasks;

namespace Solhigson.Framework.Web.Api;

public interface IApiRequestService
{
    Task<ApiRequestResponse> SendAsync(ApiRequest request, CancellationToken ct = default);

    Task<ApiRequestResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken ct = default);
}
