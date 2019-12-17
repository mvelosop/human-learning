using System.Threading.Tasks;

namespace AlexaBotApp.Contracts
{
    public interface ICommandHandler<TCommand>
    {
        Task HandleAsync(TCommand command);
    }

    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }
}