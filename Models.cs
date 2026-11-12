public sealed record Customer(int Id, string FirstName, string LastName, string Email, string Phone);

public sealed record AvailableRoom(
    int RoomId,
    string Accommodation,
    string City,
    string RoomNumber,
    int MaxGuests,
    decimal PricePerNight,
    int Rating);

public sealed record BookingDetails(
    int BookingId,
    int CustomerId,
    string Accommodation,
    string City,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    int Guests,
    decimal PricePerNight,
    decimal TotalPrice);

public enum SortOrder { Price, Rating }

// Sökkriterier: de fyra första är obligatoriska, resten är valfria filter.
public sealed record RoomSearch(
    string City,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    bool RequirePool = false,
    bool RequireRestaurant = false,
    int? MaxBeachDistance = null,
    int? MaxCenterDistance = null,
    SortOrder Sort = SortOrder.Price);
