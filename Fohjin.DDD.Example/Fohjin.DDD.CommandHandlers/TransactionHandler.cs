using Fohjin.DDD.Commands;
using Fohjin.DDD.Diagnostics;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Fohjin.DDD.CommandHandlers;

public class TransactionHandler<TCommand, TCommandHandler>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionHandler<TCommand, TCommandHandler>> log
        ) :
    ITransactionHandler<TCommand, TCommandHandler>
    where TCommandHandler : CommandHandlerBase<TCommand>
    where TCommand : class, ICommand
{
    private static readonly string CommandType = typeof(TCommand).Name;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger _log = log;

    public async Task ExecuteAsync(TCommand command, TCommandHandler commandHandler)
    {
        using var activity = Telemetry.ActivitySource.StartActivity($"command.execute {CommandType}");
        activity?.SetTag("cqrs.command.type", CommandType);
        activity?.SetTag("cqrs.command.handler.type", typeof(TCommandHandler).Name);
        var stopwatch = Stopwatch.StartNew();

        _log.LogInformation($"{nameof(ExecuteAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}", command, commandHandler);
        try
        {
            await commandHandler.ExecuteAsync(command);
            _log.LogInformation($"{nameof(ExecuteAsync)}-{nameof(_unitOfWork.CommitAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}", command, commandHandler);
            await _unitOfWork.CommitAsync();
            RecordOutcome("success", stopwatch.Elapsed, activity);
        }
        catch (Exception ex)
        {
            _log.LogError($"{nameof(ExecuteAsync)}-{nameof(_unitOfWork.RollbackAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}-{{{nameof(ex.Message)}}}", command, commandHandler, ex.Message);
            await _unitOfWork.RollbackAsync();
            RecordOutcome("failure", stopwatch.Elapsed, activity);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    public Task ExecuteAsync(object command, object commandHandler) =>
        ExecuteAsync((TCommand)command, (TCommandHandler)commandHandler);

    private static void RecordOutcome(string outcome, TimeSpan elapsed, Activity? activity)
    {
        var tags = new TagList { { "cqrs.command.type", CommandType }, { "cqrs.outcome", outcome } };
        Telemetry.CommandsHandled.Add(1, tags);
        Telemetry.CommandDuration.Record(elapsed.TotalSeconds, tags);
        activity?.SetTag("cqrs.outcome", outcome);
    }
}