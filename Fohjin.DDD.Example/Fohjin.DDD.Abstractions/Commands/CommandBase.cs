namespace Fohjin.DDD.Commands;

public abstract record CommandBase(Guid Id) : ICommand;
