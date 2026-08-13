namespace Fohjin.DDD.Reporting;

public interface IReportingRepository
{
    // Composable read side: one query mechanism for every DTO, rather than each call site
    // inventing its own example-object shape (the former GetByExampleAsync). Callers add
    // .Where()/.OrderBy()/etc themselves; the OData endpoints layer ODataQueryOptions.ApplyTo
    // on top of the exact same IQueryable<TDto>.
    IQueryable<TDto> Query<TDto>() where TDto : class;

    // The common single-row-by-primary-key case gets its own fast path rather than being
    // folded into Query<TDto>() - every DTO's PK is literally named Id.
    Task<TDto?> GetByIdAsync<TDto>(object id) where TDto : class;

    Task SaveAsync<TDto>(TDto dto) where TDto : class;
    Task UpdateAsync<TDto>(object update, object where) where TDto : class;
    Task DeleteAsync<TDto>(object example) where TDto : class;
}
