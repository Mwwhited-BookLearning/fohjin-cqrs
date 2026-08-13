using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;

namespace Fohjin.DDD.Tests.Bus;

public class FirstTestCommandHandler : CommandHandlerBase<TestCommand>
{
    public List<Guid> Ids;
    public readonly SemaphoreSlim Signal = new(0);

    // Opt-in, defaults false - All_commands_must_have_a_handler.cs reflects over every
    // concrete CommandHandlerBase<TestCommand> in the assembly and constructs it with
    // ActivatorUtilities (no property setters run), so a dedicated throwing handler class
    // would get swept into that smoke test and fail it. This property lets a test opt a
    // specific instance into throwing without adding a second discoverable handler type.
    public bool ShouldThrow { get; set; }

    public FirstTestCommandHandler()
    {
        Ids = [];
    }

    public override Task ExecuteAsync(TestCommand compensatingCommand)
    {
        if (ShouldThrow)
            throw new InvalidOperationException("boom");

        Ids.Add(compensatingCommand.Id);
        Signal.Release();
        return Task.CompletedTask;
    }
}
