using AlexaBotApp.Commands;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class CreatePhraseExerciseCommandHandler : ICommandHandler<CreatePhraseExerciseCommand, PhraseExercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public CreatePhraseExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new System.ArgumentNullException(nameof(dbContext));
        }

        public async Task<PhraseExercise> HandleAsync(CreatePhraseExerciseCommand command)
        {
            await EndUnfinishedExercises();

            var entity = new PhraseExercise(command.TargetPhrase, command.Language);

            entity.Start();

            _dbContext.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        private async Task EndUnfinishedExercises()
        {
            var unfinishedExercises = await _dbContext.PhraseExercises
                .Where(e => !e.IsFinished)
                .Include(e => e.Utterances)
                .ToListAsync();

            unfinishedExercises.ForEach(e => e.End());
        }
    }
}