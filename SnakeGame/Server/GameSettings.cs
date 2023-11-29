using System;
using System.Xml.Serialization;
using Model;
using Server;

namespace Server
{
    [XmlRoot("GameSettings")]
    public class GameSettings
    {
        [XmlElement("MSPerFrame")]
        public int MSPerFram { get; private set; }


        [XmlElement("RespawnRate")]
        public int RespawnRate { get; private set; }


        [XmlElement("RespawnRate")]
        public int UniverseSize { get; private set; }

        [XmlArray("Walls")]
        [XmlArrayItem("Wall")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public List<Wall> Walls { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }


    public static class Test
    {
        static void Main(string[] args)
        {
            var serializer = new XmlSerializer(typeof(GameSettings));
            using (StreamReader reader = new StreamReader("library.xml"))
            {
                var walls = (GameSettings)serializer.Deserialize(reader);
                foreach (Wall wall in walls.Walls)
                {
                    Console.WriteLine($"P1: {wall.p1}, P2: {wall.p2}");
                }
            }
        }
    }
}