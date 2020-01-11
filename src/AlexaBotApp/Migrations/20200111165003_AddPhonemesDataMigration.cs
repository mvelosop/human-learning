using Microsoft.EntityFrameworkCore.Migrations;

namespace AlexaBotApp.Migrations
{
    public partial class AddPhonemesDataMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LevenshteinDistance",
                schema: "SpeechTherapy",
                table: "Utterances",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhonemes",
                schema: "SpeechTherapy",
                table: "Utterances",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PercentDeviation",
                schema: "SpeechTherapy",
                table: "Utterances",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phonemes",
                schema: "SpeechTherapy",
                table: "Utterances",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 300,
                nullable: true,
                defaultValue: "");

            migrationBuilder.Sql("update SpeechTherapy.PhraseExercises set NormalizedPhonemes = '.' where NormalizedPhonemes is null");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedPhonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonName",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.Sql("update SpeechTherapy.PhraseExercises set PersonName = '.' where PersonName is null");

            migrationBuilder.AlterColumn<string>(
                name: "PersonName",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 300,
                nullable: true,
                defaultValue: "");

            migrationBuilder.Sql("update SpeechTherapy.PhraseExercises set Phonemes = '.' where Phonemes is null");

            migrationBuilder.AlterColumn<string>(
                name: "Phonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevenshteinDistance",
                schema: "SpeechTherapy",
                table: "Utterances");

            migrationBuilder.DropColumn(
                name: "NormalizedPhonemes",
                schema: "SpeechTherapy",
                table: "Utterances");

            migrationBuilder.DropColumn(
                name: "PercentDeviation",
                schema: "SpeechTherapy",
                table: "Utterances");

            migrationBuilder.DropColumn(
                name: "Phonemes",
                schema: "SpeechTherapy",
                table: "Utterances");

            migrationBuilder.DropColumn(
                name: "NormalizedPhonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises");

            migrationBuilder.DropColumn(
                name: "PersonName",
                schema: "SpeechTherapy",
                table: "PhraseExercises");

            migrationBuilder.DropColumn(
                name: "Phonemes",
                schema: "SpeechTherapy",
                table: "PhraseExercises");

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 5,
                oldNullable: true);
        }
    }
}
