using System;
using SnakeGame;

namespace Model
{
	public class World
	{
		public Dictionary<int, Snake> Snakes;
		public Dictionary<int, PowerUp> PowerUps;
		public Dictionary<int, Wall> Walls;
		public int Size
		{ get; private set; }

		public World(int _size)
		{

			Snakes = new Dictionary<int, Snake>();
			PowerUps = new Dictionary<int, PowerUp>();
			Walls = new Dictionary<int, Wall>();
			Size = _size;
		}
	}
}

