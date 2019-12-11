using System;

namespace AlexaBotApp.Commands
{
    public class CreatePhraseExerciseCommand
    {
        public CreatePhraseExerciseCommand(string targetPhrase)
        {
            if (string.IsNullOrWhiteSpace(targetPhrase)) throw new ArgumentException("null or empty", nameof(targetPhrase));

            TargetPhrase = targetPhrase;
        }

        public string TargetPhrase { get; }
    }
}