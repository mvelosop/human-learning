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
    public class CreateExerciseCommandHandler : ICommandHandler<CreateExerciseCommand, Exercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public CreateExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new System.ArgumentNullException(nameof(dbContext));
        }

        public async Task<Exercise> HandleAsync(CreateExerciseCommand command)
        {
            await EndUnfinishedExercises();

            var entity = new Exercise(command.TargetPhrase, command.Language);

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