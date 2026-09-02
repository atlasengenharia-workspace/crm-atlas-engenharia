using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace CrmAtlas.Infrastructure.Data;

public sealed class CadastroServicoRepository(AtlasDbContext dbContext)
    : ICadastroServicoRepository
{
    public async Task<CadastroServico?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await dbContext.CadastrosServico.FindAsync([id], cancellationToken);

    public async Task<CadastroServico?> FindAsync(Expression<Func<CadastroServico, bool>> predicate, CancellationToken cancellationToken = default) =>
        await dbContext.CadastrosServico.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<CadastroServico>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CadastrosServico.AsNoTracking().ToListAsync(cancellationToken);

    public IQueryable<CadastroServico> AsQueryable() =>
        dbContext.CadastrosServico.AsNoTracking();

    public IQueryable<CadastroServico> AsNoTrackingDetailed() =>
        Query().AsNoTracking();

    public async Task<IReadOnlyList<CadastroServico>> ToListAsync(
        IQueryable<CadastroServico> query,
        CancellationToken cancellationToken = default) =>
        await query.ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        IQueryable<CadastroServico> query,
        CancellationToken cancellationToken = default) =>
        await query.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CadastroServico>> ListDetailedAsync(
        CancellationToken cancellationToken = default) =>
        await Query().AsNoTracking().ToListAsync(cancellationToken);

    public Task<CadastroServico?> GetDetailedAsync(long id, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(CadastroServico entity, CancellationToken cancellationToken = default) =>
        await dbContext.CadastrosServico.AddAsync(entity, cancellationToken);

    public void Update(CadastroServico entity) => dbContext.CadastrosServico.Update(entity);
    public void Remove(CadastroServico entity) => dbContext.CadastrosServico.Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<CadastroServico> Query() =>
        dbContext.CadastrosServico
            .Include(x => x.Cliente)
            .Include(x => x.Orcamento)
            .Include(x => x.CondicaoPagamento)
            .Include(x => x.Parcelas)
            .Include(x => x.Prestadores)
                .ThenInclude(x => x.Prestador)
            .Include(x => x.CodigoHistorico);
}

