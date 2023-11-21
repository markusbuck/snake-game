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
        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;
        public delegate void UpdateHandler();
        public event UpdateHandler? Updated;

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

                if (world is null && newMessages.Count == 2)
                {
                    Int32.TryParse(newMessages.ElementAt<string>(1), out int worldSize);
                    this.world = new World(worldSize);
                    state.RemoveData(0, p.Length);
                    continue;
                }

                if (Int32.TryParse(p, out int worldSizez))
                {
                    state.RemoveData(0, p.Length);
                    continue;
                }

                JsonDocument doc = JsonDocument.Parse(p);
                if (doc.RootElement.TryGetProperty("wall", out _))
                {
                    Wall? wall = JsonSerializer.Deserialize<Wall>(doc);
                    world.Walls[wall.wall] = wall;
                }
                else if (doc.RootElement.TryGetProperty("snake", out _))
                { 
                    Snake? player = JsonSerializer.Deserialize<Snake>(doc);
                    this.world.Snakes[player.snake] = player;
                    Console.WriteLine(p);
                }

                else if (doc.RootElement.TryGetProperty("power", out _))
                {
                    PowerUp? powerUp = JsonSerializer.Deserialize<PowerUp>(doc);
                    this.world.PowerUps[powerUp.power] = powerUp;
                }

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }

            // inform the view
            Updated?.Invoke();
        }

        public void SendCommand(string dir)
        {
            Networking.Send(server.TheSocket, "{\"moving\": \"" + dir + "\"}");
        }
    }
}

