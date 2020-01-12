using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using MediatR;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Alexa;
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
        private readonly IMediator _mediator;
        private readonly ObjectLogger _objectLogger;

        public AlexaBot(
            ObjectLogger objectLogger,
            BotConversation conversation,
            IAdapterIntegration botAdapter,
            IConfiguration configuration,
            BotStateAccessors accessors,
            IMediator mediator,
            ILogger<AlexaBot> logger)
        {
            _objectLogger = objectLogger;
            _conversation = conversation;
            _botAdapter = botAdapter;
            _configuration = configuration;
            _accessors = accessors;
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _objectLogger.LogObjectAsync(turnContext.Activity, turnContext.Activity.Id);

            switch (turnContext.Activity.Type)
            {
                case "LaunchRequest":
                    await HandleLaunchRequestAsync(turnContext, cancellationToken);
                    return;

                case "StopIntent":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Terminando la sesión", inputHint: InputHints.IgnoringInput));
                    return;
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // ** Save bot state changes
            await _accessors.SaveChangesAsync(turnContext);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            // ** Echo event information to monitor bot
            await EchoEventAsync(turnContext);

            switch (turnContext.Activity.Name)
            {
                case "LaunchRequest":
                    await HandleLaunchRequestAsync(turnContext, cancellationToken);
                    return;

                case "StopIntent":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Terminando la sesión", inputHint: InputHints.IgnoringInput));
                    return;
            }

            // ** Speak back any other event
            await turnContext.SendActivityAsync(
                $"Event received: {turnContext.Activity.Name}");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var message = turnContext.Activity.Text.ToLower();
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            _logger.LogInformation(@"----- Retrieved alexaConversation ({@AlexaConversation})", alexaConversation);

            // ** Handle goodbye message
            if (message == "adiós")
            {
                // ** Echo user message to monitor
                await EchoUserMessageAsync(turnContext);

                await turnContext.SendActivityAsync(MessageFactory.Text("Adiós José Manuel! Buenas noches."), cancellationToken);

                await ClearConversationAsync(turnContext, "end");

                return;
            }

            if (message == "pausa")
            {
                // ** Echo user message to monitor
                await EchoUserMessageAsync(turnContext);

                await turnContext.SendActivityAsync(MessageFactory.Text("Muy bien, me pongo en pausa y seguimos luego."), cancellationToken);

                return;
            }

            var commandConfirmation = string.Empty;

            if (message == "cambiar palabra")
            {
                // ** Echo user message to monitor
                await EchoUserMessageAsync(turnContext);

                await ClearConversationAsync(turnContext, "end");

                commandConfirmation = "Acabo de dar por terminado el ejercicio, así que ";
            }

            if (message == "eliminar palabra")
            {
                // ** Echo user message to monitor
                await EchoUserMessageAsync(turnContext);

                await ClearConversationAsync(turnContext, "delete");

                commandConfirmation = "Acabo de eliminar el ejercicio, así que ";
            }

            if (message.StartsWith(newTargetPhraseUtterance))
            {
                // ** Echo user message to monitor
                await EchoUserMessageAsync(turnContext);

                alexaConversation.Phrase = GetTargetPhrase(message);
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
                _logger.LogInformation(@"----- Registering utterance ""{Utterance}"" for exercise ({@Exercise})", message, alexaConversation.CurrentExercise);

                var utterance = await RegisterUtteranceAsync(alexaConversation.CurrentExercise, message);

                // ** Echo user utterance to monitor
                await EchoUserUtteranceAsync(turnContext, utterance);

                replyMessage = GetResultMessage(turnContext, alexaConversation);

                await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);
            }

            var activity = MessageFactory.Text(replyMessage, inputHint: InputHints.ExpectingInput);
            await turnContext.SendActivityAsync(activity, cancellationToken);
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

        private async Task<Exercise> CreateExerciseAsync(AlexaConversation alexaConversation)
        {
            return await _mediator.Send(new CreateExerciseCommand("José Manuel", alexaConversation.Phrase, alexaConversation.Language));
        }

        private async Task DeleteExerciseAsync(int id)
        {
            await _mediator.Send(new DeleteExerciseCommand(id));
        }

        private async Task EchoEventAsync(ITurnContext<IEventActivity> turnContext)
        {
            // ** Nothing to do if no conversation reference
            if (_conversation.Reference == null) return;

            var eventValue = JsonConvert.SerializeObject(turnContext.Activity.Value, Formatting.Indented);
            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            // ** Send proactive message
            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"Event received:\n```\n{eventValue}\n```");
            });
        }

        private async Task EchoUserMessageAsync(ITurnContext<IMessageActivity> turnContext)
        {
            // ** Nothing to do if no conversation reference
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            // ** Send proactive message
            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"User said ({turnContext.Activity.Locale}):\n**{turnContext.Activity.Text.ToLowerInvariant()}**");
            });
        }

        private async Task EchoUserUtteranceAsync(ITurnContext<IMessageActivity> turnContext, Utterance utterance)
        {
            // ** Nothing to do if no conversation reference
            if (_conversation.Reference == null) return;

            var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

            // ** Send proactive message
            await _botAdapter.ContinueConversationAsync(botAppId, _conversation.Reference, async (context, token) =>
            {
                await context.SendActivityAsync($"User said ({turnContext.Activity.Locale}):\n**{utterance.RecognizedPhrase}** => {utterance.Phonemes} (deviation: {utterance.PercentDeviation}%)");
            });
        }

        private async Task EndExerciseAsync(int id)
        {
            await _mediator.Send(new EndExerciseCommand(id));
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

        private async Task HandleLaunchRequestAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            var greetingMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                ? "Hola, soy tu logopeda virtual, tienes que decirme qué frase o palabra vamos a trabajar"
                : $@"Hola, continuamos trabajando la {(alexaConversation.Phrase.Contains(" ") ? "frase" : "palabra")} ""{alexaConversation.Phrase}"". A ver José Manuel, dime ""{alexaConversation.Phrase}""";

            await turnContext.SendActivityAsync(MessageFactory.Text(greetingMessage, inputHint: InputHints.ExpectingInput));
        }

        private async Task<Utterance> RegisterUtteranceAsync(Exercise currentExercise, string recognizedPhrase)
        {
            return await _mediator.Send(new RegisterUtteranceCommand(currentExercise.Id, recognizedPhrase));
        }
    }
}