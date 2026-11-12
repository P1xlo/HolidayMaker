using MySqlConnector;

public class CustomerRepository(MySqlConnection connection)
{
    private const string SelectColumns =
        "SELECT customer_id, first_name, last_name, email, phone FROM customers";

    // CREATE: registrera en ny kund, id genereras av databasen.
    public async Task<int> CreateCustomerAsync(string firstName, string lastName, string email, string phone)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO customers (first_name, last_name, email, phone)
            VALUES (@firstName, @lastName, @email, @phone);
            """, connection);
        command.Parameters.AddWithValue("@firstName", firstName);
        command.Parameters.AddWithValue("@lastName", lastName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@phone", phone);
        await command.PrepareAsync();
        await command.ExecuteNonQueryAsync();
        return checked((int)command.LastInsertedId);
    }

    // READ: alla kunder.
    public async Task<List<Customer>> GetCustomersAsync()
    {
        await using var command = new MySqlCommand(
            $"{SelectColumns} ORDER BY last_name, first_name;", connection);
        return await ReadCustomersAsync(command);
    }

    // READ: sök på namn, e-post eller telefon (delsträng).
    public async Task<List<Customer>> SearchCustomersAsync(string search)
    {
        await using var command = new MySqlCommand(
            $"""
            {SelectColumns}
            WHERE CONCAT(first_name, ' ', last_name) LIKE CONCAT('%', @search, '%')
               OR email LIKE CONCAT('%', @search, '%')
               OR phone LIKE CONCAT('%', @search, '%')
            ORDER BY last_name, first_name;
            """, connection);
        command.Parameters.AddWithValue("@search", search);
        await command.PrepareAsync();
        return await ReadCustomersAsync(command);
    }

    // READ: en kund, eller null när id saknas.
    public async Task<Customer?> GetCustomerAsync(int id)
    {
        await using var command = new MySqlCommand(
            $"{SelectColumns} WHERE customer_id = @id;", connection);
        command.Parameters.AddWithValue("@id", id);
        await command.PrepareAsync();
        List<Customer> customers = await ReadCustomersAsync(command);
        return customers.Count > 0 ? customers[0] : null;
    }

    private static async Task<List<Customer>> ReadCustomersAsync(MySqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var customers = new List<Customer>();
        while (await reader.ReadAsync())
        {
            customers.Add(new Customer(
                reader.GetInt32("customer_id"),
                reader.GetString("first_name"),
                reader.GetString("last_name"),
                reader.GetString("email"),
                reader.GetString("phone")));
        }
        return customers;
    }
}
