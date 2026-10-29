namespace TicketingAI.Api.Chat;

public class ChatModelException(string message, Exception? inner = null) : Exception(message, inner);
