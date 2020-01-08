using MediatR;
using System;

namespace AlexaBotApp.Commands
{
    public class DeleteExerciseCommand : IRequest<bool>
    {
        public DeleteExerciseCommand(int id)
        {
            if (id == 0) throw new ArgumentException("must be a non-zero value", nameof(id));

            Id = id;
        }

        public int Id { get; }
    }
}