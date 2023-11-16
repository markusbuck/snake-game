using System;
using Model;
using NetworkUtil;
namespace GameController
{
	public class GameController
	{
		private World world;
		public GameController()
		{
		}

		public void JoinServer(Action<SocketState> toCall, string hostname)
		{
			Networking.ConnectToServer(toCall, hostname, 11000);
		}
	}
}

