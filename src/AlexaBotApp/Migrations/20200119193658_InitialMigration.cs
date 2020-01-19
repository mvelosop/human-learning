using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AlexaBotApp.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "HumanLearning");

            migrationBuilder.CreateTable(
                name: "PhraseExercises",
                schema: "HumanLearning",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Attempts = table.Column<int>(nullable: false),
                    ExerciseDuration = table.Column<TimeSpan>(nullable: true),
                    FinishDate = table.Column<DateTime>(nullable: true),
                    IsFinished = table.Column<bool>(nullable: false),
                    Language = table.Column<string>(maxLength: 5, nullable: true),
                    NormalizedPhonemes = table.Column<string>(maxLength: 300, nullable: false),
                    PersonName = table.Column<string>(maxLength: 100, nullable: false),
                    Phonemes = table.Column<string>(maxLength: 300, nullable: false),
                    StartDate = table.Column<DateTime>(nullable: true),
                    TargetPhrase = table.Column<string>(maxLength: 150, nullable: false),
                    WasSuccessfull = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhraseExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utterances",
                schema: "HumanLearning",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(nullable: false),
                    ExerciseId = table.Column<int>(nullable: false),
                    LevenshteinDistance = table.Column<int>(nullable: true),
                    NormalizedPhonemes = table.Column<string>(maxLength: 300, nullable: true),
                    PercentDeviation = table.Column<int>(nullable: true),
                    Phonemes = table.Column<string>(maxLength: 300, nullable: true),
                    RecognizedPhrase = table.Column<string>(maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utterances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utterances_PhraseExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalSchema: "HumanLearning",
                        principalTable: "PhraseExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Utterances_ExerciseId",
                schema: "HumanLearning",
                table: "Utterances",
                column: "ExerciseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Utterances",
                schema: "HumanLearning");

            migrationBuilder.DropTable(
                name: "PhraseExercises",
                schema: "HumanLearning");
        }
    }
}
