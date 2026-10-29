using System.Text.Json.Serialization;

namespace TicketingAI.Data.Entities;

// Written as text ("InProgress") in the database and in the API.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatus
{
    New,
    InProgress,
    Completed
}
