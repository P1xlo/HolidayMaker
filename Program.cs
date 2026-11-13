using MySqlConnector;

string connectionString = "Server=localhost;Database=HolidayMaker;User ID=root;Password=kurslosenord;UseAffectedRows=false;";

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

var bookings = new BookingRepository(connection);
var customers = new CustomerRepository(connection);
var rooms = new RoomRepository(connection);

Customer? currentCustomer = null;
RoomSearch? lastSearch = null;
List<AvailableRoom> lastResults = [];

int val;
bool Running = true;

while(Running)
{
    Console.WriteLine("1. Skapa ny bokning\n"
        + "2. Ändra antal personer i en bokning\n"
        + "3. Avboka\n"
        + "4. Avsluta program");
    currentCustomer = null;
    while(!int.TryParse(Console.ReadLine(), out val) || val < 1 || val > 4)
        Console.WriteLine("Välj ett nummer mellan 1 och 4");
    switch(val)
    {
        case 1:
        await ChooseCustomer();
        if(currentCustomer == null)
            break;
        await SearchRooms();
        await CreateBooking();

            break;
        case 2:
        await ExistingCustomers();
        if(currentCustomer == null)
            break;
        await ChangeGuests();

            break;
        case 3:
        await ExistingCustomers();
        if(currentCustomer == null)
            break;
        await CancelBooking();
            break;
        case 4:
        Running = false;
        Console.WriteLine("Avslutar programmet");
            break;
    }
    
}



async Task ChooseCustomer()
{
    Console.WriteLine("1. Ny kund  2. Sök befintlig kund");
    switch (Console.ReadLine())
    {
        case "1":
            string first = ReadText("Förnamn: ");
            string last  = ReadText("Efternamn: ");
            string email = ReadText("E-post: ");
            string phone = ReadText("Telefon: ");
            try
            {
                int id = await customers.CreateCustomerAsync(first, last, email, phone);
                currentCustomer = await customers.GetCustomerAsync(id);
                Console.WriteLine($"Kund registrerad med id {id}.");
            }
            catch(MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                Console.WriteLine("Det finns redan en kund med denna e-postadressen.");
            }
            break;

        case "2":
            string search = ReadText("Sök namn/e-post/telefon: ");
            List<Customer> found = await customers.SearchCustomersAsync(search);
            if (found.Count == 0) 
            { 
                Console.WriteLine("Ingen kund hittades."); 
                break; 
            }

            for (int i = 0; i < found.Count; i++)
                Console.WriteLine($"{i + 1}. {found[i].FirstName} {found[i].LastName} – {found[i].Email}, {found[i].Phone}");

            int choice = MinMaxInt("Välj kund (nummer): ", 1, found.Count);
            currentCustomer = found[choice - 1];
            Console.WriteLine($"Vald kund: {currentCustomer.FirstName} {currentCustomer.LastName}");
            break;
    }
}

async Task ExistingCustomers()
{
    string search = ReadText("Sök namn/e-post/telefon: ");
    List<Customer> found = await customers.SearchCustomersAsync(search);
    if (found.Count == 0) 
    { 
        Console.WriteLine("Ingen kund hittades."); 
        return;
    }

    for (int i = 0; i < found.Count; i++)
        Console.WriteLine($"{i + 1}. {found[i].FirstName} {found[i].LastName} – {found[i].Email}, {found[i].Phone}");

    int choice = MinMaxInt("Välj kund (nummer): ", 1, found.Count);
    currentCustomer = found[choice - 1];
    Console.WriteLine($"Vald kund: {currentCustomer.FirstName} {currentCustomer.LastName}");
}

async Task SearchRooms()
{
    Console.WriteLine("Orter: " + string.Join(", ", await rooms.GetCitiesAsync()));
    string city = ReadText("Ort: ");
    bool check = true;

    //default values so that lastSearch doesnt complain
    DateOnly checkIn = DateOnly.Parse("2000-01-01");
    DateOnly checkOut = DateOnly.Parse("1999-01-01");
    
    while(check)
    {
        checkIn  = ReadDate("Incheckning");
        checkOut = ReadDate("Utcheckning");
        if (checkOut <= checkIn) 
            Console.WriteLine("Utcheckning måste vara efter incheckning.");
        else 
            check = false;
    }
    int guests = MinMaxInt("Antal personer: ", 1, 20);

    bool pool       = JorN("Måste ha pool?");
    bool restaurant = JorN("Måste ha restaurang?");
    int? maxBeach   = ReadOptionalInt("Max avstånd till strand i meter (tomt felt = spelar ingen roll): ");
    int? maxCenter  = ReadOptionalInt("Max avstånd till centrum i meter (tomt felt = spelar ingen roll): ");

    Console.WriteLine("Sortera: 1. Pris (lägst först)  2. Omdöme (högst först)");
    SortOrder sort = Console.ReadLine() switch
    {
        "2" => SortOrder.Rating,
        _   => SortOrder.Price
    };

    lastSearch = new RoomSearch(city, checkIn, checkOut, guests, pool, restaurant, maxBeach, maxCenter, sort);
    lastResults = await rooms.SearchAvailableAsync(lastSearch);
}

