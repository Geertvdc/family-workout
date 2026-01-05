using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyFitness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkoutDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workout_types",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_memberships_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_memberships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantIndex = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_session_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_session_participants_workout_sessions_WorkoutSessio~",
                        column: x => x.WorkoutSessionId,
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_workout_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTypeId = table.Column<string>(type: "text", nullable: false),
                    StationIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_workout_types", x => x.Id);
                    table.CheckConstraint("CK_WorkoutSessionWorkoutType_StationIndex", "\"StationIndex\" >= 1 AND \"StationIndex\" <= 4");
                    table.ForeignKey(
                        name: "FK_workout_session_workout_types_workout_sessions_WorkoutSessi~",
                        column: x => x.WorkoutSessionId,
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_interval_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    StationIndex = table.Column<int>(type: "integer", nullable: false),
                    WorkoutTypeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_interval_scores", x => x.Id);
                    table.CheckConstraint("CK_WorkoutIntervalScore_RoundNumber", "\"RoundNumber\" >= 1 AND \"RoundNumber\" <= 3");
                    table.CheckConstraint("CK_WorkoutIntervalScore_Score", "\"Score\" >= 0");
                    table.CheckConstraint("CK_WorkoutIntervalScore_StationIndex", "\"StationIndex\" >= 1 AND \"StationIndex\" <= 4");
                    table.ForeignKey(
                        name: "FK_workout_interval_scores_workout_session_participants_Partic~",
                        column: x => x.ParticipantId,
                        principalTable: "workout_session_participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_GroupId_UserId",
                table: "group_memberships",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_UserId",
                table: "group_memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_interval_scores_ParticipantId_RoundNumber_StationIn~",
                table: "workout_interval_scores",
                columns: new[] { "ParticipantId", "RoundNumber", "StationIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_interval_scores_ParticipantId_WorkoutTypeId_Recorde~",
                table: "workout_interval_scores",
                columns: new[] { "ParticipantId", "WorkoutTypeId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_participants_UserId",
                table: "workout_session_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_participants_WorkoutSessionId_ParticipantIn~",
                table: "workout_session_participants",
                columns: new[] { "WorkoutSessionId", "ParticipantIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_participants_WorkoutSessionId_UserId",
                table: "workout_session_participants",
                columns: new[] { "WorkoutSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_workout_types_WorkoutSessionId_StationIndex",
                table: "workout_session_workout_types",
                columns: new[] { "WorkoutSessionId", "StationIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_CreatorId",
                table: "workout_sessions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_GroupId_SessionDate",
                table: "workout_sessions",
                columns: new[] { "GroupId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_types_Name",
                table: "workout_types",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "workout_interval_scores");

            migrationBuilder.DropTable(
                name: "workout_session_workout_types");

            migrationBuilder.DropTable(
                name: "workout_types");

            migrationBuilder.DropTable(
                name: "workout_session_participants");

            migrationBuilder.DropTable(
                name: "workout_sessions");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
