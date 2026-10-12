namespace TexasPortal.Contracts.Sales.Heads;

public sealed record CreateHeadRequest(
    string FirstName,
    string LastName,
    string Type, // customer, agent
    string MobileNo,
    string Email,
    string Nationality,
    string Address
);

public sealed record UpdateHeadRequest(
    string FirstName,
    string LastName,
    string Type,
    string MobileNo,
    string Email,
    string Nationality,
    string Address
);
