using MediatR;
using Wallet.Application.Common;

namespace Wallet.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;