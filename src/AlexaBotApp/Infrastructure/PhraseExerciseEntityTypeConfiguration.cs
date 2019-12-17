using AlexaBotApp.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace AlexaBotApp.Infrastructure
{
    public class PhraseExerciseEntityTypeConfiguration : IEntityTypeConfiguration<PhraseExercise>
    {
        public void Configure(EntityTypeBuilder<PhraseExercise> builder)
        {
            builder.ToTable("PhraseExercises", "SpeechTherapy");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.TargetPhrase)
                .IsRequired()
                .HasMaxLength(150);

        }
    }
}