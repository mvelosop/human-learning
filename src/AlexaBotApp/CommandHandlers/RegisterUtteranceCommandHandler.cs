using AlexaBotApp.Commands;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.CommandHandlers
{
    public class RegisterUtteranceCommandHandler : IRequestHandler<RegisterUtteranceCommand, Exercise>
    {
        private readonly SpeechTherapyDbContext _dbContext;

        public RegisterUtteranceCommandHandler(SpeechTherapyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Exercise> Handle(RegisterUtteranceCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PhraseExercises
                .Include(e => e.Utterances)
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (entity is null) throw new InvalidOperationException($@"Phrase exercise not found! (id={request.Id})");

            entity.RegisterUtterance(request.RecognizedPhrase);

            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }
    }
}