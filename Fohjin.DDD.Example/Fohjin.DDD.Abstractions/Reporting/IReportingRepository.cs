namespace Fohjin.DDD.Reporting
{
    public interface IReportingRepository
    {
        Task<IEnumerable<TDto>> GetByExampleAsync<TDto>(object? example) where TDto : class;
        Task SaveAsync<TDto>(TDto dto) where TDto : class;
        Task UpdateAsync<TDto>(object update, object where) where TDto : class;
        Task DeleteAsync<TDto>(object example) where TDto : class;
    }
}
