using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Operacao;
using Microsoft.EntityFrameworkCore;

namespace CrmAtlas.Infrastructure.Data;

public sealed class AcompanhamentoRepository(AtlasDbContext db) : IAcompanhamentoRepository
{
    public async Task<IReadOnlyList<AcompanhamentoServico>> ListDetailedAsync(CancellationToken ct=default)=>
        await Query().AsNoTracking().ToListAsync(ct);
    public IQueryable<AcompanhamentoServico> AsQueryable()=>
        Query().AsNoTracking();
    public async Task<IReadOnlyList<AcompanhamentoServico>> ToListAsync(IQueryable<AcompanhamentoServico> query,CancellationToken ct=default)=>
        await query.ToListAsync(ct);
    public Task<AcompanhamentoServico?> GetDetailedAsync(long id,CancellationToken ct=default)=>
        Query().FirstOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<IReadOnlyList<AcompanhamentoServicoSituacaoConfig>> ListSituationsAsync(CancellationToken ct=default)=>
        await db.AcompanhamentoSituacoes.Include(x=>x.Pendencias).AsNoTracking().ToListAsync(ct);
    public Task<AcompanhamentoServicoSituacaoConfig?> GetSituationAsync(long id,CancellationToken ct=default)=>
        db.AcompanhamentoSituacoes.Include(x=>x.Pendencias).FirstOrDefaultAsync(x=>x.Id==id,ct);
    public async Task AddAsync(AcompanhamentoServico entity,CancellationToken ct=default)=>await db.Acompanhamentos.AddAsync(entity,ct);
    public async Task AddSituationAsync(AcompanhamentoServicoSituacaoConfig entity,CancellationToken ct=default)=>await db.AcompanhamentoSituacoes.AddAsync(entity,ct);
    public void Update(AcompanhamentoServico entity)=>db.Acompanhamentos.Update(entity);
    public void UpdateSituation(AcompanhamentoServicoSituacaoConfig entity)=>db.AcompanhamentoSituacoes.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct=default)=>db.SaveChangesAsync(ct);
    private IQueryable<AcompanhamentoServico> Query()=>db.Acompanhamentos
        .Include(x=>x.Historicos).Include(x=>x.Pendencias);
}
