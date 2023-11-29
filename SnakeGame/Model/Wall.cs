// Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023.

using System;
using SnakeGame;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Model
{
    /// <summary>
    /// This class represents a wall in the snake game.
    /// </summary>
  
    public class Wall
	{
		// Property for the wall ID
		[XmlElement("ID")]
		public int wall { get; set; }

        // Property for the location of the wall in the game
        [XmlElement("p1")]
		public Vector2D p1 { get; set; }

        // Property for the location of the wall in the game
        [XmlElement("p2")]
        public Vector2D p2 { get; set; }

        /// <summary>
        /// Constructor for Json Deserializing that creates a wall, given an ID,
        /// and two vector points for the location.
        /// </summary>
        /// <param name="wall">The ID of the wall.</param>
        /// <param name="p1">A point where the wall starts or ends.</param>
        /// <param name="p2">A point where the wall starts or ends.</param>
        [JsonConstructor]
		public Wall(int wall, Vector2D p1, Vector2D p2)
		{
			this.wall = wall;
			this.p1 = p1;
			this.p2 = p2;
		}
	}
}

