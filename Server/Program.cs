using Server;
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
playerApi.MapPost("/register/{name}", (string name) =>
{
    var player = new Player(Guid.NewGuid(), name);
    players.Add(player);
    return Results.Ok(player);
});
playerApi.MapDelete("/{id}", (Guid id) =>
{
    var player = players.FirstOrDefault(p => p.ID == id);
    if (player is null)
    {
        return Results.NotFound();
    }
    players.Remove(player);
    foreach (var game in games)
    {
        game.Players.Remove(player);
    }
    return Results.Ok(player);
});



var gamesApi = app.MapGroup("/games");
gamesApi.MapGet("/", () => games);
gamesApi.MapGet("/{id}", (Guid id) =>
    games.FirstOrDefault(a => a.ID == id) is { } game
        ? Results.Ok(game)
        : Results.NotFound());
gamesApi.MapPost("/create/{name}", (string name) =>
{
    var game = new Game(name);
    games.Add(game);
    return Results.Ok(game);
});
gamesApi.MapDelete("/delete/{id}", (Guid id) =>
{
    var game = games.FirstOrDefault(g => g.ID.Equals(id));
    if (game is null)
    {
        return Results.NotFound();
    }
    games.Remove(game);
    return Results.Ok(game);
});
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
gamesApi.MapPost("/leave/{gameId}/{playerId}", (Guid gameId, Guid playerId) =>
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
[JsonSerializable(typeof(HashSet<Player>))]
[JsonSerializable(typeof(List<Player>))]
[JsonSerializable(typeof(Player))]
[JsonSerializable(typeof(Game))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
