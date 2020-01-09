using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class DeleteExerciseCommandHandler : IRequestHandler<DeleteExerciseCommand, bool>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public DeleteExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<bool> Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={request.Id})");

            _dbContext.Remove(entity);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}