using MySqlConnector;

public class RoomRepository(MySqlConnection connection)
{
    // READ: lediga rum som har plats för alla och inte är bokade någon natt i perioden.
    // Valfria filter skickas som NULL/FALSE och påverkar då inte resultatet.
    public async Task<List<AvailableRoom>> SearchAvailableAsync(RoomSearch search)
    {
        // ORDER BY kan inte parametriseras, så vi väljer mellan två fasta strängar.
        string orderBy = search.Sort == SortOrder.Rating
            ? "a.rating DESC, r.price_per_night ASC"
            : "r.price_per_night ASC, a.rating DESC";

        await using var command = new MySqlCommand(
            $"""
            SELECT r.room_id, a.name AS accommodation, ci.name AS city,
                   r.room_number, r.max_guests, r.price_per_night, a.rating
            FROM rooms r
            JOIN accommodations a ON a.accommodation_id = r.accommodation_id
            JOIN cities ci        ON ci.city_id = a.city_id
            WHERE ci.name = @city
              AND r.max_guests >= @guests
              AND (@requirePool = FALSE       OR a.has_pool = TRUE)
              AND (@requireRestaurant = FALSE OR a.has_restaurant = TRUE)
              AND (@maxBeach  IS NULL OR a.distance_to_beach_m  <= @maxBeach)
              AND (@maxCenter IS NULL OR a.distance_to_center_m <= @maxCenter)
              AND NOT EXISTS (
                    SELECT 1 FROM bookings b
                    WHERE b.room_id = r.room_id
                      AND b.check_in_date  < @checkOut
                      AND b.check_out_date > @checkIn)
            ORDER BY {orderBy};
            """, connection);
        command.Parameters.AddWithValue("@city", search.City);
        command.Parameters.AddWithValue("@guests", search.Guests);
        command.Parameters.AddWithValue("@requirePool", search.RequirePool);
        command.Parameters.AddWithValue("@requireRestaurant", search.RequireRestaurant);
        command.Parameters.AddWithValue("@maxBeach", (object?)search.MaxBeachDistance ?? DBNull.Value);
        command.Parameters.AddWithValue("@maxCenter", (object?)search.MaxCenterDistance ?? DBNull.Value);
        command.Parameters.AddWithValue("@checkIn", search.CheckIn);
        command.Parameters.AddWithValue("@checkOut", search.CheckOut);
        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var rooms = new List<AvailableRoom>();
        while (await reader.ReadAsync())
        {
            rooms.Add(new AvailableRoom(
                reader.GetInt32("room_id"),
                reader.GetString("accommodation"),
                reader.GetString("city"),
                reader.GetString("room_number"),
                reader.GetInt32("max_guests"),
                reader.GetDecimal("price_per_night"),
                reader.GetInt32("rating")));
        }
        return rooms;
    }

    // READ: alla orter, bra att visa som val innan sökningen.
    public async Task<List<string>> GetCitiesAsync()
    {
        await using var command = new MySqlCommand(
            "SELECT name FROM cities ORDER BY name;", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var cities = new List<string>();
        while (await reader.ReadAsync())
        {
            cities.Add(reader.GetString("name"));
        }
        return cities;
    }
}
