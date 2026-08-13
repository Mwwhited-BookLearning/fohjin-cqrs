using Fohjin.DDD.Reporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.TestUtilities.Tools;

public class TestReportingRepository(
    TestContext testContext,
    IServiceProvider serviceProvider
        ) : IReportingRepository
{
    private readonly TestContext _testContext = testContext;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IQueryable<TDto> Query<TDto>() where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name + "-query", typeof(TDto));
        return new List<TDto> { (TDto)typeof(TDto).BuildObject(_serviceProvider) }.AsQueryable();
    }

    public Task<TDto?> GetByIdAsync<TDto>(object id) where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name + "-getbyid", id);
        return Task.FromResult<TDto?>((TDto)typeof(TDto).BuildObject(_serviceProvider));
    }

    public Task DeleteAsync<TDto>(object example) where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name + "-delete", example);
        return Task.CompletedTask;
    }

    public Task SaveAsync<TDto>(TDto dto) where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name, dto);
        return Task.CompletedTask;
    }

    public Task UpdateAsync<TDto>(object update, object where) where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name + "-update", update);
        _testContext.AddResults(typeof(TDto).Name + "-where", where);
        return Task.CompletedTask;
    }
}
