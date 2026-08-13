using Fohjin.DDD.Reporting.Dtos;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Fohjin.DDD.WebApi.OData;

public static class ODataModel
{
    public static IEdmModel Build()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<ClientReport>("Clients");
        builder.EntitySet<ClientDetailsReport>("ClientDetails");
        builder.EntitySet<AccountReport>("Accounts");
        builder.EntitySet<AccountDetailsReport>("AccountDetails");
        builder.EntitySet<LedgerReport>("Ledgers");
        builder.EntitySet<BankCardReport>("BankCards");

        // AllAccounts is the real EF navigation backing ClientDetailsReport.Accounts/ClosedAccounts
        // (two filtered views over it, see ClientDetailsReport.cs) - excluding it from the EDM
        // model keeps those two the only nested-route entry points into it, rather than a third,
        // unfiltered one appearing alongside them.
        builder.EntityType<ClientDetailsReport>().Ignore(x => x.AllAccounts);

        return builder.GetEdmModel();
    }
}
