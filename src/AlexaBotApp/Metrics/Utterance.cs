using System;

namespace AlexaBotApp.Metrics
{
    public class Utterance
    {
        public DateTime Date { get; set; }

        public Exercise Exercise { get; set; }

        public int ExerciseId { get; set; }

        public int Id { get; set; }

        public int LevenshteinDistance { get; set; }

        public string NormalizedPhonemes { get; set; }

        public int PercentDeviation { get; set; }

        public string Phonemes { get; set; }

        public string RecognizedPhrase { get; set; }
    }
}