using MySqlConnector;


string connectionString = "Server=localhost;Database=HolidayMaker;User ID=root;Password=kurslosenord;UseAffectedRows=false;";

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

var repository = new ProductRepository(connection);

List<Product> products = await repository.GetProductsAsync();

foreach(Product product in products)
{
    Console.WriteLine(product);
}