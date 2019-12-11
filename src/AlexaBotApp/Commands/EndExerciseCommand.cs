using System;

namespace AlexaBotApp.Commands
{
    public class RegisterUtteranceCommand
    {
        public RegisterUtteranceCommand(int id, string recognizedPhrase)
        {
            if (id == 0) throw new ArgumentException("zero value", nameof(id));
            if (string.IsNullOrWhiteSpace(recognizedPhrase)) throw new ArgumentException("null or empty", nameof(recognizedPhrase));

            Id = id;
            RecognizedPhrase = recognizedPhrase;
        }

        public int Id { get; }

        public string RecognizedPhrase { get; }
    }
}