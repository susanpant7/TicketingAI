namespace TicketingAI.Api.Knowledge;

public class EmbeddingException(string message, Exception? inner = null) : Exception(message, inner);
