using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Identidade;

public sealed record CurrentUserDto(long Id,string Auth0Sub,string Nome,string Email,string Username);

public interface IIdentityService
{
    Task<CurrentUserDto> ResolveAsync(string auth0Sub,string? email,string? name,CancellationToken ct=default);
}

public sealed record UserPreferencesDto(string Theme,string TableDensity,bool SidebarOpen,bool EmailSummary,bool BrowserAlerts);
public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetAsync(long userId,CancellationToken ct=default);
    Task<UserPreferencesDto> SaveAsync(long userId,UserPreferencesDto dto,CancellationToken ct=default);
}

public sealed class UserPreferencesService(IRepository<Usuario> users,IRepository<UsuarioPreferencia> preferences)
    : IUserPreferencesService
{
    public async Task<UserPreferencesDto> GetAsync(long userId,CancellationToken ct=default)
    {
        if(await users.GetByIdAsync(userId,ct) is null)throw new NotFoundException("Usuário não encontrado.");
        var item=await preferences.FindAsync(x=>x.UserId==userId,ct);
        return item is null?new("Sistema","Confortável",true,true,false):Map(item);
    }
    public async Task<UserPreferencesDto> SaveAsync(long userId,UserPreferencesDto dto,CancellationToken ct=default)
    {
        if(await users.GetByIdAsync(userId,ct) is null)throw new NotFoundException("Usuário não encontrado.");
        var validThemes=new[]{"Sistema","Claro","Escuro"};var validDensity=new[]{"Confortável","Compacta"};
        if(!validThemes.Contains(dto.Theme)||!validDensity.Contains(dto.TableDensity))throw new ArgumentException("Preferência de interface inválida.");
        var item=await preferences.FindAsync(x=>x.UserId==userId,ct);
        if(item is null)
        {
            item=new(){UserId=userId,Theme=dto.Theme,TableDensity=dto.TableDensity,SidebarOpen=dto.SidebarOpen,
                EmailSummary=dto.EmailSummary,BrowserAlerts=dto.BrowserAlerts,UpdatedAt=DateTime.UtcNow};
            await preferences.AddAsync(item,ct);
        }
        else
        {
            item.Theme=dto.Theme;item.TableDensity=dto.TableDensity;item.SidebarOpen=dto.SidebarOpen;
            item.EmailSummary=dto.EmailSummary;item.BrowserAlerts=dto.BrowserAlerts;item.UpdatedAt=DateTime.UtcNow;
            preferences.Update(item);
        }
        await preferences.SaveChangesAsync(ct);return Map(item);
    }
    private static UserPreferencesDto Map(UsuarioPreferencia x)=>new(x.Theme,x.TableDensity,x.SidebarOpen,x.EmailSummary,x.BrowserAlerts);
}

public sealed class IdentityService(IRepository<Usuario> repository) : IIdentityService
{
    public async Task<CurrentUserDto> ResolveAsync(string auth0Sub,string? email,string? name,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(auth0Sub))throw new ArgumentException("Identificador Auth0 ausente.");
        var safeEmailFilter = email?.ToLower();
        var user=await repository.FindAsync(x=>x.Auth0Sub==auth0Sub,ct)
            ?? (string.IsNullOrWhiteSpace(safeEmailFilter) ? null : await repository.FindAsync(x=>x.Email.ToLower() == safeEmailFilter,ct));
        if(user is null)
        {
            var allUsers=await repository.ListAsync(ct);
            var safeEmail=string.IsNullOrWhiteSpace(email)?$"{auth0Sub.Replace('|','-')}@auth0.local":email.Trim();
            var prefix=safeEmail.Split('@')[0];var username=prefix;var suffix=1;
            while(allUsers.Any(x=>x.Username.Equals(username,StringComparison.OrdinalIgnoreCase)))username=$"{prefix}{suffix++}";
            user=new Usuario{Auth0Sub=auth0Sub,Email=safeEmail,NomeCompleto=string.IsNullOrWhiteSpace(name)?prefix:name.Trim(),
                Username=username,Enabled=true};
            await repository.AddAsync(user,ct);await repository.SaveChangesAsync(ct);
        }
        else if(string.IsNullOrWhiteSpace(user.Auth0Sub))
        {
            user.Auth0Sub=auth0Sub;repository.Update(user);await repository.SaveChangesAsync(ct);
        }
        return new(user.Id,user.Auth0Sub!,user.NomeCompleto,user.Email,user.Username);
    }
}
