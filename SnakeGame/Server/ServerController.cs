using System.Numerics;
using System.Xml;
using SnakeGame;
using Model;
using NetworkUtil;
using System.Text.RegularExpressions;

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

    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;


    public ServerController(int s)
    {
        size = s;
        theWorld = new(size);
        clients = new Dictionary<long, SocketState>();
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

        // Save the client state
        // Need to lock here because clients can disconnect at any time
        lock (clients)
        {
            clients[state.ID] = state;
        }

        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ReceiveMessage;

        Networking.GetData(state);
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

        state.OnNetworkAction = ProcessMessage;
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

            double headX = rand.Next(-size/2, size/2);
            double headY = rand.Next(-size / 2, size / 2);


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
                    //if (!Networking.Send(client.TheSocket!, "Message from client " + state.ID + ": " + p))
                        //disconnectedClients.Add(client.ID);
                }
            }
            foreach (long id in disconnectedClients)
                RemoveClient(id);
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
            { /* empty loop body */ }

            Console.WriteLine(watch.ElapsedMilliseconds);
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
        while (theWorld.Snakes.Count < maxPlayers)
        {
            // TODO: Figure out correct params to enter into the constructor
            //Snake p = new Snake(nextPlayerID++, -halfSize + rand.Next(size), -halfSize + rand.Next(size), rand.NextDouble() * 360);
            //theWorld.Snakes.Add(p.snake, p);
        }

        while (theWorld.PowerUps.Count < maxPowerups)
        {
            PowerUp p = new PowerUp(nextPowID++, new Vector2D(-halfSize + rand.Next(size), -halfSize + rand.Next(size)), false);
            theWorld.PowerUps.Add(p.power, p);
        }

        // move/update the existing objects in the world
        foreach (Snake p in theWorld.Snakes.Values)
            //Add moving method into
            //p.Step(theWorld.Size);
            //REMOVE RETURN
            return;

        foreach (PowerUp p in theWorld.PowerUps.Values)
            //p.Step();
            // REMOVE RETURN
            return;

    }
}
