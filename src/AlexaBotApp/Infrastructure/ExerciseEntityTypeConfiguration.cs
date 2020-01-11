using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace AlexaBotApp.Infrastructure
{
    public class ExerciseEntityTypeConfiguration : IEntityTypeConfiguration<Exercise>
    {
        public void Configure(EntityTypeBuilder<Exercise> builder)
        {
            builder.ToTable("PhraseExercises", "SpeechTherapy");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.PersonName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.TargetPhrase)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(e => e.Phonemes)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(e => e.NormalizedPhonemes)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(e => e.Language)
                .HasMaxLength(5);
        }
    }
}