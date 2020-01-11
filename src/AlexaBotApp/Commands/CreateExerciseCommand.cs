using AlexaBotApp.Metrics;
using MediatR;
using System;

namespace AlexaBotApp.Commands
{
    public class CreateExerciseCommand : IRequest<Exercise>
    {
        public CreateExerciseCommand(string personName, string targetPhrase, string language)
        {
            if (string.IsNullOrWhiteSpace(personName)) throw new ArgumentException("null or empty", nameof(personName));
            if (string.IsNullOrWhiteSpace(targetPhrase)) throw new ArgumentException("null or empty", nameof(targetPhrase));
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException("message", nameof(language));

            PersonName = personName;
            TargetPhrase = targetPhrase;
            Language = language;
        }

        public string Language { get; }

        public string PersonName { get; }

        public string TargetPhrase { get; }
    }
}