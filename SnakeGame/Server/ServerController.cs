//Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023. 

using System.Numerics;
using System.Xml;
using SnakeGame;
using Model;
using NetworkUtil;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace Server;

/// <summary>
/// This class represents the controller for the server.
/// </summary>
public class ServerController
{
    private World theWorld;
    private Random rand = new();

    public delegate void ServerUpdateHandler(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups);
    public event ServerUpdateHandler? ServerUpdate;

    // settings
    private long msPerFrame;
    private int maxPowerups = 20;
    private int RespawnRate;
    private int size;
    private int nextPowID = 0;
    private GameSettings gameSettings;

    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;

    int snakeSpeed = 6;


    /// <summary>
    /// This constructor will create all the walls in the world
    /// that was specified in the GameSettings file. 
    /// </summary>
    /// <param name="gameSettings"></param>
    public ServerController(GameSettings gameSettings)
    {
        this.gameSettings = gameSettings;
        this.size = gameSettings.UniverseSize;
        this.theWorld = new World(this.size);
        this.msPerFrame = gameSettings.MSPerFrame;
        this.RespawnRate = gameSettings.RespawnRate;

        foreach (Wall wall in this.gameSettings.Walls)
        {
            this.theWorld.Walls[wall.wall] = wall;
        }

        this.clients = new Dictionary<long, SocketState>();
    }

    /// <summary>
    /// This method will start the server in port 11000, and
    /// uses the NewClientConnected as a call back for when new clients
    /// are trying to connect to the server.
    /// </summary>
    public void BeginServer()
    {
        Console.WriteLine("Server started");
        Networking.StartServer(NewClientConnected, 11000);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects 
    /// </summary>
    /// <param name="state">The SocketState representing the new client.</param>
    private void NewClientConnected(SocketState state)
    {
        Console.WriteLine("Client Connected");
        if (state.ErrorOccurred)
            return;

        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ReceiveMessage;

        Networking.GetData(state);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a network action occurs
    /// </summary>
    /// <param name="state">The SocketState representing the new client.</param>
    private void ReceiveMessage(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        ProcessMessage(state);

        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }

    /// <summary>
    /// This method will process the message recieved from the clients state, and
    /// send the start up information to the client. The start up information consists of
    /// the players ID, world size, and all the walls in the world.
    /// </summary>
    /// <param name="state">The SocketState representing the new client.</param>
    private void ProcessMessage(SocketState state)
    {
        string totalData = state.GetData();

        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until we have processed all messages.
        // We may have received more than one.
        foreach (string p in parts)
        {
            // Ignore empty strings added by the regex splitter
            if (p.Length == 0)
                continue;
            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;

            // Spawns a snake in a random location
            Snake snake = this.SnakeSpawn((int)state.ID, p);

            lock (theWorld)
            {
                this.theWorld.Snakes[(int)state.ID] = snake;
            }

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);

            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }

            state.OnNetworkAction = CommandRequest;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(state.ID + "\n" + this.size + "\n");

            foreach (Wall wall in this.gameSettings.Walls)
            {
                string jsonString = JsonSerializer.Serialize(wall);

                stringBuilder.Append(jsonString + "\n");
            }

            foreach (PowerUp powerUp in this.theWorld.PowerUps.Values)
            {
                string jsonString = JsonSerializer.Serialize(powerUp);

                stringBuilder.Append(jsonString + "\n");
            }
            Networking.Send(state.TheSocket, stringBuilder.ToString());
        }
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a client sends a command request.
    /// </summary>
    /// <param name="state">The SocketState representing the new client.</param>
    private void CommandRequest(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        ProcessCommand(state);

        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }

    /// <summary>
    /// This method will process the command recieved from the clients socket
    /// and will change the direction of the snake.
    /// </summary>
    /// <param name="state">The SocketState representing the client.</param>
    private void ProcessCommand(SocketState state)
    {

        string totalData = state.GetData();

        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until we have processed all messages.
        // We may have received more than one.
        foreach (string p in parts)
        {
            // Ignore empty strings added by the regex splitter
            if (p.Length == 0)
                continue;
            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;

            Snake snake = this.theWorld.Snakes[(int)state.ID];
            Vector2D head = snake.body[snake.body.Count - 1];

            if (p.Contains("up"))
            {
                snake.ChangeDirection("up");
            }

            if (p.Contains("left"))
            {
                snake.ChangeDirection("left");
            }

            if (p.Contains("down"))
            {
                snake.ChangeDirection("down");
            }

            if (p.Contains("right"))
            {
                snake.ChangeDirection("right");
            }

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);
        }
    }


