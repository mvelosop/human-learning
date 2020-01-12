using Bot.Builder.Community.Adapters.Alexa;
using Bot.Builder.Community.Adapters.Alexa.Integration.AspNet.Core;
using Bot.Builder.Community.Adapters.Alexa.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlexaBotApp.Adapters
{
    public class AlexaAdapterWithErrorHandler : AlexaHttpAdapter
    {
        public AlexaAdapterWithErrorHandler(ILogger<AlexaAdapter> logger)
            : base(true)
        {
            //Adapter.Use(new AlexaIntentRequestToMessageActivityMiddleware());

            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");
                await turnContext.SendActivityAsync("<say-as interpret-as=\"interjection\">boom</say-as>, explotó.");
            };

            ShouldEndSessionByDefault = false;
            ConvertBotBuilderCardsToAlexaCards = true;

            Use(new AlexaIntentRequestToMessageActivityMiddleware(
                transformPattern: RequestTransformPatterns.MessageActivityTextFromSinglePhraseSlotValue));
        }
    }
}
