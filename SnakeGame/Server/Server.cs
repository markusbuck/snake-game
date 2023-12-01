using System.Diagnostics;
using System.Xml;
using Server;
namespace Server
{

    class Server
    {
        static void Main(string[] args)
        {
            XmlWriter xmlWriter = XmlWriter.Create("setting.xml");
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("GameSettings");
            ServerController server = new ServerController(2000);

            Console.WriteLine("Starting Server");
            server.BeginServer();

            Console.WriteLine("Starting Frame loop");
            Thread t = new Thread(server.Run);
            t.Start();
        }
    }
}

