using AlexaBotApp.Metrics;
using MediatR;
using System;

namespace AlexaBotApp.Commands
{
    public class CreateExerciseCommand : IRequest<Exercise>
    {
        public CreateExerciseCommand(string targetPhrase, string language)
        {
            if (string.IsNullOrWhiteSpace(targetPhrase)) throw new ArgumentException("null or empty", nameof(targetPhrase));
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException("message", nameof(language));

            TargetPhrase = targetPhrase;
            Language = language;
        }

        public string Language { get; }

        public string TargetPhrase { get; }
    }
}