using System.Diagnostics;
using System.Xml;
using Server;
using System.Runtime.Serialization;
namespace Server
{

    class Server
    {
        static void Main(string[] args)
        {
            DataContractSerializer ser = new(typeof(GameSettings));

            XmlReader reader = XmlReader.Create("settings.xml");

            GameSettings gameSettings = (GameSettings)ser.ReadObject(reader);

            ServerController server = new ServerController(gameSettings);

            //Console.WriteLine("Starting Server");
            server.BeginServer();

            //Console.WriteLine("Starting Frame loop");
            Thread t = new Thread(server.Run);
            t.Start();
        }
    }
}

