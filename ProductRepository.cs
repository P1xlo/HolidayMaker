using MySqlConnector;

public class ProductRepository(MySqlConnection connection)
{
    // READ: flera rader → en lista med objekt.
    public async Task<List<Product>> GetProductsAsync()
    {
        await using var command = new MySqlCommand(
            "SELECT id, name, price FROM products ORDER BY id;", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var products = new List<Product>();
        while (await reader.ReadAsync())
        {
            products.Add(new Product(
                reader.GetInt32("id"),
                reader.GetString("name"),
                reader.GetDecimal("price")));
        }
        return products;
    }

    // READ: en rad → ett objekt, eller null när id saknas.
    public async Task<Product?> GetProductAsync(int id)
    {
        await using var command = new MySqlCommand(
            "SELECT id, name, price FROM products WHERE id = @id;", connection);
        command.Parameters.AddWithValue("@id", id);
        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Product(
                reader.GetInt32("id"),
                reader.GetString("name"),
                reader.GetDecimal("price"));
        }
        return null;
    }

    // CREATE: id genereras av databasen.
    public async Task<int> CreateProductAsync(string name, decimal price)
    {
        await using var command = new MySqlCommand(
            "INSERT INTO products (name, price) VALUES (@name, @price);", connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@price", price);
        await command.PrepareAsync();
        await command.ExecuteNonQueryAsync();
        return checked((int)command.LastInsertedId);
    }

    // UPDATE: WHERE avgränsar vilken produkt som ska ändras.
    public async Task<int> UpdateProductAsync(int id, string name, decimal price)
    {
        await using var command = new MySqlCommand(
            """
            UPDATE products
            SET name = @name, price = @price
            WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@id", id);
        await command.PrepareAsync();
        return await command.ExecuteNonQueryAsync();
    }

    // DELETE: WHERE avgränsar vilken produkt som ska tas bort.
    public async Task<int> DeleteProductAsync(int id)
    {
        await using var command = new MySqlCommand(
            "DELETE FROM products WHERE id = @id;", connection);
        command.Parameters.AddWithValue("@id", id);
        await command.PrepareAsync();
        return await command.ExecuteNonQueryAsync();
    }
}
