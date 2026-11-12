using System.Diagnostics;
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


Console.WriteLine("1. Skapa ny bokning\n"
    + "2. Ändra antal personer i en bokning\n"
    + "3. Avboka\n"
    + "4. Avsluta program");



int val;
bool Running = true;

while(Running)
{
    while(int.TryParse(Console.ReadLine(), out val) || val < 1 || val > 4)
        Console.WriteLine("Välj ett nummer mellan 1 och 4");
    switch(val)
    {
        case 1:
        await Booking();

            break;
        case 2:

            break;
        case 3:

            break;
        case 4:
        Running = false;
        Console.WriteLine("Avslutar programmet");
            break;
    }
    
}



async Task Booking()
{
    await ChooseCustomer();
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
            int id = await customers.CreateCustomerAsync(first, last, email, phone);
            currentCustomer = await customers.GetCustomerAsync(id);
            Console.WriteLine($"Kund registrerad med id {id} och vald.");
            break;

        case "2":
            string search = ReadText("Sök namn/e-post/telefon: ");
            List<Customer> found = await customers.SearchCustomersAsync(search);
            if (found.Count == 0) { Console.WriteLine("Ingen kund hittades."); break; }

            for (int i = 0; i < found.Count; i++)
                Console.WriteLine($"{i + 1}. {found[i].FirstName} {found[i].LastName} – {found[i].Email}, {found[i].Phone}");

            int choice = ReadInt("Välj kund (nummer): ", 1, found.Count);
            currentCustomer = found[choice - 1];
            Console.WriteLine($"Vald kund: {currentCustomer.FirstName} {currentCustomer.LastName}");
            break;
    }
}


static string ReadText(string prompt)
{
    while (true)
    {
        string? input = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(input)) return input;
        Console.WriteLine("Får inte vara tomt.");
    }
}

static int ReadInt(string prompt, int min, int max)
{
    while (true)
    {
        if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            return value;
        Console.WriteLine($"Ange ett heltal mellan {min} och {max}.");
    }
}

static int? ReadOptionalInt(string prompt)
{
    while (true)
    {
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (int.TryParse(input, out int value) && value >= 0) return value;
        Console.WriteLine("Ange ett tal eller lämna tomt.");
    }
}

static bool ReadYesNo(string prompt)
{
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