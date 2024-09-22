using Grpc.Core;
using Game;
using Microsoft.Extensions.Logging;

namespace GameServer.Services;

public class EndlessFiveGameService(ILogger<EndlessFiveGameService> logger) : Game.EndlessFiveGame.EndlessFiveGameBase
{
    const int InitialWidth = 10;
    const int InitialHeight = 10;
    private static Board GameState { get; set; } = new()
    {
        Width = InitialWidth,
        Height = InitialHeight,
        Turn = 0,
    };
    private static readonly List<(string Host, string ID)> Players = [];
    private static string? ActivePlayer { get; set; }
    private static object s_lock = new object();
    private static State PackState()
    {
        var state = new State
        {
            ActivePlayer = ActivePlayer,
            CurrentBoardState = GameState,
            IsYourTurn = false,
        };
        state.Players.AddRange(Players.Select(p => p.ID));
        lock (s_lock)
        {
            State?.SetResult(state);
        }
        return state;
    }

    public override Task<State> Join(PlayerInfo request, ServerCallContext context)
    {
        logger.LogInformation("Player {PlayerID} is attempting to join", request.PlayerID);
        if (Players.Any(p => p.Host == context.Host))
        {
            throw new Exception($"A player has already joined from {context.Host}");
        }
        Players.Add((context.Host, request.PlayerID));
        if (Players.Count == 1)
        {
            ActivePlayer = context.Host;
        }
        logger.LogInformation("Player {PlayerID} has joined", request.PlayerID);
        return Task.FromResult(PackState());
    }

    public override Task<OK> Leave(PlayerInfo request, ServerCallContext context)
    {
        logger.LogInformation("Player {PlayerID} is attempting to leave from {Host}", request.PlayerID, context.Host);
        var item = Players.FirstOrDefault(x => x.Host == context.Host);
        var host = (item.Host) ?? throw new InvalidOperationException($"{context.Host} has never joined the game");
        var id = item.ID;
        if (request.PlayerID != id) throw new InvalidOperationException($"{id} did not contect from {host}");
        if (ActivePlayer == context.Host)
        {
            NextPlayer();
        }
        Players.Remove(item);
        logger.LogInformation("Player {PlayerID} has left the game", request.PlayerID);
        return Task.FromResult(new OK());
    }

    private static void NextPlayer()
    {
        var current = Players.FirstOrDefault(x => x.Host == ActivePlayer);
        var nextIndex = (Players.IndexOf(current) + 1) % Players.Count;
        if (nextIndex == 0) { GameState.Turn++}
        ActivePlayer = Players[nextIndex].Host;
    }

    private static TaskCompletionSource<State>? State { get; set; }
    public bool VictoryAchieved { get; private set; } = false;


    public override async Task MainLoop(IAsyncStreamReader<Entry> requestStream, IServerStreamWriter<State> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested && !VictoryAchieved)
        {
            lock (s_lock)
            {
                State ??= new();
            }
            var sendState = await State.Task;
            if (sendState != null)
            {
                sendState.IsYourTurn = context.Host == ActivePlayer;
                await responseStream.WriteAsync(sendState, context.CancellationToken);
                lock (s_lock)
                {
                    State ??= new();
                }
                if (sendState.IsYourTurn)
                {
                    var success = await requestStream.MoveNext();
                    if (success)
                    {
                        var result = requestStream.Current;
                        bool needToResize = false;
                        if (GameState.Width < result.X)
                        {
                            GameState.Width = result.X;
                            needToResize = true;
                        }
                        if (GameState.Width < result.Y)
                        {
                            GameState.Width = result.Y;
                            needToResize = true;
                        }
                        GameState.Entries.Add(result);
                        if (needToResize) Resize();
                        GameBoard[result.X, result.Y] = result.PlayerID;
                        CheckVictory(requestStream.Current);

                    }
                    NextPlayer();
                    PackState();
                }
            }
        }
    }

    private static void Resize()
    {
        GameBoard = new string[GameState.Width, GameState.Height];
        foreach (var entry in GameState.Entries)
        {
            GameBoard[entry.X, entry.Y] = entry.PlayerID;
        }
    }

    static string[,] GameBoard = new string[10, 10];
    public const int VictoryLength = 5;

    private static Winner? CheckVictory(Entry current)
    {
        var leftCount = CheckLine(current.PlayerID, current.X, current.Y, -1, 0);
        var rightCount = CheckLine(current.PlayerID, current.X, current.Y, 1, 0);
        if((leftCount + rightCount + 1) >= VictoryLength)
        {
            return new Winner
            {
                LastEntry = current,
                PlayerID = current.PlayerID,
            };
        }
        var topCount = CheckLine(current.PlayerID, current.X, current.Y, 0, 1);
        var bottomCount = CheckLine(current.PlayerID, current.X, current.Y, 0, -1);
        if ((topCount + bottomCount + 1) >= VictoryLength)
        {
            return new Winner
            {
                LastEntry = current,
                PlayerID = current.PlayerID,
            };
        }
        var firstDiagonalUpwards = CheckLine(current.PlayerID, current.X, current.Y, 1, 1);
        var firstDiagonalDownwards = CheckLine(current.PlayerID, current.X, current.Y, -1, -1);
        if ((firstDiagonalUpwards + firstDiagonalDownwards + 1) >= VictoryLength)
        {
            return new Winner
            {
                LastEntry = current,
                PlayerID = current.PlayerID,
            };
        }
        var secondDiagonalDownwards = CheckLine(current.PlayerID, current.X, current.Y, 1, -1);
        var secondDiagonalUpwards = CheckLine(current.PlayerID, current.X, current.Y, -1, 1);
        if ((secondDiagonalDownwards + secondDiagonalUpwards + 1) >= VictoryLength)
        {
            return new Winner
            {
                LastEntry = current,
                PlayerID = current.PlayerID,
            };
        }
        return null;
    }

    private static int CheckLine(string playerID, uint x, uint y, int dx, int dy)
    {
        var count = 0;
        for (var i = 1; i < VictoryLength; i++)
        {
            var nextX = x + i * dx;
            var nextY = y + i * dy;
            if(nextX > GameState.Width) { return count; }
            if(nextY > GameState.Height) { return count; }
            if (GameBoard[nextX, nextY] == playerID)
            {
                count++;
            }
            else
            {
                return count;
            }
        }
        return count;
    }
}
