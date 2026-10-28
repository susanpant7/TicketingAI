export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
}

// Placeholder until the chat endpoint exists: replace the body with a POST to /api/chat.
export async function sendMessage(message: string): Promise<string> {
  void message
  await new Promise((resolve) => setTimeout(resolve, 900))
  return "I'm not connected to the knowledge base yet, so I can't answer this properly. Once the AI is wired up, I'll answer from your company's IT and HR documents and can raise a ticket for you."
}
