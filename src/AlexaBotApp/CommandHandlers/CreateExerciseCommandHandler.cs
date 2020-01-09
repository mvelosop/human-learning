using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class CreateExerciseCommandHandler : IRequestHandler<CreateExerciseCommand, Exercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public CreateExerciseCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new System.ArgumentNullException(nameof(dbContext));
        }

        public async Task<Exercise> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
        {
            await EndUnfinishedExercises();

            var entity = new Exercise(request.TargetPhrase, request.Language);

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