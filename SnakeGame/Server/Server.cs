//Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023. 

using System.Diagnostics;
using System.Xml;
using Server;
using System.Runtime.Serialization;
namespace Server
{
    /// <summary>
    /// This class represents the Snake server for the Snake clients.
    /// </summary>
    class Server
    {
        static void Main(string[] args)
        {
            DataContractSerializer ser = new(typeof(GameSettings));

            XmlReader reader = XmlReader.Create("settings.xml");

            GameSettings? gameSettings = ser.ReadObject(reader) as GameSettings;

#pragma warning disable CS8604 // Possible null reference argument.
            ServerController server = new ServerController(gameSettings);
#pragma warning restore CS8604 // Possible null reference argument.

            //Console.WriteLine("Starting Server");
            server.BeginServer();

            //Console.WriteLine("Starting Frame loop");
            Thread t = new Thread(server.Run);
            t.Start();
        }
    }
}

