using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using AlexaBotApp.Phonemizer;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class RegisterUtteranceCommandHandler : IRequestHandler<RegisterUtteranceCommand, Utterance>
    {
        private readonly SpeechTherapyDbContext _dbContext;
        private readonly IPhonemizerService _phonemizer;

        public RegisterUtteranceCommandHandler(
            SpeechTherapyDbContext dbContext,
            IPhonemizerService phonemizer)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _phonemizer = phonemizer ?? throw new ArgumentNullException(nameof(phonemizer));
        }

        public async Task<Utterance> Handle(RegisterUtteranceCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={request.Id})");

            var phonemes = await _phonemizer.GetPhonemesAsync(request.RecognizedPhrase);
            var utterance = entity.RegisterUtterance(request.RecognizedPhrase, phonemes);

            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();

            return utterance;
        }
    }
}