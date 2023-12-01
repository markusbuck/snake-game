using System;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Model;
using Server;
using System.Xml;

namespace Server
{
    [DataContract(Namespace ="")]
    public class GameSettings
    {
        [DataMember(Name = "MSPerFrame")]
        public int MSPerFram { get; private set; }

        [DataMember(Name = "RespawnRate")]
        public int RespawnRate { get; private set; }

        [DataMember(Name = "UniverseSize")]
        public int UniverseSize { get; private set; }

        [DataMember(Name ="Walls")]
        public Wall[] Walls { get; private set; }
    }


    //public static class Test
    //{
    //    static void Main(string[] args)
    //    {
    //        DataContractSerializer ser = new(typeof(GameSettings));

    //        XmlReader reader = XmlReader.Create("settings.xml");

    //        GameSettings settings = (GameSettings)ser.ReadObject(reader);
    //        Console.Read();
    //    }
    //}
}