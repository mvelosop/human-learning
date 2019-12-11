using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AlexaBotApp.Metrics
{
    public class PhraseExercise
    {
        public PhraseExercise()
        {
        }

        public PhraseExercise(string targetPhrase)
        {
            TargetPhrase = targetPhrase;
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
        public DateTime? StartDate { get; private set; }

        [JsonProperty]
        public string TargetPhrase { get; private set; }

        [JsonProperty]
        public List<Utterance> Utterances { get; private set; } = new List<Utterance>();

        [JsonProperty]
        public bool? WasSuccessfull { get; private set; }

        public void End()
        {
            WasSuccessfull = FinishDate.HasValue;
            Attempts = Utterances.Count;
            IsFinished = true;
        }

        public void RegisterUtterance(string recognizedPhrase)
        {
            Utterances.Add(new Utterance { Date = DateTime.Now, RecognizedPhrase = recognizedPhrase });

            if (recognizedPhrase == TargetPhrase)
            {
                FinishDate = DateTime.Now;
                ExerciseDuration = FinishDate - StartDate;

                End();
            }
        }

        public void Start()
        {
            StartDate = DateTime.Now;
        }
    }
}