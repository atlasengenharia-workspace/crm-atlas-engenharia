using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.Infrastructure.Common;

public sealed class NullUserAccessor : IUserAccessor
{
    public Task<string?> GetUserNameAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
}
