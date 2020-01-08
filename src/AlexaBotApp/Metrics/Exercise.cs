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

        public Exercise(string targetPhrase, string language)
        {
            TargetPhrase = targetPhrase;
            Language = language;
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

        public void RegisterUtterance(string recognizedPhrase)
        {
            Utterances.Add(new Utterance { Date = DateTime.Now, RecognizedPhrase = recognizedPhrase });

            if (recognizedPhrase == TargetPhrase)
            {
                End(true);
            }
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