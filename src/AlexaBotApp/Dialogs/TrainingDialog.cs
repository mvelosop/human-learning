using AlexaBotApp.Bots;
using AlexaBotApp.Commands;
using AlexaBotApp.Metrics;
using Bot.Builder.Community.Adapters.Alexa;
using Bot.Builder.Community.Adapters.Alexa.Directives;
using MediatR;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Dialogs
{
    public class TrainingDialog : ComponentDialog
    {
        private readonly BotStateAccessors _accessors;
        private readonly ILogger<TrainingDialog> _logger;
        private readonly IMediator _mediator;
        private AlexaConversation _alexaConversation;
        private readonly BotConversation _monitorConversation;
        private readonly IConfiguration _configuration;
        private readonly IAdapterIntegration _botAdapter;

        public TrainingDialog(ILogger<TrainingDialog> logger, IMediator mediator, BotConversation monitorConversation, IConfiguration configuration,
            IAdapterIntegration botAdapter, BotStateAccessors accessors) : base(nameof(TrainingDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskForWordToSpeechTherapist,
                CreateTraining,
                AskForWordToUser,
                RegisterUtterance,
                //MonitorToSpeechTherapist,
                CheckPronounciation,
                Congratulate
            }));

            InitialDialogId = nameof(WaterfallDialog);

            _logger = logger;
            _mediator = mediator;
            _monitorConversation = monitorConversation;
            _configuration = configuration;
            _botAdapter = botAdapter;
            _accessors = accessors;
        }

        private async Task<DialogTurnResult> AskForWordToSpeechTherapist(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _alexaConversation = await _accessors.AlexaConversation.GetAsync(stepContext.Context, () => new AlexaConversation());
            _logger.LogInformation(@"----- Retrieved alexaConversation ({@AlexaConversation})", _alexaConversation);

            if (string.IsNullOrEmpty(_alexaConversation.Phrase))
            {
                var activity = MessageFactory.Text("¿Qué palabra vamos a trabajar? Dime la frase o palabra que quieras.");
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = activity }, CancellationToken.None);
            }
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> CreateTraining(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string phrase = stepContext.Result as string;
            if (phrase is string)
            {
                _alexaConversation.Phrase = phrase;
                _alexaConversation.Language = stepContext.Context.Activity.Locale;
                _alexaConversation.CurrentExercise = await _mediator.Send(new CreateExerciseCommand("José Manuel", _alexaConversation.Phrase, _alexaConversation.Language));
                _alexaConversation.Count = 0;
                _logger.LogInformation("----- Current exercise saved ({@AlexaConversation})", _alexaConversation);
                await _accessors.AlexaConversation.SetAsync(stepContext.Context, _alexaConversation);
            }
            return await stepContext.NextAsync(phrase);
        }

        private async Task<DialogTurnResult> AskForWordToUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string phrase)
            {
                var activity = MessageFactory.Text($@"Muy bien, vamos a trabajar con ""{phrase}"". A ver José Manuel, di ""{phrase}""!");
                PromptOptions prompOptions = new PromptOptions { Prompt = activity };
                prompOptions.RetryPrompt = MessageFactory.Text($@"José Manuel, di ""{_alexaConversation.Phrase}"", ¡ahora!");

                return await stepContext.PromptAsync(nameof(TextPrompt), prompOptions, CancellationToken.None);
            }
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> RegisterUtterance(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string recognizedPhrase)
            {
                _logger.LogInformation(@"----- Registering utterance ""{Utterance}"" for exercise ({@Exercise})", recognizedPhrase, _alexaConversation.CurrentExercise);
                Utterance utterance = await _mediator.Send(new RegisterUtteranceCommand(_alexaConversation.CurrentExercise.Id, stepContext.Context.Activity.Text));
            }
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> MonitorToSpeechTherapist(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is Utterance utterance)
            {
                // ** Echo user utterance to monitor

                var botAppId = string.IsNullOrEmpty(_configuration["MicrosoftAppId"]) ? "*" : _configuration["MicrosoftAppId"];

                // ** Send proactive message
                await _botAdapter.ContinueConversationAsync(botAppId, _monitorConversation.Reference, async (context, token) =>
                {
                    await context.SendActivityAsync(
                        $"User said ({stepContext.Context.Activity.Locale}):\n**{utterance.RecognizedPhrase}** => {utterance.Phonemes} (deviation: {utterance.PercentDeviation}%)");
                });
            }
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> CheckPronounciation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _alexaConversation.Count++;

            var correct = stepContext.Context.Activity.Text.Equals(_alexaConversation.Phrase, StringComparison.InvariantCultureIgnoreCase);
            var random = new Random();

            var resultMessage = correct
                ? $"{TrainingMessages.CorrectMessages[random.Next(0, TrainingMessages.CorrectMessages.Length - 1)]} Seguimos. Dime otra palabra o frase para trabajar."
                : $@"Hmmm, entendí: ""{stepContext.Context.Activity.Text}"". {TrainingMessages.TryAgainMessages[random.Next(0, TrainingMessages.TryAgainMessages.Length - 1)]}. Dime ""{_alexaConversation.Phrase}"" ahora!";

            if (correct)
            {
                DisplayDirective directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(imageUrl: "https://i.imgur.com/X06zAep.gif")
                };
                stepContext.Context.AlexaResponseDirectives().Add(directive);
                _alexaConversation.Phrase = null;
                _alexaConversation.CurrentExercise = null;
            }
            else
            {
                DisplayDirective directive = new DisplayDirective()
                {
                    Template = GenerateImageTemplate6(imageUrl: "https://i.imgur.com/JqVX0yM.png")
                };

                stepContext.Context.AlexaResponseDirectives().Add(directive);
            }

            var activity = MessageFactory.Text(resultMessage);
            await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = activity }, CancellationToken.None);
            return await stepContext.EndDialogAsync();
        }

        private Task<DialogTurnResult> Congratulate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private DisplayRenderBodyTemplate6 GenerateImageTemplate6(
            string imageUrl = "https://esalcedoost.blob.core.windows.net/public/background.png")
        {
            var displayTemplate = new DisplayRenderBodyTemplate6()
            {
                BackButton = BackButtonVisibility.HIDDEN //,
                //TextContent = new TextContent()
                //{
                //    PrimaryText = new InnerTextContent()
                //    {
                //        Text = text
                //    }
                //},
                //Token = "string",
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
