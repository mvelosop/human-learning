using AlexaBotApp.Commands;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class DeleteExerciseCommandHandler : ICommandHandler<DeleteExerciseCommand>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public DeleteExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task HandleAsync(DeleteExerciseCommand command)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == command.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={command.Id})");

            _dbContext.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}