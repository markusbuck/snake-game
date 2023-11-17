using System;
using System.Text.Json;
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
			Console.WriteLine("starting to send: " + playerID);
		
			Networking.Send(server.TheSocket, playerID);
		
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

            state.OnNetworkAction = ReceiveJSON;
            
            Console.WriteLine("Connected");
            Networking.GetData(state);
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
            if (doc.RootElement.TryGetProperty("snake", out _))
                Console.WriteLine(doc.ToString());
        }
	}
}

