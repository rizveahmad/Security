using MediatR;

namespace Security.Application.Common.Interfaces;

/// <summary>
/// Marker interface for CQRS commands that return a result.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Marker interface for CQRS commands with no return value.
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
/// Marker interface for CQRS queries.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Handler for a command that returns a result.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

/// <summary>
/// Handler for a command with no return value.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand { }

/// <summary>
/// Handler for a query.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
