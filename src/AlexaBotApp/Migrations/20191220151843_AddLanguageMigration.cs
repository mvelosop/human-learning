using Microsoft.EntityFrameworkCore.Migrations;

namespace AlexaBotApp.Migrations
{
    public partial class AddLanguageMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                nullable: true);

            migrationBuilder.Sql("update SpeechTherapy.PhraseExercises set Language = 'es-US' where Language is null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                schema: "SpeechTherapy",
                table: "PhraseExercises");
        }
    }
}
