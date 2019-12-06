// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AlexaBotApp.Infrastructure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class AlexaBot : ActivityHandler
    {
        private readonly IAdapterIntegration _botAdapter;
        private readonly IConfiguration _configuration;
        private readonly ObjectLogger _objectLogger;
        private readonly BotConversation _conversation;

        public AlexaBot(
            ObjectLogger objectLogger,
            BotConversation conversation,
            IAdapterIntegration botAdapter,
            IConfiguration configuration)
        {
            _objectLogger = objectLogger;
            _conversation = conversation;
            _botAdapter = botAdapter;
            _configuration = configuration;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _objectLogger.LogObjectAsync(turnContext.Activity, turnContext.Activity.Id);

            await base.OnTurnAsync(turnContext, cancellationToken);
        }
        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                case "LaunchRequest":
                    await HandleLaunchRequestAsync(turnContext, cancellationToken);
                    return;
            }

            await turnContext.SendActivityAsync(
                $"Event received. Name: {turnContext.Activity.Name}. Value: {turnContext.Activity.Value}. Channel: {turnContext.Activity.ChannelId}");
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello world! Channel: {turnContext.Activity.ChannelId}"), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "alexa")
            {
                await EchoBackToBotFramework(turnContext);
            }
            else
            {
                // Save the conversation reference when the message doesn't come from Alexa
                _conversation.Reference = turnContext.Activity.GetConversationReference();
            }

            var echoMessage = $"Echo: {turnContext.Activity.Text} (from {turnContext.Activity.ChannelId}). Something else?";

            await turnContext.SendActivityAsync(MessageFactory.Text(echoMessage, inputHint: InputHints.ExpectingInput), cancellationToken);
        }

        private async Task EchoBackToBotFramework(ITurnContext<IMessageActivity> turnContext)
        {
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"Message to Alexa: \"{turnContext.Activity.Text}\"");
            });
        }

        private async Task HandleLaunchRequestAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Hola, soy Robotina, Cómo te llamas?", inputHint: InputHints.ExpectingInput));
        }
    }
}