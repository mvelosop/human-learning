using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using AlexaBotApp.Phonemizer;
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
        private readonly IPhonemizerService _phonemizer;

        public CreateExerciseCommandHandler(
            SpeechTherapyDbContext dbContext,
            IPhonemizerService phonemizer)
        {
            _dbContext = dbContext ?? throw new System.ArgumentNullException(nameof(dbContext));
            _phonemizer = phonemizer ?? throw new System.ArgumentNullException(nameof(phonemizer));
        }

        public async Task<Exercise> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
        {
            await EndUnfinishedExercises();

            var phonemes = await _phonemizer.GetPhonemesAsync(request.TargetPhrase);
            var entity = new Exercise(request.PersonName, request.TargetPhrase, request.Language, phonemes);

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