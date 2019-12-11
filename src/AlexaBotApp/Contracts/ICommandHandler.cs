using System.Threading.Tasks;

namespace AlexaBotApp.Contracts
{
    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }
}