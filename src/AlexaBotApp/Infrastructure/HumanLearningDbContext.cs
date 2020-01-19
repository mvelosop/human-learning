using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;

namespace AlexaBotApp.Infrastructure
{
    public class HumanLearningDbContext : DbContext
    {
        public HumanLearningDbContext(DbContextOptions<HumanLearningDbContext> options)
            : base(options)
        {
        }

        public DbSet<Exercise> PhraseExercises { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ExerciseEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UtteranceEntityTypeConfiguration());
        }
    }
}