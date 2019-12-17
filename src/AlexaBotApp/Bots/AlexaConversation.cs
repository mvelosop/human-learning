using AlexaBotApp.Metrics;
using Newtonsoft.Json;

namespace AlexaBotApp.Bots
{
    public class AlexaConversation
    {
        public bool Continue { get; set; }

        public int Count { get; set; }

        public PhraseExercise CurrentExercise { get; set; }

        public bool GotItRight { get; set; }

        public string Phrase { get; set; }
    }
}