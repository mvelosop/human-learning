using System;

namespace AlexaBotApp.Metrics
{
    public class Utterance
    {
        public DateTime Date { get; set; }

        public PhraseExercise Exercise { get; set; }

        public int ExerciseId { get; set; }

        public int Id { get; set; }

        public string RecognizedPhrase { get; set; }
    }
}