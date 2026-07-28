using Fohjin.DDD.Commands;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.CommandHandlers
{
    public class TransactionHandler<TCommand, TCommandHandler> :
        ITransactionHandler<TCommand, TCommandHandler>
        where TCommandHandler : CommandHandlerBase<TCommand>
        where TCommand : class, ICommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _log;

        public TransactionHandler(
            IUnitOfWork unitOfWork,
            ILogger<TransactionHandler<TCommand, TCommandHandler>> log
            )
        {
            _unitOfWork = unitOfWork;
            _log = log;
        }

        public async Task ExecuteAsync(TCommand command, TCommandHandler commandHandler)
        {
            _log.LogInformation($"{nameof(ExecuteAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}", command, commandHandler);
            try
            {
                await commandHandler.ExecuteAsync(command);
                _log.LogInformation($"{nameof(ExecuteAsync)}-{nameof(_unitOfWork.CommitAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}", command, commandHandler);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _log.LogError($"{nameof(ExecuteAsync)}-{nameof(_unitOfWork.RollbackAsync)}> {{{nameof(command)}}}, {{{nameof(commandHandler)}}}-{{{nameof(ex.Message)}}}", command, commandHandler, ex.Message);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public Task ExecuteAsync(object command, object commandHandler) =>
            ExecuteAsync((TCommand)command, (TCommandHandler)commandHandler);
    }
}