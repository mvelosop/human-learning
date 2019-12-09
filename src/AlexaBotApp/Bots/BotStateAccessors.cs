using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class BotStateAccessors
    {
        private readonly ConversationState _conversationState;

        public BotStateAccessors(ConversationState conversationState)
        {
            _conversationState = conversationState;

            AlexaConversation = _conversationState.CreateProperty<AlexaConversation>(nameof(AlexaConversation));
        }

        public IStatePropertyAccessor<AlexaConversation> AlexaConversation { get; }

        public async Task SaveChangesAsync(ITurnContext turnContext)
        {
            await _conversationState.SaveChangesAsync(turnContext);
        }
    }
}