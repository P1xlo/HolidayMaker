using System.Diagnostics;
using MySqlConnector;


string connectionString = "Server=localhost;Database=HolidayMaker;User ID=root;Password=kurslosenord;UseAffectedRows=false;";

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

var BookingRepo = new BookingRepository(connection);
var CustomerRepo = new CustomerRepository(connection);
var RoomRepo = new RoomRepository(connection);

List<string> Cities = await RoomRepo.GetCitiesAsync();

Console.WriteLine("Städer");
foreach(string city in Cities)
{
    Console.WriteLine(city);
}

List<Customer> customers = await CustomerRepo.GetCustomersAsync();

Console.WriteLine("Personer");
foreach(Customer customer in customers)
{
    Console.WriteLine(customer);
}



Console.WriteLine("1. Registrera / Välj kund\n"
    + "2. Sök efter lediga rum\n"
    + "3. Skapa ny bokning\n"
    + "4. Visa en kunds bokning\n"
    + "5. Ändra antal pers i en bokning\n"
    + "6. Avboka\n"
    + "7. Avsluta program");


int val;
bool Running = true;

while(Running)
{
    while(int.TryParse(Console.ReadLine(), out val) || val < 1 || val > 7)
        Console.WriteLine("");
    switch(val)
    {
        case 1:

            break;
        case 2:

            break;
        case 3:

            break;
        case 4:

            break;
        case 5:

            break;
        case 6:

            break;
        case 7:

            break;
    }
    
}