namespace CrmAtlas.ApplicationCore.Common;

public interface IUserAccessor
{
    Task<string?> GetUserNameAsync(CancellationToken ct = default);
}
