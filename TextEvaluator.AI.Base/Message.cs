namespace TextEvaluator.AI.Base
{
    public class Message
    {
        public required RoleType Role { get; init; }
        public required string Content { get; init; }
    }

    public enum RoleType
    {
        System,
        User
    }
}
