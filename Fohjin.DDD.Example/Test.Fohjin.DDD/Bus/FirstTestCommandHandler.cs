using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;

namespace Test.Fohjin.DDD.Bus
{
    public class FirstTestCommandHandler : CommandHandlerBase<TestCommand>
    {
        public List<Guid> Ids;
        public readonly SemaphoreSlim Signal = new(0);

        public FirstTestCommandHandler()
        {
            Ids = new List<Guid>();
        }

        public override Task ExecuteAsync(TestCommand compensatingCommand)
        {
            Ids.Add(compensatingCommand.Id);
            Signal.Release();
            return Task.CompletedTask;
        }
    }
}
