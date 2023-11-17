using System;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Model;
using NetworkUtil;
namespace GameController
{
    public class GameController
    {
        private World? world = null;
        public delegate void JSONHandler(IEnumerable<string> messages);
        public event JSONHandler? JSONArrived;
        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

        private SocketState? server = null;



        public void StartSend(string playerID)
        {
            playerID += "\n";
            Console.WriteLine("starting to send: " + playerID);

            Networking.Send(server.TheSocket, playerID );

        }

        public void JoinServer(string hostname)
        {
            Networking.ConnectToServer(OnConnect, hostname, 11000);
        }

        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Error Connecting to the server");
            }

            server = state;

            Connected?.Invoke();

            state.OnNetworkAction = ReceiveMessage;
            //state.OnNetworkAction = ReceiveJSON;

            Console.WriteLine("Connected");
            Networking.GetData(state);
        }

        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Lost connection to server");
                return;
            }
            ProcessMessages(state);

            // Continue the event loop
            // state.OnNetworkAction has not been changed, 
            // so this same method (ReceiveMessage) 
            // will be invoked when more data arrives
            Networking.GetData(state);
        }

        private void ProcessMessages(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.

            List<string> newMessages = new List<string>();

            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                // build a list of messages to send to the view
                newMessages.Add(p);


                Console.WriteLine("debug");

                if (world is null && newMessages.Count == 2)
                {
                    Int32.TryParse(newMessages.ElementAt<string>(1), out int worldSize);
                    World world = new World(worldSize);
                }

                if (Int32.TryParse(p, out int worldSizez))
                {
                    continue;
                }

                string data = "{\"wall\":0,\"p1\":{\"x\":-575.0,\"y\":-575.0},\"p2\":{\"x\":575.0,\"y\":-575.0}}";
                JsonDocument doc = JsonDocument.Parse(data);

             
                if (doc.RootElement.TryGetProperty("wall", out _))
                {
                    Console.WriteLine(data);
                    Wall wall = JsonSerializer.Deserialize<Wall>(data);
                    this.world.Walls[wall.wall] = wall;
                }
                else if (doc.RootElement.TryGetProperty("snake", out _))
                {
                    Console.WriteLine(doc.ToString());
                    Snake player = JsonSerializer.Deserialize<Snake>(doc);
                    this.world.Snakes[player.snake] = player;
                }

                else if (doc.RootElement.TryGetProperty("power", out _))
                {
                    Console.WriteLine(doc.ToString());
                    PowerUp powerUp = JsonSerializer.Deserialize<PowerUp>(doc);
                    this.world.PowerUps[powerUp.power] = powerUp;
                }

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }


            foreach (string message in parts)
            {
                Console.WriteLine(message);
            }

            Console.WriteLine("debug");
            // inform the view
            //MessagesArrived?.Invoke(newMessages);
        }

        private void ReceiveJSON(SocketState state)
        {
            Console.WriteLine("Receiving Started");
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Connection to the Server was interupted");
                return;
            }
            ParseJSON(state);
            Networking.GetData(state);
        }

        private void ParseJSON(SocketState state)
        {
            Console.WriteLine("Attempting to Parse");
            JsonDocument doc = JsonDocument.Parse(state.GetData());
            if (doc.RootElement.TryGetProperty("wall", out _))
                Console.WriteLine(doc.ToString());
        }

        private void UpdateCameFromServer(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups)
        {
            //Random r = new Random(); // ignore this for now

            // The server is not required to send updates about every object,
            // so we update our local copy of the world only for the objects that
            // the server gave us an update for.
            foreach (Snake play in players)
            {
                //while (r.Next() % 1000 != 0) ; // ignore this loop for now
                if (!play.alive)
                    world.Snakes.Remove(play.snake);
                else
                    world.Snakes[play.snake] = play;
            }

            foreach (PowerUp pow in powerups)
            {
                if (pow.died)
                    world.PowerUps.Remove(pow.power);
                else
                    world.PowerUps[pow.power] = pow;
            }
            // Notify any listeners (the view) that a new game world has arrived from the server
            //UpdateArrived?.Invoke();

            // TODO: for whatever user inputs happened during the last frame, process them.
        }

    }
}

