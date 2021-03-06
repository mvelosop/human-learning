﻿using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class BotStateAccessors
    {
        private readonly UserState _userState;

        public BotStateAccessors(UserState userState)
        {
            _userState = userState;

            AlexaConversation = _userState.CreateProperty<AlexaConversation>(nameof(AlexaConversation));
        }

        public IStatePropertyAccessor<AlexaConversation> AlexaConversation { get; }

        public async Task SaveChangesAsync(ITurnContext turnContext)
        {
            await _userState.SaveChangesAsync(turnContext);
        }
    }
}