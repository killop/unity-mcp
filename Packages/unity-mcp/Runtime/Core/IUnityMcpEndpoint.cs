using System.Threading.Tasks;

namespace UnityMcp
{
    public interface IUnityMcpEndpoint
    {
        string EndpointId { get; }
        string EndpointKind { get; }
        string EndpointName { get; }
        UnityMcpToolRegistry Tools { get; }
        string BuildHealthJson();
        Task<string> CallToolAsync(string name, UnityMcpToolArguments arguments);
    }
}
