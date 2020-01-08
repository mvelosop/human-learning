using AlexaBotApp.Commands;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class EndExerciseCommandHandler : IRequestHandler<EndExerciseCommand, Exercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public EndExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Exercise> Handle(EndExerciseCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={request.Id})");

            entity.End();

            await _dbContext.SaveChangesAsync();

            return entity;
        }
    }
}