void PrintRooms(List<AvailableRoom> list)
{
    if (list.Count == 0) 
    { 
        Console.WriteLine("Inga lediga rum matchar."); 
        return; 
    }

    for (int i = 0; i < list.Count; i++)
    {
        var r = list[i];
        Console.WriteLine($"{i + 1,2}. {r.Accommodation,-25} {r.City,-10} Rum {r.RoomNumber,-7} " +
            $"max {r.MaxGuests} pers  {r.PricePerNight,8} kr/natt  {r.Rating}/5");
    }
}

async Task CreateBooking()
{
    if (currentCustomer is null) 
    { 
        Console.WriteLine("Ingen kund vald"); 
        return; 
    }
    if (lastSearch is null) 
    { 
        Console.WriteLine("Inget rum valt"); 
        return; 
    }

    PrintRooms(lastResults);
    int choice = MinMaxInt("Välj rum (nummer): ", 1, lastResults.Count);
    AvailableRoom room = lastResults[choice - 1];

    int bookingId = await bookings.CreateBookingAsync(
        currentCustomer.Id, room.RoomId, lastSearch.CheckIn, lastSearch.CheckOut, lastSearch.Guests);

    BookingDetails? b = await bookings.GetBookingAsync(bookingId);
    Console.WriteLine("--- Bokningsbekräftelse ---");
    Console.WriteLine($"Bokningsnummer: {b!.BookingId}");
    Console.WriteLine($"{b.Accommodation}, {b.City}, rum {b.RoomNumber}");
    Console.WriteLine($"{b.CheckIn} – {b.CheckOut}, {b.Guests} personer");
    Console.WriteLine($"{b.Nights} nätter × {b.PricePerNight:0} kr = {b.TotalPrice:0} kr");

    lastResults.Clear();   // the list is out of date now that the room is booked
}

async Task ChangeGuests()
{
    if (currentCustomer is null) 
    { 
        Console.WriteLine("Ingen kund vald"); 
        return; 
    }

    List<BookingDetails> list = await bookings.GetBookingsForCustomerAsync(currentCustomer.Id);
    if (list.Count == 0) 
    { 
        Console.WriteLine("Kunden har inga bokningar."); 
        return; 
    }

    for (int i = 0; i < list.Count; i++)
    {
        var b = list[i];
        Console.WriteLine($"{i + 1,2}. #{b.BookingId,-4} {b.Accommodation,-25} rum {b.RoomNumber,-7} " +
            $"{b.CheckIn} – {b.CheckOut}  {b.Guests} pers ");
    }

    int choice = MinMaxInt("Välj bokning: ", 1, list.Count);
    BookingDetails booking = list[choice - 1];

    int newGuests = MinMaxInt($"Nytt antal personer (nu {booking.Guests}): ", 1, 50);

    int affected = await bookings.UpdateGuestsAsync(booking.BookingId, currentCustomer.Id, newGuests);
    Console.WriteLine(affected == 1
        ? "Antal personer ändrat."
        : "Ryms det inte så många i rummet.");
}
async Task ShowBookings()
{
    if (currentCustomer is null) { Console.WriteLine("Välj kund först (1)."); return; }

    List<BookingDetails> list = await bookings.GetBookingsForCustomerAsync(currentCustomer.Id);
    if (list.Count == 0) { Console.WriteLine("Kunden har inga bokningar."); return; }

    foreach (var b in list)
    {
        Console.WriteLine($"#{b.BookingId,-4} {b.Accommodation,-25} rum {b.RoomNumber,-7} " +
                          $"{b.CheckIn} – {b.CheckOut}  {b.Guests} pers  {b.TotalPrice,8:0} kr");
    }
}

async Task CancelBooking()
{
    if (currentCustomer is null) { Console.WriteLine("Välj kund först (1)."); return; }

    await ShowBookings();
    int bookingId = MinMaxInt("Bokningsnummer att avboka: ", 1, int.MaxValue);

    int affected = await bookings.DeleteBookingAsync(bookingId, currentCustomer.Id);
    Console.WriteLine(affected == 1 ? "Bokningen är avbokad." : "Hittade ingen sådan bokning för kunden.");
}

//=========================================================
// Helper methods
//=========================================================
static string ReadText(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(input)) return input;
        Console.WriteLine("Får inte vara tomt.");
    }
}

static int MinMaxInt(string prompt, int min, int max)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            return value;
        Console.WriteLine($"Ange ett heltal mellan {min} och {max}.");
    }
}

static int? ReadOptionalInt(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (int.TryParse(input, out int value) && value >= 0) return value;
        Console.WriteLine("Ange ett tal eller lämna tomt.");
    }
}

static bool JorN(string prompt)
{
    Console.Write($"{prompt} (j/n): ");
    return Console.ReadLine()?.Trim().ToLower() == "j";
}

static DateOnly ReadDate(string prompt)
{
    while (true)
    {
        Console.Write($"{prompt} (ÅÅÅÅ-MM-DD): ");
        if (DateOnly.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", out DateOnly date))
            return date;
        Console.WriteLine("Ogiltigt datum.");
    }
}