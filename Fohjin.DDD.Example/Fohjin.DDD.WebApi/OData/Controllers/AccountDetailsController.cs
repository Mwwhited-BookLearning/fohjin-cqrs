using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Fohjin.DDD.WebApi.OData.Controllers;

// Nested/contained OData route under AccountDetails, e.g.
// /odata/AccountDetails(00000000-0000-0000-0000-000000000000)/Ledgers?$filter=...
// See ClientDetailsController for the routing-convention explanation. Authorization comes from
// Program.cs's app.MapControllers().RequireAuthorization(), not a local [Authorize].
public class AccountDetailsController(IReportingRepository repository) : ODataController
{
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<LedgerReport> GetLedgers([FromRoute] Guid key) =>
        repository.Query<AccountDetailsReport>()
            .Where(x => x.Id == key)
            .SelectMany(x => x.Ledgers);
}
