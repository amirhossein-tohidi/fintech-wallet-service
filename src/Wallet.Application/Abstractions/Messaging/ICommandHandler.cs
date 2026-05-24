using MediatR;
using Wallet.Application.Common;

namespace Wallet.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> 
    where TCommand : ICommand;