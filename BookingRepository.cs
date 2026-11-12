using MySqlConnector;

public class BookingRepository(MySqlConnection connection)
{
    // Vyn v_booking_details räknar ut nätter och totalpris i SQL.
    private const string SelectColumns =
        """
        SELECT booking_id, customer_id, accommodation, city, room_number,
               check_in_date, check_out_date, nights, guests,
               price_per_night, total_price
        FROM v_booking_details
        """;

    // CREATE: boka ett rum åt kunden. Triggern i databasen kastar ett
    // MySqlException om rummet är upptaget eller har för få platser.
    public async Task<int> CreateBookingAsync(
        int customerId, int roomId, DateOnly checkIn, DateOnly checkOut, int guests)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO bookings (customer_id, room_id, check_in_date, check_out_date, guests)
            VALUES (@customerId, @roomId, @checkIn, @checkOut, @guests);
            """, connection);
        command.Parameters.AddWithValue("@customerId", customerId);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@checkIn", checkIn);
        command.Parameters.AddWithValue("@checkOut", checkOut);
        command.Parameters.AddWithValue("@guests", guests);
        await command.PrepareAsync();
        await command.ExecuteNonQueryAsync();
        return checked((int)command.LastInsertedId);
    }

    // READ: en bokning (t.ex. för bekräftelsen), eller null när id saknas.
    public async Task<BookingDetails?> GetBookingAsync(int bookingId)
    {
        await using var command = new MySqlCommand(
            $"{SelectColumns} WHERE booking_id = @bookingId;", connection);
        command.Parameters.AddWithValue("@bookingId", bookingId);
        await command.PrepareAsync();
        List<BookingDetails> bookings = await ReadBookingsAsync(command);
        return bookings.Count > 0 ? bookings[0] : null;
    }

    // READ: alla bokningar för en kund.
    public async Task<List<BookingDetails>> GetBookingsForCustomerAsync(int customerId)
    {
        await using var command = new MySqlCommand(
            $"{SelectColumns} WHERE customer_id = @customerId ORDER BY check_in_date;", connection);
        command.Parameters.AddWithValue("@customerId", customerId);
        await command.PrepareAsync();
        return await ReadBookingsAsync(command);
    }

    // UPDATE: ändra antal personer. Returnerar 0 om bokningen inte finns,
    // inte tillhör kunden eller om det nya antalet inte ryms i rummet.
    public async Task<int> UpdateGuestsAsync(int bookingId, int customerId, int newGuests)
    {
        await using var command = new MySqlCommand(
            """
            UPDATE bookings b
            JOIN rooms r ON r.room_id = b.room_id
            SET b.guests = @newGuests
            WHERE b.booking_id = @bookingId
              AND b.customer_id = @customerId
              AND @newGuests BETWEEN 1 AND r.max_guests;
            """, connection);
        command.Parameters.AddWithValue("@newGuests", newGuests);
        command.Parameters.AddWithValue("@bookingId", bookingId);
        command.Parameters.AddWithValue("@customerId", customerId);
        await command.PrepareAsync();
        return await command.ExecuteNonQueryAsync();
    }

    // DELETE: avboka. customer_id i WHERE gör att man bara kan avboka
    // den valda kundens bokningar. Rummet blir direkt ledigt igen.
    public async Task<int> DeleteBookingAsync(int bookingId, int customerId)
    {
        await using var command = new MySqlCommand(
            "DELETE FROM bookings WHERE booking_id = @bookingId AND customer_id = @customerId;",
            connection);
        command.Parameters.AddWithValue("@bookingId", bookingId);
        command.Parameters.AddWithValue("@customerId", customerId);
        await command.PrepareAsync();
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<BookingDetails>> ReadBookingsAsync(MySqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var bookings = new List<BookingDetails>();
        while (await reader.ReadAsync())
        {
            bookings.Add(new BookingDetails(
                reader.GetInt32("booking_id"),
                reader.GetInt32("customer_id"),
                reader.GetString("accommodation"),
                reader.GetString("city"),
                reader.GetString("room_number"),
                reader.GetDateOnly("check_in_date"),
                reader.GetDateOnly("check_out_date"),
                checked((int)reader.GetInt64("nights")),
                reader.GetInt32("guests"),
                reader.GetDecimal("price_per_night"),
                reader.GetDecimal("total_price")));
        }
        return bookings;
    }
}
