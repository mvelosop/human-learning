// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AlexaBotApp.Infrastructure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class AlexaBot : ActivityHandler
    {
        private static readonly string[] CorrectMessages =
        {
            "Excelente, eso es tuvo muy bien!",
            "Perfecto Jose, excelente!",
            "Buenísimo Jose, muy bien dicho!",
        };

        private static readonly string[] TryAgainMessages =
        {
            "Vamos a ver, inténtalo otra vez Jose.",
            "Ya falta poco Jose, prueba de nuevo.",
            "Casi casi, a ver Jose dilo una vez más.",
            "Ánimo Jose que tú lo puedes hacer.",
            "Un poco más Jose, seguro que ahora sí lo dices bien.",
        };

        private readonly BotStateAccessors _accessors;
        private readonly IAdapterIntegration _botAdapter;
        private readonly IConfiguration _configuration;
        private readonly BotConversation _conversation;
        private readonly ObjectLogger _objectLogger;

        public AlexaBot(
            ObjectLogger objectLogger,
            BotConversation conversation,
            IAdapterIntegration botAdapter,
            IConfiguration configuration,
            BotStateAccessors accessors)
        {
            _objectLogger = objectLogger;
            _conversation = conversation;
            _botAdapter = botAdapter;
            _configuration = configuration;
            _accessors = accessors;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _objectLogger.LogObjectAsync(turnContext.Activity, turnContext.Activity.Id);

            await base.OnTurnAsync(turnContext, cancellationToken);

            await _accessors.SaveChangesAsync(turnContext);
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

                if (turnContext.Activity.Text.Equals("adiós", StringComparison.InvariantCultureIgnoreCase))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Adiós Jose! Buenas noches."), cancellationToken);

                    return;
                }

                var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

                if (turnContext.Activity.Text.StartsWith("trabajar", StringComparison.InvariantCultureIgnoreCase))
                {
                    alexaConversation.Phrase = turnContext.Activity.Text.Substring(turnContext.Activity.Text.IndexOf(" ")).Trim();

                    await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);

                    await turnContext.SendActivityAsync(MessageFactory.Text($@"Muy bien, vamos a trabajar con ""{alexaConversation.Phrase}"". A ver Jose, di ""{alexaConversation.Phrase}""", inputHint: InputHints.ExpectingInput));

                    return;
                }

                var replyMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                    ? @"Necesito la frase o palabra que vamos a trabajar. Dime: ""Trabajar"", y luego la frase o palabra que quieras"
                    : GetResultMessage(turnContext, alexaConversation);

                var activity = MessageFactory.Text(replyMessage, inputHint: InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(activity, cancellationToken);
            }
            else
            {
                // Save the conversation reference when the message doesn't come from Alexa
                _conversation.Reference = turnContext.Activity.GetConversationReference();

                await turnContext.SendActivityAsync($@"Echo from AlexaBot: ""{turnContext.Activity.Text}""");
            }

        }

        private async Task EchoBackToBotFramework(ITurnContext<IMessageActivity> turnContext)
        {
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"Enviado a Alexa: \"{turnContext.Activity.Text}\"");
            });
        }

        private string GetResultMessage(ITurnContext<IMessageActivity> turnContext, AlexaConversation alexaConversation)
        {
            var correct = turnContext.Activity.Text.Equals(alexaConversation.Phrase, StringComparison.InvariantCultureIgnoreCase);
            var random = new Random();

            var resultMessage = correct
                ? $"{CorrectMessages[random.Next(0, CorrectMessages.Length - 1)]} Ahora dime otra palabra o frase para trabajar."
                : $@"Hmmm, entendí: ""{turnContext.Activity.Text}"". {TryAgainMessages[random.Next(0, TryAgainMessages.Length - 1)]}. Dime ""{alexaConversation.Phrase}""";

            if (correct)
            {
                alexaConversation.Phrase = null;
            }

            return resultMessage;
        }

        private async Task HandleLaunchRequestAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            var greetingMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                ? "Hola, soy Robotina, tienes que decirme qué frase o palabra vamos a trabajar"
                : $@"Hola, soy Robotina, estamos trabajando la {(alexaConversation.Phrase.Contains(" ") ? "frase" : "palabra")} ""{alexaConversation.Phrase}"". A ver Jose, di ""{alexaConversation.Phrase}""";

            await turnContext.SendActivityAsync(MessageFactory.Text(greetingMessage, inputHint: InputHints.ExpectingInput));
        }
    }
}