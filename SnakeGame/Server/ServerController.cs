using System.Numerics;
using System.Xml;
using SnakeGame;
using Model;
using NetworkUtil;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;

namespace Server;

public class ServerController
{
    private World theWorld;
    private Random rand = new();

    public delegate void ServerUpdateHandler(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups);
    public event ServerUpdateHandler? ServerUpdate;

    // settings
    private long msPerFrame = 34;
    private int maxPlayers = 50;
    private int maxPowerups = 20;
    private int RespawnRate = 100;
    private int size = 2000;

    private int nextPlayerID = 0;
    private int nextPowID = 0;
    private GameSettings gameSettings;

    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;


    public ServerController(GameSettings gameSettings)
    {
        this.gameSettings = gameSettings;
        this.size = gameSettings.UniverseSize;
        this.theWorld = new World(this.size);

        foreach(Wall wall in this.gameSettings.Walls)
        {
            this.theWorld.Walls[wall.wall] = wall;
        }

        this.clients = new Dictionary<long, SocketState>();
    }

    public void BeginServer()
    {
        Console.WriteLine("Server started");
        Networking.StartServer(NewClientConnected, 11000);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects (see line 41)
    /// </summary>
    /// <param name="state">The SocketState representing the new client</param>
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

        // Save the client state
        // Need to lock here because clients can disconnect at any time
        lock (clients)
        {
            clients[state.ID] = state;
        }
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a network action occurs (see lines 64-66)
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        // TODO : This was called when the client gets closed, idk why 
        //state.OnNetworkAction = ProcessMessage;

        ProcessMessage(state);

        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }


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
            Console.WriteLine("received message from client " + state.ID + ": \"" + p.Substring(0, p.Length - 1) + "\"");


            // I left it static for now
            double headX = rand.Next(-size / 2, size / 2);
            double headY = rand.Next(-size / 2, size / 2);

            double tailX = headX - 120;
            double tailY = headY;

            Vector2D headVector = new Vector2D(headX, headY);
            Vector2D tailVector = new Vector2D(tailX, tailY);

            List<Vector2D> body = new List<Vector2D>();
            body.Add(tailVector);
            body.Add(headVector);

            Vector2D dir = new Vector2D(1, 0);
            Snake snake = new Snake((int)state.ID, body, dir, p, 0, false, true, false, true);

            this.theWorld.Snakes[(int)state.ID] = snake;

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);

            // Broadcast the message to all clients
            // Lock here beccause we can't have new connections 
            // adding while looping through the clients list.
            // We also need to remove any disconnected clients.
            HashSet<long> disconnectedClients = new HashSet<long>();
            lock (clients)
            {
                foreach (SocketState client in clients.Values)
                {
                    //TODO: change sending info
                    if (!Networking.Send(client.TheSocket!, "Message from client " + state.ID + ": " + p))
                        disconnectedClients.Add(client.ID);

                }
            }

            foreach (long id in disconnectedClients)
                RemoveClient(id);

            state.OnNetworkAction = CommandRequest;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(state.ID + "\n" + this.size + "\n");

            foreach(Wall wall in this.gameSettings.Walls)
            {
                string jsonString = JsonSerializer.Serialize(wall);
                
                stringBuilder.Append(jsonString + "\n");
            }

            Console.WriteLine(stringBuilder.ToString());
            Networking.Send(state.TheSocket,stringBuilder.ToString());
        }
    }


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
                snake.dir.X = 0;
                snake.dir.Y = -1;

                snake.body.Add(new Vector2D(head.GetX(), head.GetY()));
            }

            if (p.Contains("left"))
            {
                //snake.body.Add(new Vector2D());
                snake.dir.X = -1;
                snake.dir.Y = 0;
            }

            if (p.Contains("down"))
            {
                //snake.body.Add(new Vector2D());
                snake.dir.X = 0;
                snake.dir.Y = 1;
              
            }

            if (p.Contains("right"))
            {
                //snake.body.Add(new Vector2D());
                snake.dir.X = 1;
                snake.dir.Y = 0;
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
            clients.Remove(id);
        }
    }

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

            //Console.WriteLine(watch.ElapsedMilliseconds);
            watch.Restart();


            Update();

            ServerUpdate?.Invoke(theWorld.Snakes.Values, theWorld.PowerUps.Values);

        }
    }

    private void Update()
    {
        // cleanup the deactivated objects
        IEnumerable<int> playersToRemove = theWorld.Snakes.Values.Where(x => !x.alive).Select(x => x.snake);
        IEnumerable<int> powsToRemove = theWorld.PowerUps.Values.Where(x => x.died).Select(x => x.power);
        foreach (int i in playersToRemove)
            theWorld.Snakes.Remove(i);
        foreach (int i in powsToRemove)
            theWorld.PowerUps.Remove(i);

        // add new objects back in
        int halfSize = size / 2;

        // TODO: This is an infinite loop which is why it only printed the frame rate once
        //while (theWorld.Snakes.Count < maxPlayers)
        //{
        //    // TODO: Figure out correct params to enter into the constructor
        //    //Snake p = new Snake(nextPlayerID++, -halfSize + rand.Next(size), -halfSize + rand.Next(size), rand.NextDouble() * 360);
        //    //theWorld.Snakes.Add(p.snake, p);
        //}

        //while (theWorld.PowerUps.Count < maxPowerups)
        //{
        //    PowerUp p = new PowerUp(nextPowID++, new Vector2D(-halfSize + rand.Next(size), -halfSize + rand.Next(size)), false);
        //    theWorld.PowerUps.Add(p.power, p);
        //}

        // move/update the existing objects in the world

        StringBuilder stringBuilder = new StringBuilder();

        foreach (Snake p in theWorld.Snakes.Values)
        {

            int snakeSpeed = 6;
            //Vector2D head = p.body.ElementAt();

            for(int i = p.body.Count - 1; i >= 0; i--)
            {
                Vector2D bodyPart = p.body.ElementAt(i);
                bodyPart.X += p.dir.X * snakeSpeed;
                bodyPart.Y += p.dir.Y * snakeSpeed;
            }

            //Vector2D tail = p.body.ElementAt(0);
            //head.X += p.dir.X * snakeSpeed;
            //head.Y += p.dir.Y * snakeSpeed;

            //tail.X += p.dir.X * snakeSpeed;
            //tail.Y += p.dir.Y * snakeSpeed;

            stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
        }

        foreach(SocketState state in this.clients.Values)
        {

            Networking.Send(state.TheSocket, stringBuilder.ToString());
        }

        //Add moving method into
        //p.Step(theWorld.Size);
        //REMOVE RETURN
        return;

        foreach (PowerUp p in theWorld.PowerUps.Values)
            //p.Step();
            // REMOVE RETURN
            return;

    }

    private bool SnakeWallCollision(Snake snake)
    {
        foreach(Wall wall in this.theWorld.Walls.Values)
        {
            Vector2D p1 = wall.p1;
            Vector2D p2 = wall.p2;

            Vector2D head = snake.body[snake.body.Count - 1];
            Vector2D secondBody = snake.body[snake.body.Count - 2];

            //snake.dir;

            //if()
            //{
            //    return true;
            //}
                
        }
        return false;
    }
}
