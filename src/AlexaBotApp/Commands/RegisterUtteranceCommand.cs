using System;

namespace AlexaBotApp.Commands
{
    public class EndExerciseCommand
    {
        public EndExerciseCommand(int id)
        {
            if (id == 0) throw new ArgumentException("zero value", nameof(id));

            Id = id;
        }

        public int Id { get; }
    }
}