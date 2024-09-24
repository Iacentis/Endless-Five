# Endless-Five
The repository for the hobby project of Infinite Five-in-a-row

## Conceptual Idea
Create a website to play five-in-a-row with multiple players (an arbitrary amount, perhaps 6?).
The playing field should be virtually endless, although restrictions exist on player moves.
Games should be possible to be saved between sessions, either on user accounts or as a link.

### Rules
Players take turns sequentially, and markers can only be placed adjacent to other already placed markers (diagonals included).
When one player gets five markers in a row (horizontally, vertically or diagonally), they win.

### Requirements
- Different player markers should be easily differentiable (shape and/or color).
- Should there be a time limit?
- Should there be an indicator for legal moves?
- Round tracker?
- Playing field needs to be scrollable.

### To Run
Open up a terminal, navigate to /server and execute ```dotnet run```. This will open the server in localhost:5093
In a different terminal, or your IDE of choice, navigate to /client and execute ```dotnet run```. This will open up the blazor app.


### Architecture (Draft)
```mermaid
classDiagram
    %% Example showing the use of cardinalities

    %% Defining the classes
    class Client {
        User ID
        Display Name
        Renders Lobby for User()
        Renders Game for User()
        Join Game(Game)

    }
    class LobbyServer {
        List of Players
        List of Games
        Get Games()
        Create Game()
        Delete Game()
        Get Players()
        Register Player()
        Delete Player()
    }
    class GameServer {
        Game Board
        Active Players
        Current Turn
    }

    %% Defining the relationships with cardinalities
    Client --> LobbyServer : Queries for games
    Client --> LobbyServer : Request user Registration
    
    Client --> GameServer : Joins and Plays

    GameServer --> Client : Notifies


    
    GameServer --> LobbyServer : Notifies of Closure
    LobbyServer --> GameServer : Spawns

    %% Adding a note to explain the diagram
    note for Client "A user connects to the client (made in Blazor) through the browser"
    note for GameServer "Multiple may be spawned by the lobby server, one per game"
    %% Apply CSS classes using cssClass statement
    %% cssClass "Company, Employee, Project" generalClass
     style GameServer fill:#393,stroke:#333,stroke-width:2px
     style Client fill:#353,stroke:#333,stroke-width:2px
     style LobbyServer fill:#263,stroke:#66f,stroke-width:2px,color:#fff,stroke-dasharray: 5 5

```
