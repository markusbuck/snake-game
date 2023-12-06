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

        public void Step(int speed, string movementRequest)
        {
            int snakeSpeed = speed;
            if (this.dir.Equals(new Vector2D(0, 0)))
            {
                this.dir = (new Vector2D(1, 0));
            }
            Vector2D velocity = this.dir * snakeSpeed;
            Console.WriteLine("velcocity: " + velocity.ToString());
            if (this.died)
            {
                velocity = new Vector2D(0, 0);
            }
            else if (movementRequest == "right")
            {
                this.dir = new Vector2D(1, 0);
                velocity = this.dir * snakeSpeed;
                this.body.Add(this.body.Last() + velocity);
            }
            else if (movementRequest == "left")
            {
                this.dir = new Vector2D(-1, 0);
                velocity = this.dir * snakeSpeed;
                this.body.Add(this.body.Last() + velocity);
            }
            else if (movementRequest == "up")
            {
                this.dir = new Vector2D(0, -1);
                velocity = this.dir * snakeSpeed;
                this.body.Add(this.body.Last() + velocity);
            }
            else if (movementRequest == "down")
            {
                this.dir = new Vector2D(0, 1);
                velocity = this.dir * snakeSpeed;
                this.body.Add(this.body.Last() + velocity);
            }
            else
            {
                this.body[this.body.Count - 1] += velocity;
            }
            float angle = Vector2D.AngleBetweenPoints(this.body[0], this.body[1]);
            if (this.died)
            {
                this.body[0] += new Vector2D(0, 0) * snakeSpeed;
            }
            else if (angle == 90)
            {
                this.body[0] += new Vector2D(-1, 0) * snakeSpeed;
            }
            else if (angle == -90)
            {
                this.body[0] += new Vector2D(1, 0) * snakeSpeed;
            }
            else if (angle == 0)
            {
                this.body[0] += new Vector2D(0, 1) * snakeSpeed;
            }
            else if (angle == 180)
            {
                this.body[0] += new Vector2D(0, -1) * snakeSpeed;
            }
            if (this.body[0].Equals(this.body[1]))
            {
                this.body.RemoveAt(0);
            }
        }
    }
}

