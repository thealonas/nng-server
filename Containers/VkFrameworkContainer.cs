using nng.DatabaseProviders;
using nng.Extensions;
using nng.VkFrameworks;

namespace nng_server.Containers;

public class VkFrameworkContainer
{
    private VkFrameworkContainer()
    {
        Framework = new VkFramework();
    }

    public VkFramework Framework { get; private set; }

    private static VkFrameworkContainer? Instance { get; set; }

    public void UpdateToken(TokensDatabaseProvider tokens)
    {
        var token = tokens.GetTokenWithPermission("server");

        if (Framework.CurrentUser.Id.Equals(token.UserId)) return;

        Framework = new VkFramework(token.Token ?? throw new InvalidOperationException());
    }

    public static VkFrameworkContainer GetInstance()
    {
        return Instance ??= new VkFrameworkContainer();
    }
}
