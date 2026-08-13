using Fohjin.DDD.Reporting.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Fohjin.DDD.Reporting.Infrastructure;

public class SqlServerReportingRepository(IDbContextFactory<ReportingDbContext> dbContextFactory) : IReportingRepository, IAsyncDisposable
{
    private readonly IDbContextFactory<ReportingDbContext> _dbContextFactory = dbContextFactory;

    // Query<TDto>()/GetByIdAsync<TDto>() return/use a *live* context the caller composes
    // further (.Where(), OData's ApplyTo(), an ordered .Include(), etc) after the method
    // itself has already returned - unlike every other method here, which opens and disposes
    // its own DbContext within a single call, these can't dispose before returning.
    //
    // A single cached context reused across every call on this instance (the first thing
    // tried here) is unsafe in this codebase specifically: IReportingRepository is Transient,
    // but Fohjin.DDD.MessageRouting's EventSubscriptionBootstrapper resolves every IEventHandler
    // exactly once at startup and keeps that instance (and everything it captured via
    // constructor injection, transitively) for the app's entire lifetime to back its Rx
    // subscription - so a repository injected into an event handler (or anything an event
    // handler's constructor chain depends on, e.g. Fohjin.DDD.Services.MoneyTransferService)
    // is not actually short-lived just because it's registered Transient. A single reused
    // DbContext there would violate "DbContext isn't safe for concurrent operations" the
    // moment two dispatches overlap, and its change tracker would grow for as long as the
    // process runs, since query results are tracked by default. Creating a fresh context per
    // call and disposing all of them together (whenever this repository instance itself
    // finally is disposed) avoids both, at the cost of not reusing a context across multiple
    // Query/GetByIdAsync calls on the same instance - a fine trade here since nothing in this
    // codebase composes a query built from one call against a query built from another.
    private readonly List<ReportingDbContext> _readContexts = [];

    public IQueryable<TDto> Query<TDto>() where TDto : class
    {
        var context = _dbContextFactory.CreateDbContext();
        _readContexts.Add(context);
        return context.Set<TDto>();
    }

    public async Task<TDto?> GetByIdAsync<TDto>(object id) where TDto : class
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        _readContexts.Add(context);

        // ClientDetailsReport/AccountDetailsReport are the only DTOs with real child
        // navigations (ReportingDbContext) - a short, explicit list rather than a reflection
        // walk, matching Query<TDto>()'s own "caller orders explicitly" philosophy everywhere
        // else. EF Core's filtered/ordered Include() only recognizes inline LINQ operators in
        // the lambda, not a call to a shared extension method, so the InsertionSequence
        // ordering is written out at each site rather than factored into one helper.
        if (typeof(TDto) == typeof(ClientDetailsReport) && id is Guid clientDetailsId)
        {
            return (TDto?)(object?)await context.Set<ClientDetailsReport>()
                .Include(x => x.AllAccounts.OrderBy(a => EF.Property<long>(a, ReportingDbContext.InsertionSequenceShadowProperty)))
                .Include(x => x.BankCards.OrderBy(b => EF.Property<long>(b, ReportingDbContext.InsertionSequenceShadowProperty)))
                .FirstOrDefaultAsync(x => x.Id == clientDetailsId);
        }

        if (typeof(TDto) == typeof(AccountDetailsReport) && id is Guid accountDetailsId)
        {
            return (TDto?)(object?)await context.Set<AccountDetailsReport>()
                .Include(x => x.Ledgers.OrderBy(l => EF.Property<long>(l, ReportingDbContext.InsertionSequenceShadowProperty)))
                .FirstOrDefaultAsync(x => x.Id == accountDetailsId);
        }

        return await context.Set<TDto>().FirstOrDefaultAsync(BuildPredicate<TDto>(new Dictionary<string, object?> { ["Id"] = id }));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _readContexts)
            await context.DisposeAsync();
    }

    public async Task SaveAsync<TDto>(TDto dto) where TDto : class
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.Set<TDto>().Add(dto);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync<TDto>(object update, object where) where TDto : class
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var predicate = BuildPredicate<TDto>(GetPropertyInformation(where));
        var matches = await context.Set<TDto>().Where(predicate).ToListAsync();

        foreach (var (name, value) in GetPropertyInformation(update))
        {
            var property = typeof(TDto).GetProperty(name)
                ?? throw new ApplicationException($"{typeof(TDto)} has no property named {name}");

            foreach (var match in matches)
                property.SetValue(match, value);
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync<TDto>(object example) where TDto : class
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var predicate = BuildPredicate<TDto>(GetPropertyInformation(example));
        var matches = await context.Set<TDto>().Where(predicate).ToListAsync();

        context.Set<TDto>().RemoveRange(matches);
        await context.SaveChangesAsync();
    }

    private static Expression<Func<TDto, bool>> BuildPredicate<TDto>(IReadOnlyDictionary<string, object?> example)
    {
        var parameter = Expression.Parameter(typeof(TDto), "x");
        Expression body = Expression.Constant(true);

        foreach (var (key, value) in example)
        {
            var property = Expression.Property(parameter, key);
            var constant = Expression.Constant(value, property.Type);
            body = Expression.AndAlso(body, Expression.Equal(property, constant));
        }

        return Expression.Lambda<Func<TDto, bool>>(body, parameter);
    }

    private static Dictionary<string, object?> GetPropertyInformation(object? example)
    {
        if (example == null)
            return new Dictionary<string, object?>();

        return example.GetType().GetProperties()
            .Where(p => p.GetMethod?.IsStatic != true)
            .ToDictionary(p => p.Name, p => p.GetValue(example));
    }
}
