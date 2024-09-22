namespace Server
{
    public record Game(Guid ID, string Name, HashSet<Player> Players)
    {
        private static int Count = 0;
        public Game(string? name) : this(Guid.NewGuid(), name ?? $"Game{Count}", [])
        {
            Count++;
        }
    }
}