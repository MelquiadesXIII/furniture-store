using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Sillas" },
                    { 2, "Mesas" },
                    { 3, "Estanterías" },
                    { 4, "Lámparas" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "Price", "ProductCategoryId" },
                values: new object[,]
                {
                    { 1, "Silla Roble Nórdico", 189.99m, 1 },
                    { 2, "Silla Tapizada Lino", 149.50m, 1 },
                    { 3, "Silla Alta de Bar Teca", 129.00m, 1 },
                    { 4, "Silla Plegable Bambú", 79.90m, 1 },
                    { 5, "Mesa de Centro Nogal", 249.00m, 2 },
                    { 6, "Mesa Comedor Encino 6 Puestos", 899.00m, 2 },
                    { 7, "Mesa Auxiliar Mármol", 199.99m, 2 },
                    { 8, "Mesa Escritorio Minimalista", 329.00m, 2 },
                    { 9, "Estantería Modular Roble", 259.00m, 3 },
                    { 10, "Librero Escalera Pino", 179.00m, 3 },
                    { 11, "Estante Flotante Nogal (Set x3)", 89.90m, 3 },
                    { 12, "Vitrina Vintage", 449.00m, 3 },
                    { 13, "Lámpara de Pie Arco", 159.00m, 4 },
                    { 14, "Lámpara de Mesa Cerámica", 69.90m, 4 },
                    { 15, "Lámpara Colgante Rattan", 99.00m, 4 },
                    { 16, "Lámpara LED Escritorio", 45.50m, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
