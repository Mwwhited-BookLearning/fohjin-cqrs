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
        return builder.GetEdmModel();
    }
}
