//Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023. 

using System;
using System.Text.Json.Serialization;
using SnakeGame;
namespace Model
{
    /// <summary>
    /// This class represents a snake in the snake game.
    /// </summary>
	public class Snake
	{
		public int snake { get; set; }
		public string name { get; set; }
        public List<Vector2D> body { get; set; }
        public Vector2D dir { get; set; }
        public int score { get; set; }
        public bool died { get; set; }
        public bool alive { get; set; }
        public bool dc { get; set; }
        public bool join { get; set; }

        /// <summary>
        /// Constructor for Json Deserializing that creates a snake, given an ID,
        /// a list of Vector2D points for the snake body, a name, the total score,
        /// a bool representing if they died, if theyre alive, if theyve disconnected from the server,
        /// and if they joined the server.
        /// </summary>
        /// <param name="snake">The ID of the snake.</param>
        /// <param name="body">A list of points for the body.</param>
        /// <param name="dir">The direction the snake is facing.</param>
        /// <param name="name">The name of the snake.</param>
        /// <param name="score">The total number points.</param>
        /// <param name="died">Bool representing if they have died.</param>
        /// <param name="alive">Bool representing if they are alive.</param>
        /// <param name="dc">Bool representing if they've disconnected from the server.</param>
        /// <param name="join"Bool representing if they've connected to the server.></param>
        [JsonConstructor]
		public Snake(int snake, List<Vector2D> body, Vector2D dir, string name, int score, bool died, bool alive, bool dc, bool join)
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

