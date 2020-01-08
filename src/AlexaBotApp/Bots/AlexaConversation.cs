using AlexaBotApp.Metrics;

namespace AlexaBotApp.Bots
{
    public class AlexaConversation
    {
        public bool Continue { get; set; }

        public int Count { get; set; }

        public Exercise CurrentExercise { get; set; }

        public bool GotItRight { get; set; }

        public string Language { get; set; }

        public string Phrase { get; set; }
    }
}