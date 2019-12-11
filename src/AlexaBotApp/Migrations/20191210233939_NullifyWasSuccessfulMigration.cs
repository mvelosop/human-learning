using Microsoft.EntityFrameworkCore.Migrations;

namespace AlexaBotApp.Migrations
{
    public partial class NullifyWasSuccessfulMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "WasSuccessfull",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                nullable: true,
                oldClrType: typeof(bool));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "WasSuccessfull",
                schema: "SpeechTherapy",
                table: "PhraseExercises",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }
    }
}
