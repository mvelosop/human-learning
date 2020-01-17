using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class BotStateAccessors
    {
        public UserState UserState { get; set; }

        public BotStateAccessors(UserState userState)
        {
            UserState = userState;

            AlexaConversation = UserState.CreateProperty<AlexaConversation>(nameof(AlexaConversation));
        }

        public IStatePropertyAccessor<AlexaConversation> AlexaConversation { get; }

        public async Task SaveChangesAsync(ITurnContext turnContext)
        {
            await UserState.SaveChangesAsync(turnContext);
        }
    }
}