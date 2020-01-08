using AlexaBotApp.Commands;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class RegisterUtteranceCommandHandler : ICommandHandler<RegisterUtteranceCommand, Exercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public RegisterUtteranceCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Exercise> HandleAsync(RegisterUtteranceCommand command)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == command.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={command.Id})");

            entity.RegisterUtterance(command.RecognizedPhrase);

            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }
    }
}