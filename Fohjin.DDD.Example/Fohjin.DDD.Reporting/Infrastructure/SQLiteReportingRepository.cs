using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Fohjin.DDD.Reporting.Infrastructure
{
    public class SqliteReportingRepository : IReportingRepository
    {
        private readonly IDbContextFactory<ReportingDbContext> _dbContextFactory;

        public SqliteReportingRepository(IDbContextFactory<ReportingDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<TDto>> GetByExampleAsync<TDto>(object? example) where TDto : class
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var predicate = BuildPredicate<TDto>(GetPropertyInformation(example));
            var dtos = await context.Set<TDto>().Where(predicate).ToListAsync();

            await LoadChildrenAsync(context, dtos);

            return dtos;
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

        // Child collections (e.g. ClientDetailsReport.Accounts, AccountDetailsReport.Ledgers) aren't
        // modeled as EF navigations (see ReportingDbContext) - they're loaded here with a follow-up
        // query per the same "{ParentTypeName}Id" convention the original ADO.NET implementation used.
        private async Task LoadChildrenAsync<TDto>(ReportingDbContext context, List<TDto> dtos) where TDto : class
        {
            var idProperty = typeof(TDto).GetProperty("Id");
            if (idProperty == null)
                return;

            foreach (var property in typeof(TDto).GetProperties().Where(IsChildCollection))
            {
                var childDtoType = property.PropertyType.GetGenericArguments().First();
                var fkPropertyName = $"{typeof(TDto).Name}Id";

                foreach (var dto in dtos)
                {
                    var parentId = idProperty.GetValue(dto);
                    if (parentId == null)
                        continue;

                    var task = (Task)GetType()
                        .GetMethod(nameof(GetChildrenOfTypeAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(childDtoType)
                        .Invoke(this, new object[] { context, fkPropertyName, parentId })!;

                    await task;
                    var children = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
                    property.SetValue(dto, children);
                }
            }
        }

        private static async Task<List<TChild>> GetChildrenOfTypeAsync<TChild>(ReportingDbContext context, string fkPropertyName, object parentId) where TChild : class
        {
            var predicate = BuildPredicate<TChild>(new Dictionary<string, object?> { [fkPropertyName] = parentId });
            return await context.Set<TChild>().Where(predicate).ToListAsync();
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

        private static bool IsChildCollection(PropertyInfo propertyInfo) => propertyInfo.PropertyType.IsGenericType;
    }
}
