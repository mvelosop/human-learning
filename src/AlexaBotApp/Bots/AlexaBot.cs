// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AlexaBotApp.Commands;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Bots
{
    public class AlexaBot : ActivityHandler
    {
        private const string newTargetPhraseUtterance = "trabajar";

        private static readonly string[] CorrectMessages =
        {
            "Excelente, eso estuvo muy bien!",
            "Perfecto José Manuel, excelente!",
            "Buenísimo José Manuel, muy bien dicho!",
        };

        private static readonly string[] TryAgainMessages =
        {
            "Una vez más.",
            "Prueba de nuevo.",
            "Intenta otra vez.",
            "Vamos, tú puedes.",
            "Un poco más rápido.",
            "Un poco más seguido.",
            "Otra vez.",
        };

        private readonly BotStateAccessors _accessors;
        private readonly IAdapterIntegration _botAdapter;
        private readonly IConfiguration _configuration;
        private readonly BotConversation _conversation;
        private readonly ILogger<AlexaBot> _logger;
        private readonly ObjectLogger _objectLogger;
        private readonly IServiceProvider _serviceProvider;

        public AlexaBot(
            ObjectLogger objectLogger,
            BotConversation conversation,
            IAdapterIntegration botAdapter,
            IConfiguration configuration,
            BotStateAccessors accessors,
            IServiceProvider serviceProvider,
            ILogger<AlexaBot> logger)
        {
            _objectLogger = objectLogger;
            _conversation = conversation;
            _botAdapter = botAdapter;
            _configuration = configuration;
            _accessors = accessors;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _objectLogger.LogObjectAsync(turnContext.Activity, turnContext.Activity.Id);

            await base.OnTurnAsync(turnContext, cancellationToken);

            await _accessors.SaveChangesAsync(turnContext);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            await EchoBackToBotFramework(turnContext);

            switch (turnContext.Activity.Name)
            {
                case "LaunchRequest":
                    await HandleLaunchRequestAsync(turnContext, cancellationToken);
                    return;

                case "StopIntent":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Terminando la sesión", inputHint: InputHints.IgnoringInput));
                    return;
            }

            await turnContext.SendActivityAsync(
                $"Event received. Channel: {turnContext.Activity.ChannelId}, Name: {turnContext.Activity.Name}");
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
                var commandConfirmation = string.Empty;

                await EchoBackToBotFramework(turnContext);

                if (turnContext.Activity.Text.Equals("adiós", StringComparison.InvariantCultureIgnoreCase))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Adiós José Manuel! Buenas noches."), cancellationToken);

                    await ClearConversationAsync(turnContext, "end");

                    return;
                }

                if (turnContext.Activity.Text.Equals("pausa", StringComparison.InvariantCultureIgnoreCase))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Muy bien, me pongo en pausa y seguimos luego."), cancellationToken);

                    return;
                }

                if (turnContext.Activity.Text.Equals("cambiar palabra", StringComparison.CurrentCultureIgnoreCase))
                {
                    await ClearConversationAsync(turnContext, "end");

                    commandConfirmation = "Acabo de dar por terminado el ejercicio, así que ";
                }

                if (turnContext.Activity.Text.Equals("eliminar palabra", StringComparison.CurrentCultureIgnoreCase))
                {
                    await ClearConversationAsync(turnContext, "delete");

                    commandConfirmation = "Acabo de eliminar el ejercicio, así que ";
                }

                var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

                _logger.LogInformation(@"----- Retrieved alexaConversation ({@AlexaConversation})", alexaConversation);

                if (turnContext.Activity.Text.StartsWith(newTargetPhraseUtterance, StringComparison.InvariantCultureIgnoreCase))
                {
                    alexaConversation.Phrase = GetTargetPhrase(turnContext.Activity.Text);
                    alexaConversation.Language = turnContext.Activity.Locale;

                    if (!string.IsNullOrWhiteSpace(alexaConversation.Phrase))
                    {
                        alexaConversation.CurrentExercise = await CreateExerciseAsync(alexaConversation);
                        alexaConversation.Count = 0;

                        _logger.LogInformation("----- Current exercise saved ({@AlexaConversation})", alexaConversation);

                        await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);

                        await turnContext.SendActivityAsync(MessageFactory.Text($@"Muy bien, vamos a trabajar con ""{alexaConversation.Phrase}"". A ver José Manuel, di ""{alexaConversation.Phrase}"" ahora!", inputHint: InputHints.ExpectingInput));

                        return;
                    }
                }

                var replyMessage = string.Empty;

                if (string.IsNullOrEmpty(alexaConversation.Phrase))
                {
                    replyMessage = $@"{commandConfirmation} necesito saber qué otra palabra vamos a trabajar. Dime, ""Trabajar"", y luego la frase o palabra que quieras.";
                }
                else
                {
                    _logger.LogInformation(@"----- Registering utterance ""{Utterance}"" for exercise ({@Exercise})", turnContext.Activity.Text, alexaConversation.CurrentExercise);

                    await RegisterUtteranceAsync(alexaConversation.CurrentExercise, turnContext.Activity.Text);

                    replyMessage = GetResultMessage(turnContext, alexaConversation);

                    await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);
                }

                var activity = MessageFactory.Text(replyMessage, inputHint: InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(activity, cancellationToken);
            }
            else
            {
                if (turnContext.Activity.Text.Equals("monitor alexa", StringComparison.OrdinalIgnoreCase))
                {
                    // Save the conversation reference when the message doesn't come from Alexa
                    _conversation.Reference = turnContext.Activity.GetConversationReference();
                    await turnContext.SendActivityAsync($@"Alexa monitor is on");
                }
                else
                {
                    await turnContext.SendActivityAsync($"Echo from AlexaBot: **{turnContext.Activity.Text}**");
                }
            }
        }

        private async Task ClearConversationAsync(ITurnContext<IMessageActivity> turnContext, string endAction)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            if (alexaConversation.CurrentExercise != null)
            {
                switch (endAction)
                {
                    case "end":
                        await EndExerciseAsync(alexaConversation.CurrentExercise.Id);
                        break;

                    case "delete":
                        await DeleteExerciseAsync(alexaConversation.CurrentExercise.Id);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown endAction: {endAction}");
                }
            }

            alexaConversation.Phrase = null;
            alexaConversation.CurrentExercise = null;
            alexaConversation.Count = 0;

            await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);
        }

        private async Task<PhraseExercise> CreateExerciseAsync(AlexaConversation alexaConversation)
        {
            var command = new CreatePhraseExerciseCommand(alexaConversation.Phrase, alexaConversation.Language);
            var handler = _serviceProvider.GetService<ICommandHandler<CreatePhraseExerciseCommand, PhraseExercise>>();

            return await handler.HandleAsync(command);
        }

        private async Task DeleteExerciseAsync(int id)
        {
            var command = new DeleteExerciseCommand(id);
            var handler = _serviceProvider.GetService<ICommandHandler<DeleteExerciseCommand>>();

            await handler.HandleAsync(command);
        }

        private async Task EchoBackToBotFramework(ITurnContext<IEventActivity> turnContext)
        {
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            var eventValue = JsonConvert.SerializeObject(turnContext.Activity.Value, Formatting.Indented);

            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"Event received:\n```\n{eventValue}\n```");
            });
        }

        private async Task EchoBackToBotFramework(ITurnContext<IMessageActivity> turnContext)
        {
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"Message received ({turnContext.Activity.Locale}):\n**{turnContext.Activity.Text}**");
            });
        }

        private async Task EndExerciseAsync(int id)
        {
            var command = new EndExerciseCommand(id);
            var handler = _serviceProvider.GetService<ICommandHandler<EndExerciseCommand, PhraseExercise>>();

            await handler.HandleAsync(command);
        }

        private string GetResultMessage(ITurnContext<IMessageActivity> turnContext, AlexaConversation alexaConversation)
        {
            alexaConversation.Count++;

            var correct = turnContext.Activity.Text.Equals(alexaConversation.Phrase, StringComparison.InvariantCultureIgnoreCase);
            var random = new Random();

            var resultMessage = correct
                ? $"{CorrectMessages[random.Next(0, CorrectMessages.Length - 1)]} Seguimos. Dime otra palabra o frase para trabajar."
                : $@"Hmmm, entendí: ""{turnContext.Activity.Text}"". {TryAgainMessages[random.Next(0, TryAgainMessages.Length - 1)]}. Dime ""{alexaConversation.Phrase}"" ahora!";

            if (correct)
            {
                alexaConversation.Phrase = null;
                alexaConversation.CurrentExercise = null;
            }

            return resultMessage;
        }

        private string GetTargetPhrase(string text)
        {
            if (text.StartsWith(newTargetPhraseUtterance, StringComparison.InvariantCultureIgnoreCase) && text.Length == newTargetPhraseUtterance.Length) return null;

            return text.Substring(newTargetPhraseUtterance.Length).Trim();
        }

        private async Task HandleLaunchRequestAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            var greetingMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                ? "Hola, soy tu logopeda virtual, tienes que decirme qué frase o palabra vamos a trabajar"
                : $@"Hola, continuamos trabajando la {(alexaConversation.Phrase.Contains(" ") ? "frase" : "palabra")} ""{alexaConversation.Phrase}"". A ver José Manuel, dime ""{alexaConversation.Phrase}""";

            await turnContext.SendActivityAsync(MessageFactory.Text(greetingMessage, inputHint: InputHints.ExpectingInput));
        }

        private async Task RegisterUtteranceAsync(PhraseExercise currentExercise, string recognizedPhrase)
        {
            var command = new RegisterUtteranceCommand(currentExercise.Id, recognizedPhrase);
            var handler = _serviceProvider.GetService<ICommandHandler<RegisterUtteranceCommand, PhraseExercise>>();

            await handler.HandleAsync(command);
        }
    }
}