    /// <summary>
    /// Removes a client from the clients dictionary
    /// </summary>
    /// <param name="id">The ID of the client</param>
    private void RemoveClient(long id)
    {
        Console.WriteLine("Client " + id + " disconnected");
        lock (clients)
        {
            theWorld.Snakes[(int)id].alive = false;
            clients.Remove(id);

        }
    }

    /// <summary>
    /// This method will create a stop watch to determine the framerate of updating
    /// the clients.
    /// </summary>
    public void Run()
    {
        // Start a new timer to control the frame rate
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        while (true)
        {
            // wait until the next frame
            while (watch.ElapsedMilliseconds < msPerFrame)
            { /* empty loop body */}

            watch.Restart();

            Update();

            ServerUpdate?.Invoke(theWorld.Snakes.Values, theWorld.PowerUps.Values);
        }
    }

    /// <summary>
    /// This method will update all the snakes locations, generate new locations for powerups
    /// and will determine if a snake has collided with an object. It will then send a serialized
    /// string of all the snakes and powerups to all the clients in the server.
    /// </summary>
    private void Update()
    {
        // cleanup the deactivated objects
        IEnumerable<int> playersToRemove = theWorld.Snakes.Values.Where(x => !x.alive).Select(x => x.snake);
        IEnumerable<int> powsToRemove = theWorld.PowerUps.Values.Where(x => x.died).Select(x => x.power);
        lock (theWorld)
        {

            foreach (int i in powsToRemove)
                theWorld.PowerUps.Remove(i);
        }

        // add new objects back in
        int halfSize = size / 2;

        while (theWorld.PowerUps.Count < maxPowerups)
        {
            PowerUp p = new PowerUp(nextPowID++, this.PowerupSpawn(25), false);
            lock (theWorld)
            {
                theWorld.PowerUps.Add(p.power, p);
            }
        }

        // move/update the existing objects in the world

        StringBuilder stringBuilder = new StringBuilder();

        lock (theWorld)
        {
            foreach (Snake p in theWorld.Snakes.Values)
            {
                if (!clients.ContainsKey(p.snake))
                {
                    p.alive = false;
                }

                if (p.alive)
                {
                    bool isSnakeColliding = SnakeWallCollision(p) || this.SnakeCollisionSnake(p) || this.SnakeCollisionSelf(p);

                    if (isSnakeColliding)
                    {
                        p.died = true;

                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        // wait until the next frame
                        while (watch.ElapsedMilliseconds < RespawnRate)
                        { /* empty loop body */}

                        watch.Stop();

                        stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");

                        this.theWorld.Snakes[p.snake] = SnakeSpawn(p.snake, p.name);
                        continue;
                    }

                    p.Step(this.snakeSpeed);

                    bool isPowerupCollide = this.SnakePowerupCollision(p);
                    if (isPowerupCollide)
                    {
                        p.score++;
                        p.frameRate = msPerFrame;
                        Thread thread = new Thread(p.RecievedPowerup);
                        thread.Start();
                    }
                }

                stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
            }
        }

        foreach (PowerUp p in theWorld.PowerUps.Values)
        {
            stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
        }

        // Broadcast the message to all clients
        // Lock here beccause we can't have new connections 
        // adding while looping through the clients list.
        // We also need to remove any disconnected clients.
        HashSet<long> disconnectedClients = new HashSet<long>();
        lock (clients)
        {
            foreach (SocketState client in clients.Values)
            {
                if (!Networking.Send(client.TheSocket!, stringBuilder.ToString()))
                    disconnectedClients.Add(client.ID);
            }
        }

        foreach (long id in disconnectedClients)
        {
            RemoveClient(id);
        }
    }

