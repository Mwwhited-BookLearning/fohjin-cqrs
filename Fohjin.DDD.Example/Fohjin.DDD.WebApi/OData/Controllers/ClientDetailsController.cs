using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Fohjin.DDD.WebApi.OData.Controllers;

// Nested/contained OData routes under ClientDetails, e.g.
// /odata/ClientDetails(00000000-0000-0000-0000-000000000000)/Accounts?$filter=...
// Matched purely by OData routing convention (Microsoft.AspNetCore.OData.Routing.Conventions.
// NavigationRoutingConvention): controller name "ClientDetails" -> the "ClientDetails" entity
// set (ODataModel.cs), action "Get{NavigationProperty}" -> the matching navigation property on
// ClientDetailsReport. No attribute routing needed or wanted here - [EnableQuery]'s own
// OData-JSON formatter (full $select/$expand/$top/$skip/$count) is fine at this level since
// nothing pins these routes' response shape yet, unlike the bare-JSON-array top-level entity
// sets (MapODataEntitySet<TDto>) that ODataClientsEndpointTest.cs pins. Authorization comes
// from Program.cs's app.MapControllers().RequireAuthorization(), not a local [Authorize].
public class ClientDetailsController(IReportingRepository repository) : ODataController
{
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<AccountReport> GetAccounts([FromRoute] Guid key) =>
        repository.Query<ClientDetailsReport>()
            .Where(x => x.Id == key)
            .SelectMany(x => x.AllAccounts)
            .Where(a => a.Status != "Closed");

    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<AccountReport> GetClosedAccounts([FromRoute] Guid key) =>
        repository.Query<ClientDetailsReport>()
            .Where(x => x.Id == key)
            .SelectMany(x => x.AllAccounts)
            .Where(a => a.Status == "Closed");

    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<BankCardReport> GetBankCards([FromRoute] Guid key) =>
        repository.Query<ClientDetailsReport>()
            .Where(x => x.Id == key)
            .SelectMany(x => x.BankCards);
}
