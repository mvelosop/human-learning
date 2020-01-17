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
using Microsoft.Bot.Builder.Dialogs;

namespace AlexaBotApp.Bots
{
    public class AlexaBot<T> : AlexaActivityHandler where T : Dialog
    {
        private readonly BotStateAccessors _accessors;
        private readonly IAdapterIntegration _botAdapter;
        private readonly IConfiguration _configuration;
        private readonly BotConversation _conversation;
        private readonly ILogger<AlexaBot<T>> _logger;
        private readonly IMediator _mediator;
        private readonly ObjectLogger _objectLogger;
        private readonly T _dialog;
        private readonly BotState _conversationState;

        public AlexaBot(ObjectLogger objectLogger, BotConversation conversation, IAdapterIntegration botAdapter,
                        IConfiguration configuration, BotStateAccessors accessors, IMediator mediator,
                        ILogger<AlexaBot<T>> logger, T dialog, ConversationState conversationState)
        {
            _objectLogger = objectLogger;
            _conversation = conversation;
            _botAdapter = botAdapter;
            _configuration = configuration;
            _accessors = accessors;
            _mediator = mediator;
            _logger = logger;
            _dialog = dialog;
            _conversationState = conversationState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _objectLogger.LogObjectAsync(turnContext.Activity, turnContext.Activity.Id);

            await base.OnTurnAsync(turnContext, cancellationToken);

            // ** Save bot state changes
            await _accessors.SaveChangesAsync(turnContext);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
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
            DialogSet dialogSet = new DialogSet(_conversationState.CreateProperty<DialogState>("TrainingDialogState"));
            dialogSet.Add(_dialog);

            DialogContext dialogContext =
                await dialogSet.CreateContextAsync(turnContext, cancellationToken);

            DialogTurnResult results =
                await dialogContext.ContinueDialogAsync(cancellationToken);

            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(_dialog.Id, null, cancellationToken);
            }
        }

        protected override async Task OnTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
        
        private async Task EndExerciseAsync(int id)
        {
            await _mediator.Send(new EndExerciseCommand(id));
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var alexaConversation = await _accessors.AlexaConversation.GetAsync(turnContext, () => new AlexaConversation());

            var greetingMessage = string.IsNullOrEmpty(alexaConversation.Phrase)
                ? "Hola, soy tu logopeda virtual, tienes que decirme qué frase o palabra vamos a trabajar"
                : $@"Hola, continuamos trabajando la {(alexaConversation.Phrase.Contains(" ") ? "frase" : "palabra")} ""{alexaConversation.Phrase}"". A ver José Manuel, dime ""{alexaConversation.Phrase}""";

            DisplayDirective directive = new DisplayDirective()
            {
                Template = GenerateImageTemplate6(text: "Human Learning", imageUrl: "https://esalcedoost.blob.core.windows.net/public/background.png")
            };

            turnContext.AlexaResponseDirectives().Add(directive);
            await turnContext.SendActivityAsync(greetingMessage, cancellationToken: cancellationToken);
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