    /// <summary>
    /// This method is used to check if a snake has collided with all
    /// the walls in the world.
    /// </summary>
    /// <param name="snake">The snake used to check the collision with the walls.</param>
    /// <returns>Returns true if the snake has collided with a wall, false otherwise.</returns>
    private bool SnakeWallCollision(Snake snake)
    {
        foreach (Wall wall in this.theWorld.Walls.Values)
        {
            int snakeWidth = 10;
            int wallWidth = 25;
            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D head = snake.body.Last();
            double snakeHeadX = head.GetX();
            double snakeHeadY = head.GetY();

            if (p1.GetX() - wallWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + wallWidth + snakeWidth
                && p1.GetY() - wallWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + wallWidth + snakeWidth)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// This method is used to determine if a snakes body element has collided with
    /// all the walls in the world.
    /// </summary>
    /// <param name="snake">The snake used to check the collisions with the walls.</param>
    /// <param name="bodyIndex">The index of the body to check for collisions.</param>
    /// <returns>Returns true if the snakes body segment has collided with a wall, false otherise.</returns>
    private bool SnakeBodyCollision(Snake snake, int bodyIndex)
    {
        foreach (Wall wall in this.theWorld.Walls.Values)
        {
            int snakeWidth = 10;
            int wallWidth = 25;
            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D bodyPart = snake.body.ElementAt(bodyIndex);
            double snakeHeadX = bodyPart.GetX();
            double snakeHeadY = bodyPart.GetY();


            if (p1.GetX() - wallWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + wallWidth + snakeWidth
                && p1.GetY() - wallWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + wallWidth + snakeWidth)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// This method is used to determine if the snake has collided with itself.
    /// </summary>
    /// <param name="snake">The snake used to check for the collisions of itself</param>
    /// <returns>Returns true if the snake has collided with itself, false otherwise.</returns>
    private bool SnakeCollisionSelf(Snake snake)
    {
        int snakeWidth = 10;

        if (snake.body.Count < 5)
        {
            return false;
        }

        Vector2D head = snake.body.Last();

        for (int i = 0; i < snake.body.Count - 4; i++)
        {
            int j = i + 1;

            Vector2D p1 = snake.body[i];
            Vector2D p2 = snake.body[j];

            if ((p1.GetX() > p2.GetX()) || (p1.GetY() > p2.GetY()))
            {
                p1 = p2;
                p2 = p1;
            }

            double snakeHeadX = head.GetX();
            double snakeHeadY = head.GetY();


            if (p1.GetX() - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + snakeWidth
                && p1.GetY() - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// This method is used to check if the snake has collided with all the
    /// other snakes in the world.
    /// </summary>
    /// <param name="snake">The snake used to check for collisions with all the other snakes.</param>
    /// <returns>Returns true if the snake has collided wih another snake, false otherwise.</returns>
    private bool SnakeCollisionSnake(Snake snake)
    {
        foreach (Snake p in theWorld.Snakes.Values)
        {
            if (p.snake == snake.snake || p.died)
            {
                continue;
            }

            int snakeWidth = 10;

            Vector2D p1;
            Vector2D p2;
            if (p.Equals(snake))
            {
                for (int i = 0; i < p.body.Count() - 2; i++)
                {
                    if ((p.body[i].GetX() < p.body[i + 1].GetX()) || (p.body[i].GetY() < p.body[i].GetY()))
                    {
                        p1 = p.body[i];
                        p2 = p.body[i + 1];
                    }
                    else
                    {
                        p1 = p.body[i + 1];
                        p2 = p.body[i];
                    }

                    Vector2D head = snake.body.Last();
                    double snakeHeadX = head.GetX();
                    double snakeHeadY = head.GetY();

                    if (p1.GetX() - snakeWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + snakeWidth + snakeWidth
                        && p1.GetY() - snakeWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth + snakeWidth)
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < p.body.Count() - 1; i++)
                {
                    if ((p.body[i].GetX() < p.body[i + 1].GetX()) || (p.body[i].GetY() < p.body[i].GetY()))
                    {
                        p1 = p.body[i];
                        p2 = p.body[i + 1];
                    }
                    else
                    {
                        p1 = p.body[i + 1];
                        p2 = p.body[i];
                    }

                    Vector2D head = snake.body.Last();
                    double snakeHeadX = head.GetX();
                    double snakeHeadY = head.GetY();

                    if (p1.GetX() - snakeWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + snakeWidth + snakeWidth
                        && p1.GetY() - snakeWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth + snakeWidth)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// This method will spawn a snake in a location where it does not collide
    /// with other snakes, walls, and powerups in the world.
    /// </summary>
    /// <param name="ID">The id of the client to create the snake.</param>
    /// <param name="name">The name of the snake.</param>
    /// <returns>Returns the a new snake that is not colliding with other objects in the world.</returns>
    private Snake SnakeSpawn(int ID, string name)
    {
        double headX = rand.Next(-size / 2, size / 2);
        double headY = rand.Next(-size / 2, size / 2);


        int dirX = rand.Next(-1, 2);
        int dirY;

        if (dirX == 0)
        {
            dirY = rand.Next(-1, 1) + 1;
        }
        else
        {
            dirY = 0;
        }

        Vector2D dir = new Vector2D(dirX, dirY);
        Vector2D head = new Vector2D(headX, headY);
        Vector2D tail = new Vector2D(0, 0);

        if (dir.ToAngle() == 0)
        {
            tail = new Vector2D(headX, headY + 120);
        }
        else if (dir.ToAngle() == 90)
        {
            tail = new Vector2D(headX - 120, headY);
        }
        else if (dir.ToAngle() == 180)
        {
            tail = new Vector2D(headX, headY - 120);
        }
        else if (dir.ToAngle() == -90)
        {
            tail = new Vector2D(headX + 120, headY);
        }
        List<Vector2D> body = new List<Vector2D>();
        body.Add(tail);
        body.Add(head);

        Snake snake = new Snake(ID, body, dir, name, 0, false, true, false, true);


        bool isSnakeCollidingWall = !(this.SnakeBodyCollision(snake, 0) ||
                                    this.SnakeBodyCollision(snake, 1) ||
                                    this.SnakeWallCollision(snake));

        while (!isSnakeCollidingWall)
        {
            headX = rand.Next(-size / 2, size / 2);
            headY = rand.Next(-size / 2, size / 2);


            dirX = rand.Next(-1, 2);

            if (dirX == 0)
            {
                dirY = rand.Next(-1, 1) + 1;
            }
            else
            {
                dirY = 0;
            }

            dir = new Vector2D(dirX, dirY);
            head = new Vector2D(headX, headY);
            tail = new Vector2D(0, 0);

            if (dir.ToAngle() == 0)
            {
                tail = new Vector2D(headX, headY + 120);
            }
            else if (dir.ToAngle() == 90)
            {
                tail = new Vector2D(headX - 120, headY);
            }
            else if (dir.ToAngle() == 180)
            {
                tail = new Vector2D(headX, headY - 120);
            }
            else if (dir.ToAngle() == -90)
            {
                tail = new Vector2D(headX + 120, headY);
            }
            body = new List<Vector2D>();
            body.Add(tail);
            body.Add(head);

            snake = new Snake(ID, body, dir, name, 0, false, true, false, true);


            isSnakeCollidingWall = !(this.SnakeBodyCollision(snake, 0)
                                || this.SnakeBodyCollision(snake, 1)
                                || this.SnakeWallCollision(snake));
        }

        return snake;
    }

    /// <summary>
    /// This method will spawn powerups in the world that will not collide
    /// with snakes, and walls in the world.
    /// </summary>
    /// <param name="offset">An offset for the powerup to not collide with walls.</param>
    /// <returns>Returns a Vector2D that is the location of the powerup.</returns>
    private Vector2D PowerupSpawn(int offset)
    {

        int powerupWidth = 10;
        int wallWidth = 25;

        foreach (Wall wall in this.theWorld.Walls.Values)
        {

            double locationX = rand.Next(-size / 2 + 100, size / 2 - 50);
            double locationY = rand.Next(-size / 2 + 100, size / 2 - 50);

            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D location = new Vector2D(locationX, locationY);

            bool collision = false;

            while (!collision)
            {
                if (p1.GetX() - wallWidth - powerupWidth - offset < locationX && locationX < p2.GetX() + wallWidth + powerupWidth + offset
                && p1.GetY() - wallWidth - powerupWidth - offset < locationY && locationY < p2.GetY() + wallWidth + powerupWidth + offset)
                {
                    collision = true;
                }

                if (locationX > size || locationX < -size ||
                locationY > size || locationY < -size)
                {
                    collision = true;
                }

                locationX = rand.Next(-size / 2, size / 2);
                locationY = rand.Next(-size / 2, size / 2);
            }
            return location;


        }
        return new Vector2D();
    }

    /// <summary>
    /// This method will check if a snake has collided with a powerup in the world.
    /// </summary>
    /// <param name="snake">The snake used to check for collisions with all the powerups.</param>
    /// <returns>Returns true if the snake has collided with a powerup.</returns>
    private bool SnakePowerupCollision(Snake snake)
    {
        int snakeWidth = 10;
        int powerupWidth = 10;
        foreach (PowerUp powerUp in this.theWorld.PowerUps.Values)
        {
            if ((powerUp.loc - snake.body.Last()).Length() < snakeWidth + powerupWidth)
            {
                powerUp.died = true;
                return true;
            }
        }
        return false;
    }
}
