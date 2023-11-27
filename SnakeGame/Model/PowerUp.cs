// Authors: Kevin Soto-Mirada 2023, Markus Buckwalter 2023.

using System;
using SnakeGame;
using System.Text.Json.Serialization;
namespace Model
{
    /// <summary>
    /// A class to represent a powerup in the snake game.
    /// </summary>
    public class PowerUp
	{
		public int power{ get; set; }
        public Vector2D loc { get; set; }
        public bool died { get; set; }

        /// <summary>
        /// Constructor for Json Deserializing that creates a powerup, given an ID,
        /// the location of the powerup, and a bool for if it has died.
        /// </summary>
        /// <param name="power">An ID for the powerup.</param>
        /// <param name="loc">The location of the powerup in the world.</param>
        /// <param name="died">A bool for if the powerup died.</param>
        [JsonConstructor]
		public PowerUp(int power, Vector2D loc, bool died)
		{
			this.power = power;
			this.loc = loc;
			this.died = died;
		}
	}
}

