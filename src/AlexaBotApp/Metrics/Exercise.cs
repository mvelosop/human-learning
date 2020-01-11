using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AlexaBotApp.Metrics
{
    public class Exercise
    {
        public Exercise()
        {
        }

        public Exercise(string personName, string targetPhrase, string language, string phonemes)
        {
            PersonName = personName;
            TargetPhrase = targetPhrase;
            Language = language;
            Phonemes = phonemes;
            NormalizedPhonemes = phonemes.AsNormalizedPhonemes();
        }

        [JsonProperty]
        public int Attempts { get; private set; }

        [JsonProperty]
        public TimeSpan? ExerciseDuration { get; private set; }

        [JsonProperty]
        public DateTime? FinishDate { get; private set; }

        [JsonProperty]
        public int Id { get; private set; }

        [JsonProperty]
        public bool IsFinished { get; private set; }

        [JsonProperty]
        public string Language { get; private set; }

        [JsonProperty]
        public string NormalizedPhonemes { get; private set; }

        [JsonProperty]
        public string PersonName { get; private set; }

        [JsonProperty]
        public string Phonemes { get; private set; }

        [JsonProperty]
        public DateTime? StartDate { get; private set; }

        [JsonProperty]
        public string TargetPhrase { get; private set; }

        [JsonProperty]
        public List<Utterance> Utterances { get; private set; } = new List<Utterance>();

        [JsonProperty]
        public bool? WasSuccessfull { get; private set; }

        public void End()
        {
            End(false);
        }

        public Utterance RegisterUtterance(string recognizedPhrase, string phonemes)
        {
            var normalizedPhonemes = phonemes.AsNormalizedPhonemes();
            var levenshteinDistance = normalizedPhonemes.LevenshteinDistanceFrom(NormalizedPhonemes);

            var utterance = new Utterance
            {
                Date = DateTime.Now,
                RecognizedPhrase = recognizedPhrase,
                Phonemes = phonemes,
                NormalizedPhonemes = normalizedPhonemes,
                LevenshteinDistance = levenshteinDistance,
                PercentDeviation = NormalizedPhonemes.Length > 0
                    ? 100 * levenshteinDistance / NormalizedPhonemes.Length
                    : (int?)null
            };

            Utterances.Add(utterance);

            if (recognizedPhrase == TargetPhrase)
            {
                End(true);
            }

            return utterance;
        }

        public void Start()
        {
            StartDate = DateTime.Now;
        }

        private void End(bool success)
        {
            WasSuccessfull = success;

            FinishDate = DateTime.Now;
            ExerciseDuration = FinishDate - StartDate;
            Attempts = Utterances.Count;
            IsFinished = true;
        }
    }
}