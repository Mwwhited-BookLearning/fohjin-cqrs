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

    public Task DeleteAsync<TDto>(object example) where TDto : class
    {
        _testContext.AddResults(typeof(TDto).Name + "-delete", example);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TDto>> GetByExampleAsync<TDto>(object? example) where TDto : class
    {
        if (example == null)
            return Task.FromResult(Enumerable.Empty<TDto>());

        _testContext.AddResults(typeof(TDto).Name + "-getby", example);
        var results = new List<TDto> { (TDto)typeof(TDto).BuildObject(_serviceProvider) };
        return Task.FromResult<IEnumerable<TDto>>(results);
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
