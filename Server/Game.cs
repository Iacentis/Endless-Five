namespace Server
{
    public record Game(Guid ID, string Name, HashSet<Player> Players, string url)
    {
        private static int Count = 0;
        public Game(string? name, string url) : this(Guid.NewGuid(), name ?? $"Game{Count}", [], url)
        {
            Count++;
        }
    }
}