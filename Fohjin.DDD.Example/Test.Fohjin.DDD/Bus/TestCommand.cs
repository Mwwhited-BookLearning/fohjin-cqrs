using Fohjin.DDD.Commands;

namespace Test.Fohjin.DDD.Bus;

public record TestCommand(Guid Id) : CommandBase(Id);
