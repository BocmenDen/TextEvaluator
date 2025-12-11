namespace TextEvaluator.Ollama
{
    public class Message
    {
        public string Role { get; private init; } = null!;
        public required string Content { get; init; }

        private Message() { }

        public static Message CreateSystem(string content)
        {
            return new Message
            {
                Role = "system",
                Content = content
            };
        }
        public static Message CreateUser(string content)
        {
            return new Message
            {
                Role = "user",
                Content = content
            };
        }
    }
}
