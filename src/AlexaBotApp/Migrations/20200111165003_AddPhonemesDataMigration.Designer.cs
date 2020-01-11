﻿// <auto-generated />
using System;
using AlexaBotApp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AlexaBotApp.Migrations
{
    [DbContext(typeof(SpeechTherapyDbContext))]
    [Migration("20200111165003_AddPhonemesDataMigration")]
    partial class AddPhonemesDataMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("AlexaBotApp.Metrics.Exercise", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Attempts");

                    b.Property<TimeSpan?>("ExerciseDuration");

                    b.Property<DateTime?>("FinishDate");

                    b.Property<bool>("IsFinished");

                    b.Property<string>("Language")
                        .HasMaxLength(5);

                    b.Property<string>("NormalizedPhonemes")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<string>("PersonName")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<string>("Phonemes")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<DateTime?>("StartDate");

                    b.Property<string>("TargetPhrase")
                        .IsRequired()
                        .HasMaxLength(150);

                    b.Property<bool?>("WasSuccessfull");

                    b.HasKey("Id");

                    b.ToTable("PhraseExercises","SpeechTherapy");
                });

            modelBuilder.Entity("AlexaBotApp.Metrics.Utterance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Date");

                    b.Property<int>("ExerciseId");

                    b.Property<int?>("LevenshteinDistance");

                    b.Property<string>("NormalizedPhonemes")
                        .HasMaxLength(300);

                    b.Property<int?>("PercentDeviation");

                    b.Property<string>("Phonemes")
                        .HasMaxLength(300);

                    b.Property<string>("RecognizedPhrase")
                        .HasMaxLength(150);

                    b.HasKey("Id");

                    b.HasIndex("ExerciseId");

                    b.ToTable("Utterances","SpeechTherapy");
                });

            modelBuilder.Entity("AlexaBotApp.Metrics.Utterance", b =>
                {
                    b.HasOne("AlexaBotApp.Metrics.Exercise", "Exercise")
                        .WithMany("Utterances")
                        .HasForeignKey("ExerciseId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
