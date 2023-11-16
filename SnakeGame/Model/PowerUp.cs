using System;
using SnakeGame;
using System.Text.Json.Serialization;
namespace Model
{
	public class PowerUp
	{
		public int power;
		public Vector2D loc;
		public bool died;

		[JsonConstructor]
		public PowerUp(int power, Vector2D loc, bool died)
		{
			this.power = power;
			this.loc = loc;
			this.died = died;
		}
	}
}

