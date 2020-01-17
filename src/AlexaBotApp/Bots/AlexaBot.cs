using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using Bot.Builder.Community.Adapters.Alexa.Directives;
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
using AlexaBotApp.Alexa;
using System.Collections.Generic;

namespace AlexaBotApp.Bots
{
    public class AlexaBot : AlexaActivityHandler
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

        public AlexaBot(ObjectLogger objectLogger, BotConversation conversation, IAdapterIntegration botAdapter,
                        IConfiguration configuration, BotStateAccessors accessors, IMediator mediator,
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

            await base.OnTurnAsync(turnContext, cancellationToken);

            // ** Save bot state changes
            await _accessors.SaveChangesAsync(turnContext);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            // ** Echo event information to monitor bot
            await EchoEventAsync(turnContext);

            // ** Speak back any other event
            await turnContext.SendActivityAsync(
                $"Event received: {turnContext.Activity.Name}");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            _logger.LogInformation(@"----- Retrieved alexaConversation ({@AlexaConversation})", alexaConversation);

            var replyMessage = string.Empty;

            if (string.IsNullOrEmpty(alexaConversation.Phrase))
            {
                replyMessage = "¿Qué palabra vamos a trabajar? Dime la frase o palabra que quieras.";
            }
            else
            {
                Utterance utterance = await RegisterUtteranceAsync(alexaConversation.CurrentExercise, turnContext.Activity.Text);

                // ** Echo user utterance to monitor
                await EchoUserUtteranceAsync(turnContext, utterance);

                replyMessage = GetResultMessage(turnContext, alexaConversation);
                await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);

                DisplayDirective directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(text: alexaConversation.Phrase)
                };

                turnContext.AlexaResponseDirectives().Add(directive);
            }
            
            var activity = MessageFactory.Text(replyMessage, inputHint: InputHints.ExpectingInput);
            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        protected override async Task OnCreateTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation =  new AlexaConversation();

            // ** Echo user message to monitor
            await EchoUserMessageAsync(turnContext);

            alexaConversation.Phrase = GetTargetPhrase(turnContext.Activity.Text);
            alexaConversation.Language = turnContext.Activity.Locale;

            alexaConversation.CurrentExercise = await CreateExerciseAsync(alexaConversation);
            alexaConversation.Count = 0;

            _logger.LogInformation("----- Current exercise saved ({@AlexaConversation})", alexaConversation);

            await _accessors.AlexaConversation.SetAsync(turnContext, alexaConversation);

            DisplayDirective directive1 = new DisplayDirective() { Template = GenerateImageTemplate6() };

            turnContext.AlexaResponseDirectives().Add(directive1);
            await turnContext.SendActivityAsync(MessageFactory.Text($@"Muy bien, vamos a trabajar con ""{alexaConversation.Phrase}"". A ver José Manuel, di ""{alexaConversation.Phrase}"" ahora!", inputHint: InputHints.ExpectingInput));
        }

        protected override async Task OnCancelationIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await EchoUserMessageAsync(turnContext);
            await ClearConversationAsync(turnContext, "end");
            await turnContext.SendActivityAsync(MessageFactory.Text("Adiós José Manuel! Buenas noches."), cancellationToken);
        }

        protected override async Task OnDeleteTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // ** Echo user message to monitor
            await EchoUserMessageAsync(turnContext);
            await ClearConversationAsync(turnContext, "delete");
            await turnContext.SendActivityAsync(MessageFactory.Text("Acabo de eliminar el ejercicio, dime una nueva palabra para trabajar"), cancellationToken);
            // TODO redirect to training dialog
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
                DisplayDirective directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(imageUrl: "https://i.imgur.com/X06zAep.gif")
                };
                turnContext.AlexaResponseDirectives().Add(directive);
                directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(imageUrl: "https://i.imgur.com/Se9oYdf.png")
                };
                turnContext.AlexaResponseDirectives().Add(directive);
                alexaConversation.Phrase = null;
                alexaConversation.CurrentExercise = null;
            }
            else
            {
                DisplayDirective directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(imageUrl: "https://i.imgur.com/JqVX0yM.png")
                };

                turnContext.AlexaResponseDirectives().Add(directive);
            }
            return resultMessage;
        }

        private string GetTargetPhrase(string text)
        {
            if (text.StartsWith(newTargetPhraseUtterance, StringComparison.InvariantCultureIgnoreCase) && text.Length == newTargetPhraseUtterance.Length) return null;

            return text.Substring(newTargetPhraseUtterance.Length).Trim();
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            var greetingMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                ? "Hola, soy tu logopeda virtual, tienes que decirme qué frase o palabra vamos a trabajar"
                : $@"Hola, continuamos trabajando la {(alexaConversation.Phrase.Contains(" ") ? "frase" : "palabra")} ""{alexaConversation.Phrase}"". A ver José Manuel, dime ""{alexaConversation.Phrase}""";

            DisplayDirective directive = new DisplayDirective()
            {
                Template = GenerateImageTemplate6(text: "Human Learning", imageUrl: "~/media/greetings.png")
            };

            turnContext.AlexaResponseDirectives().Add(directive);
            await turnContext.SendActivityAsync(greetingMessage, cancellationToken: cancellationToken);
        }

        private async Task<Utterance> RegisterUtteranceAsync(Exercise currentExercise, string recognizedPhrase)
        {
            _logger.LogInformation(@"----- Registering utterance ""{Utterance}"" for exercise ({@Exercise})", recognizedPhrase, currentExercise);
            return await _mediator.Send(new RegisterUtteranceCommand(currentExercise.Id, recognizedPhrase));
        }

        private DisplayRenderBodyTemplate6 GenerateImageTemplate6(
            string text = "Human learning con Alexa",
            string imageUrl = "")
        {
            var displayTemplate = new DisplayRenderBodyTemplate6()
            {
                BackButton = BackButtonVisibility.HIDDEN,
                TextContent = new TextContent()
                {
                    PrimaryText = new InnerTextContent()
                    {
                        Text = text
                    }
                },
                Token = "string",
            };
            displayTemplate.BackgroundImage = new Image()
            {
                ContentDescription = "background",
                Sources = new ImageSource[]
                    {
                        new ImageSource()
                        {
                            Url = imageUrl
                        }
                    }
            };
            return displayTemplate;
        }
    }
}