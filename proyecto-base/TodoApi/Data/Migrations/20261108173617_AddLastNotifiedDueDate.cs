using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Data.Migrations
{
    public partial class AddLastNotifiedDueDate : Migration
    {
        // InitialCreate ya crea la columna LastNotifiedDueDate. En una base nueva (por ejemplo,
        // el contenedor de SQL Server) el AddColumn original fallaba con "Column names in each
        // table must be unique". Se agrega solo si no existe: es el típico error que no se ve
        // en la máquina de quien tenía la base ya creada.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('TodoItems', 'LastNotifiedDueDate') IS NULL
    ALTER TABLE [TodoItems] ADD [LastNotifiedDueDate] datetime2 NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se elimina: la columna pertenece a InitialCreate.
        }
    }
}
