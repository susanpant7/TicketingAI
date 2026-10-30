namespace TicketingAI.Api.Users;

/// <summary>The users an admin can load with "Load default users". The username is the lowercase first name.</summary>
public static class DefaultUsers
{
    public static IReadOnlyList<(string First, string Last)> All { get; } =
    [
        ("Aarav", "Sharma"), ("Bikash", "Thapa"), ("Anita", "Gurung"), ("Dipesh", "Adhikari"), ("Sita", "Rai"),
        ("Prakash", "Karki"), ("Sunita", "Shrestha"), ("Rajesh", "Poudel"), ("Manisha", "Tamang"), ("Nabin", "Khadka"),
        ("Binod", "Bhandari"), ("Kabita", "Magar"), ("Suman", "Basnet"), ("Puja", "Lama"), ("Roshan", "Acharya")
    ];
}
