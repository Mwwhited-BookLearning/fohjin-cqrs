using Fohjin.DDD.Common;

namespace Fohjin.DDD.Tests.TestUtilities;

public class TestSystemTimer : ISystemTimer
{
    // Runs the callback synchronously/immediately (instead of after a real delay) so scenario
    // tests can assert on its side effects right after calling Send()/Trigger().
    public void Trigger(Func<Task> value, int @in) => value().GetAwaiter().GetResult();
}
