using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var games = new List<Game>();
var players = new List<Player>();

var playerApi = app.MapGroup("/player");
playerApi.MapGet("/", () => players);
playerApi.MapGet("/{id}", (Guid id) =>
    players.FirstOrDefault(a => a.ID == id) is { } player
        ? Results.Ok(player)
        : Results.NotFound());
playerApi.MapPost("/register/{name}", (string name) => players.Add(new Player
{
    ID = Guid.NewGuid(),
    Name = name,
}));
playerApi.MapDelete("/{id}", (Guid id) =>
{
    var player = players.FirstOrDefault(p => p.ID == id);
    if (player is null)
    {
        return Results.NotFound();
    }
    players.Remove(player);
    return Results.Ok(player);
});



var gamesApi = app.MapGroup("/games");
gamesApi.MapGet("/", () => games);
gamesApi.MapGet("/{id}", (Guid id) =>
    games.FirstOrDefault(a => a.ID == id) is { } game
        ? Results.Ok(game)
        : Results.NotFound());
gamesApi.MapPost("/create/{name}", (string name) => games.Add(new Game(name)));
gamesApi.MapDelete("/{id}", (Guid id) =>
{
    var game = games.FirstOrDefault(p => p.ID == id);
    if (game is null)
    {
        return Results.NotFound();
    }
    games.Remove(game);
    return Results.Ok(game);
});
gamesApi.MapPost("/join/{gameId}/{playerId}", (Guid gameId, Guid playerId) =>
{
    
    var game = games.FirstOrDefault(p => p.ID == gameId);
    if (game is null)
    {
        return Results.NotFound();
    }
    var player = players.FirstOrDefault(p => p.ID == playerId);
    if (player is null)
    {
        return Results.NotFound();
    }
    game.Players.Add(player);
    return Results.Ok(player);
});
gamesApi.MapPost("/leave/{id}/{playerid}", (Guid gameId, Guid playerId) =>
{
    
    var game = games.FirstOrDefault(p => p.ID == gameId);
    if (game is null)
    {
        return Results.NotFound();
    }
    var player = game.Players.FirstOrDefault(p => p.ID == playerId);
    if (player is null)
    {
        return Results.NotFound();
    }
    game.Players.Remove(player);
    return Results.Ok(player);
});


app.Run();

[JsonSerializable(typeof(List<Game>))]
[JsonSerializable(typeof(List<Player>))]
[JsonSerializable(typeof(Player))]
[JsonSerializable(typeof(Game))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

public class Game
{
    private static int Count = 0;
    public Game(string? name)
    {
        Count++;
        ID = Guid.NewGuid();
        Players = [];
        Name = name ?? $"Game{Count}";
    }
    public Guid ID { get; }
    public string Name { get; }
    public List<Player> Players { get; }
}
public class Player
{
    public Guid ID { get; init; }
    public required string Name { get; init; }
}