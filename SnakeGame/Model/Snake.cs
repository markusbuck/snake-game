using System;
using System.Text.Json.Serialization;
using SnakeGame;
namespace Model
{
	public class Snake
	{

		public int snake;
		public string name;
		public Vector2D body;
		public Vector2D dir;
		public int score;
		public bool died;
		public bool alive;
		public bool dc;
		public bool join;

		[JsonConstructor]
		public Snake(int snake, string name, Vector2D body, Vector2D dir, int score, bool died, bool alive, bool dc, bool join)
		{
			this.snake = snake;
			this.name = name;
			this.body = body;
			this.dir = dir;
			this.score = score;
			this.died = died;
			this.alive = alive;
			this.dc = dc;
			this.join = join;
		}


	}
}

