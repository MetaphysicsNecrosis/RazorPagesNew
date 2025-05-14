using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RazorPagesNew.Data.Migrations
{
    public partial class SecondTry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удаляем старые внешние ключи
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_OwnedEntity_EmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_EmployeeId",
                table: "OwnedEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_LeaveRecord_EmployeeId",
                table: "OwnedEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_TaskRecord_EmployeeId",
                table: "OwnedEntity");

            // Удаляем временные метки
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetUserTokens");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetUserTokens");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetUserRoles");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetUserRoles");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetUserLogins");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetUserLogins");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetUserClaims");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetUserClaims");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "AspNetRoleClaims");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "AspNetRoleClaims");

            // Добавляем внешние ключи с Restrict (без каскада)
            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_OwnedEntity_EmployeeId",
                table: "AttendanceRecords",
                column: "EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_EmployeeId",
                table: "OwnedEntity",
                column: "EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_LeaveRecord_EmployeeId",
                table: "OwnedEntity",
                column: "LeaveRecord_EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_TaskRecord_EmployeeId",
                table: "OwnedEntity",
                column: "TaskRecord_EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем новые ключи
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_OwnedEntity_EmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_EmployeeId",
                table: "OwnedEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_LeaveRecord_EmployeeId",
                table: "OwnedEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_TaskRecord_EmployeeId",
                table: "OwnedEntity");

            // (Восстановление CreatedAt/UpdatedAt не обязательно, если они больше не нужны)
            // Если нужно — можно вернуть добавление этих столбцов

            // Возвращаем FK без каскада, чтобы миграция корректно откатывалась
            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_OwnedEntity_EmployeeId",
                table: "AttendanceRecords",
                column: "EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_EmployeeId",
                table: "OwnedEntity",
                column: "EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_LeaveRecord_EmployeeId",
                table: "OwnedEntity",
                column: "LeaveRecord_EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedEntity_OwnedEntity_TaskRecord_EmployeeId",
                table: "OwnedEntity",
                column: "TaskRecord_EmployeeId",
                principalTable: "OwnedEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
