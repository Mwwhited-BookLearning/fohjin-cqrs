using Fohjin.DDD.Common;

namespace Fohjin.DDD.Tests.TestUtilities;

public class TestSystemDateTime(
     DateTimeOffset now) : ISystemDateTime
{
    private readonly DateTimeOffset _now = now;

    public DateTimeOffset Now() => _now;
}
