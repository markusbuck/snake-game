// Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023

using System;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Model;
using NetworkUtil;
using SnakeGame;
namespace GameController
{
    /// <summary>
    /// A class representing the Controller in the MVC structure.
    /// </summary>
    public class GameController
    {
        public World? world { get; private set; }
        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;
        public delegate void UpdateHandler();
        public event UpdateHandler? Updated;
        private SocketState? server = null;

        /// <summary>
        /// This method will send a message of the players ID to the server. 
        /// </summary>
        /// <param name="playerID">The ID of the player.</param>
        public void StartSend(string playerID)
        {
            playerID += "\n";
            Networking.Send(server.TheSocket, playerID );
        }

        /// <summary>
        /// This method will start a connection process with a given host name.
        /// </summary>
        /// <param name="hostname">The name of the host to connect to.</param>
        public void JoinServer(string hostname)
        {
            Networking.ConnectToServer(OnConnect, hostname, 11000);
        }

        /// <summary>
        /// This method will start getting data from the server when the
        /// player has connected to the server.
        /// </summary>
        /// <param name="state">The SocketState to begin receiving.</param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Error Connecting to the server");
            }

            server = state;

            Connected?.Invoke();

            state.OnNetworkAction = ReceiveMessage;
            
            Networking.GetData(state);
        }

        /// <summary>
        /// This method will continue the event loop, and see if an
        /// error has occured, if so it will inform the player an error
        /// has occured, otherwise it will process the message from the server.
        /// </summary>
        /// <param name="state"></param>
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

        /// <summary>
        /// A method to process the messages recieved from the server, and
        /// will create Snakes, Powerups, Walls, in a world, and will inform the
        /// view to update.
        /// </summary>
        /// <param name="state">The SocketState to begin receiving.</param>
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

                if (world is null && newMessages.Count == 2)
                {
                    Int32.TryParse(newMessages.ElementAt<string>(1), out int worldSize);
                    Int32.TryParse(newMessages.ElementAt(0), out int snakeID);
                    this.world = new World(worldSize, snakeID);
                    state.RemoveData(0, p.Length);
                    continue;
                }

                if (Int32.TryParse(p, out int worldSizez))
                {
                    state.RemoveData(0, p.Length);
                    continue;
                }

                lock (world)
                {
                    JsonDocument doc = JsonDocument.Parse(p);
                    if (doc.RootElement.TryGetProperty("snake", out _))
                    {
                        Snake? player = JsonSerializer.Deserialize<Snake>(doc);
                        this.world.Snakes[player.snake] = player;
                    }
                    else if (doc.RootElement.TryGetProperty("wall", out _))
                    {
                        Wall? wall = JsonSerializer.Deserialize<Wall>(doc);
                        world.Walls[wall.wall] = wall;
                    }

                    if (doc.RootElement.TryGetProperty("power", out _))
                    {
                        PowerUp? powerUp = JsonSerializer.Deserialize<PowerUp>(doc);
                        this.world.PowerUps[powerUp.power] = powerUp;
                    }
                }

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }

            // inform the view
            Updated?.Invoke();
        }

        /// <summary>
        /// This method is used to send a movement command to
        /// let the server know where the player is wanting to move.
        /// </summary>
        /// <param name="dir">A string containing the direction the player is moving to.</param>
        public void SendCommand(string dir)
        {
            string item = "{\"moving\":\"" + dir + "\"}\n";
            Networking.Send(server.TheSocket, item);
        }
    }
}