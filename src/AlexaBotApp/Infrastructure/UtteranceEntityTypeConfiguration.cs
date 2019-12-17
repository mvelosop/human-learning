using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlexaBotApp.Infrastructure
{
    public class UtteranceEntityTypeConfiguration : IEntityTypeConfiguration<Utterance>
    {
        public void Configure(EntityTypeBuilder<Utterance> builder)
        {
            builder.ToTable("Utterances", "SpeechTherapy");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.RecognizedPhrase)
                .HasMaxLength(150);

            builder.HasOne(e => e.Exercise)
                .WithMany(p => p.Utterances)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}