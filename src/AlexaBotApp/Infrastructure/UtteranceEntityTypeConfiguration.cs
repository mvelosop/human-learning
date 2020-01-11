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

            builder.Property(e => e.Phonemes)
                .HasMaxLength(300);

            builder.Property(e => e.NormalizedPhonemes)
                .HasMaxLength(300);

            builder.HasOne(e => e.Exercise)
                .WithMany(p => p.Utterances)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}