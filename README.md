# Holiday Maker

En konsolapplikation i C# för en resebyråoperatör som söker lediga rum, registrerar kunder och hanterar bokningar. Data lagras i en MySQL-databas.


### 1. Det här behövs

- **MySQL Server 8.0.16 eller nyare**
- **En SQL-klient**
- **.NET 10 SDK**

### 2. Kontrollera att MySQL-servern är igång

MySQL ska lyssna på `localhost`, port `3306`. Öppna *Tjänster* (`services.msc`) och kontrollera att den har status *Körs*. Annars: högerklicka och välj *Starta*.

### 3. Anslut till servern

**DataGrip**
1. *File → New → Data Source → MySQL*.
2. Fyll i **Host** `localhost`, **Port** `3306`, **User** `root` och ditt lösenord. Lämna **Database** tomt.
3. Om DataGrip frågar, klicka *Download missing driver files*.
4. Klicka *Test Connection* och sedan *OK*.

### 4. Kör skriptet `holidaymaker.sql`

> **Obs!** Skriptet börjar med `DROP DATABASE IF EXISTS holidaymaker`. En befintlig databas med det namnet raderas och skapas om, med all data.

Skriptet innehåller `DELIMITER` (för triggerna) och måste därför köras **som en hel fil**, inte en sats i taget.

**DataGrip**
1. Dra in `holidaymaker.sql` i projektet eller öppna den via *File → Open*.
2. Högerklicka på filen och välj **Run 'holidaymaker.sql'**.
3. Välj din MySQL-anslutning som mål.


Skriptet skapar:
- databasen `holidaymaker` (teckenkodning `utf8mb4`, så att å, ä och ö fungerar)
- tabellerna `cities`, `accommodations`, `rooms`, `customers` och `bookings`
- regler: primär- och främmande nycklar, `UNIQUE` och `CHECK`
- två triggers som stoppar dubbelbokningar och fler gäster än rummet rymmer
- vyn `v_booking_details`, som räknar ut nätter och totalpris
- testdata: 5 orter, 10 boenden, 28 rum, 5 kunder och 6 bokningar i december 2026


### 5. Ställ in anslutningen i programmet

Öppna `Program.cs` och ändra anslutningssträngen på rad 3 så att den matchar din server:

```csharp
string connectionString = "Server=localhost;Database=HolidayMaker;User ID=root;Password=DITT_LÖSENORD;UseAffectedRows=false;";
```

- **`Password`**: lösenordet för din MySQL-användare.
- **`UseAffectedRows=false`** ska vara kvar. Annars räknas det som ett misslyckande om man ändrar antal personer till samma antal som innan.

### 6. Starta programmet

Kör från projektmappen:

```bash
dotnet run
```

Menyn visas. Välj **1** och sök på t.ex. ort `Ystad`, incheckning `2026-12-15`, utcheckning `2026-12-18` för att se lediga rum.