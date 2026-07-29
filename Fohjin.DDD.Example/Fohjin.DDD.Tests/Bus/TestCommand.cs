using Fohjin.DDD.Commands;

namespace Fohjin.DDD.Tests.Bus;

public record TestCommand(Guid Id) : CommandBase(Id);
