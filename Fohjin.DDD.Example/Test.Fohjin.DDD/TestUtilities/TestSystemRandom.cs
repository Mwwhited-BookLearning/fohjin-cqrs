using Fohjin.DDD.Common;

namespace Test.Fohjin.DDD.TestUtilities;

public class TestSystemRandom(
     Func<int, int, int> rand) : ISystemRandom
{
    private readonly Func<int, int, int> _rand = rand;

    public int Next(int start, int end) => _rand(start, end);
}
