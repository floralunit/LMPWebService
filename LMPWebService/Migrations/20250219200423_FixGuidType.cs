using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMPWebService.Migrations
{
    /// <inheritdoc />
    public partial class FixGuidType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OuterMessage",
                columns: table => new
                {
                    OuterMessage_ID = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OuterMessageReader_ID = table.Column<int>(type: "integer", maxLength: 255, nullable: false),
                    MessageOuter_ID = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProcessingStatus = table.Column<byte>(type: "smallint", maxLength: 255, nullable: false),
                    MessageText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ErrorCode = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    InsDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OuterMessage", x => x.OuterMessage_ID);
                });

            migrationBuilder.CreateTable(
                name: "OuterMessageReader",
                columns: table => new
                {
                    OuterMessageReader_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OuterMessageReaderName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OuterSystem_ID = table.Column<int>(type: "integer", maxLength: 255, nullable: false),
                    OuterMessageSourceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LastSuccessReadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    InsApplicationUser_ID = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false),
                    InsDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdApplicationUser_ID = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false),
                    UpdDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OuterMessageReader", x => x.OuterMessageReader_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OuterMessage");

            migrationBuilder.DropTable(
                name: "OuterMessageReader");
        }
    }
}
