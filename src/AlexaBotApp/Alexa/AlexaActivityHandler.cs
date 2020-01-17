using AlexaBotApp.Bots;
using AlexaBotApp.Infrastructure;
using Bot.Builder.Community.Adapters.Alexa;
using Bot.Builder.Community.Adapters.Alexa.Directives;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Alexa
{
    public class AlexaActivityHandler : ActivityHandler
    {
        public async override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    await Dispatcher(turnContext, cancellationToken);
                    break;
                case AlexaRequestType.LaunchRequest:
                    await OnMembersAddedAsync(
                        turnContext.Activity.MembersAdded, new DelegatingTurnContext<IConversationUpdateActivity>(turnContext), cancellationToken);
                    break;
                case AlexaRequestType.SessionEndedRequest:
                    await OnMembersRemovedAsync(
                        turnContext.Activity.MembersRemoved, new DelegatingTurnContext<IConversationUpdateActivity>(turnContext), cancellationToken);
                    break;
                default:
                    break;
            }
        }

        private Task Dispatcher(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Text)
            {
                case AlexaIntents.CancelIntent:
                    return OnCancelationIntentActivityAsync(new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);                    
                case AlexaIntents.TrainingIntent:
                    return OnTrainningIntentActivityAsync(new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);                
                default:
                    return OnMessageActivityAsync(
                                new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);                    
            }
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when <see cref="AlexaIntents.TrainingIntent"/> received
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of change training.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a message activity that indicates the user intent is to run a command referring to training.
        /// </remarks>
        /// <seealso cref="OnTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            AlexaIntent trainingIntent = turnContext.Activity.Value as AlexaIntent;
            var command = trainingIntent.Slots["TraniningCommand"].Value;

            switch (command.Substring(0,7))
            {
                case AlexaSlots.DeleteSlot:
                    return OnDeleteTrainningIntentActivityAsync(turnContext, cancellationToken);
                case AlexaSlots.ChangeSlot:
                    return OnChangeTrainningIntentActivityAsync(turnContext, cancellationToken);
                case AlexaSlots.CreateSlot:
                    return OnCreateTrainningIntentActivityAsync(turnContext, cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when <see cref="AlexaIntents.DeleteTrainingIntent"/> received
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of change training.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnDeleteTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a message activity that indicates the user intent is to delete a training.
        /// </remarks>
        /// <seealso cref="OnDeleteTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnDeleteTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when <see cref="AlexaIntents.ChangeTrainingIntent"/> received
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of change training.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnChangeTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a message activity that indicates the user intent is to change the training.
        /// </remarks>
        /// <seealso cref="OnChangeTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnChangeTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when <see cref="AlexaIntents.ChangeTrainingIntent"/> received
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of change training.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnCreateTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a message activity that indicates the user intent is to change the training.
        /// </remarks>
        /// <seealso cref="OnCreateTrainningIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnCreateTrainningIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when <see cref="AlexaIntents.CancelIntent"/> received
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnCancelationIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a message activity that indicates the user intent is to cancel.
        /// </remarks>
        /// <seealso cref="OnCancelationIntentActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnCancelationIntentActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    
    
    
    }
}
