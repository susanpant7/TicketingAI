namespace TicketingAI.Data.Entities;

public class Department
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>What the department handles, in a few words. It is given to the chat model so it can send people to the right department.</summary>
    public string Description { get; set; } = "";

    /// <summary>How to reach the department, for example an email address. The chat model gives it to the user.</summary>
    public string Contact { get; set; } = "";

    public ICollection<User> Users { get; set; } = [];
}
