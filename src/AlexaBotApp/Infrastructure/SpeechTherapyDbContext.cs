using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;

namespace AlexaBotApp.Infrastructure
{
    public class SpeechTherapyDbContext : DbContext
    {
        public SpeechTherapyDbContext(DbContextOptions<SpeechTherapyDbContext> options)
            : base(options)
        {
        }

        public DbSet<PhraseExercise> PhraseExercises { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new PhraseExerciseEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UtteranceEntityTypeConfiguration());
        }
    }
}