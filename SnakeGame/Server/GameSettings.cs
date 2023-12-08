//Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023. 

using System;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Model;
using Server;
using System.Xml;

namespace Server
{
    /// <summary>
    /// This class reads a specified XML to be able to obtain its information
    /// for the server.
    /// </summary>
    [DataContract(Namespace ="")]
    public class GameSettings
    {
        [DataMember(Name = "MSPerFrame")]
        public int MSPerFrame { get; private set; }

        [DataMember(Name = "RespawnRate")]
        public int RespawnRate { get; private set; }

        [DataMember(Name = "UniverseSize")]
        public int UniverseSize { get; private set; }

        [DataMember(Name ="Walls")]
        public Wall[] Walls { get; private set; }
    }
}