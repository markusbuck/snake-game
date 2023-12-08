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

        public long frameRate { get; set; }

        private bool recievedPowerup;

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

        /// <summary>
        /// This method will change the snakes direction depending on the movement
        /// request it recieved.
        /// </summary>
        /// <param name="movementRequest">The direction the client requested.</param>
        /// <param name="speed"></param>
        public void ChangeDirection(string movementRequest)
        {
            Vector2D right = new Vector2D(1, 0);
            Vector2D left = new Vector2D(-1, 0);
            Vector2D up = new Vector2D(0, -1);
            Vector2D down = new Vector2D(0, 1);

            if (movementRequest == "right" && !this.dir.Equals(left))
            {
                this.dir = new Vector2D(1, 0);
            }
            else if (movementRequest == "left" && !this.dir.Equals(right))
            {

                this.dir = new Vector2D(-1, 0);
            }
            else if (movementRequest == "up" && !this.dir.Equals(down))
            {
                this.dir = new Vector2D(0, -1);
            }
            else if (movementRequest == "down" && !this.dir.Equals(up))
            {
                this.dir = new Vector2D(0, 1);
            }
            this.body.Add(this.body.Last());
        }

        /// <summary>
        /// This method will increase the snakes length when a powerup is collected
        /// by the specified framerate.
        /// </summary>
        public void RecievedPowerup()
        {
            long frameRate = this.frameRate;
            double deltaTime = 1000 / frameRate;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            this.recievedPowerup = true;
            watch.Start();
            // wait until the next frame
            while (watch.ElapsedMilliseconds < 24 * deltaTime)
            { /* empty loop body */}

            watch.Stop();
            this.recievedPowerup = false;

        }

        /// <summary>
        /// This method will add a velocity to the snake head depending
        /// on the angle its going towards.
        /// </summary>
        /// <param name="speed">The speed of the snake.</param>
        /// <param name="movementRequest"></param>
        public void Step(int speed)
        {
            int snakeSpeed = speed;
            if (this.dir.Equals(new Vector2D(0, 0)))
            {
                this.dir = (new Vector2D(1, 0));
            }

            Vector2D velocity = this.dir * snakeSpeed;
            this.body[this.body.Count - 1] += velocity; // head

            if (this.body.Count < 2)
            {
                return;
            }

            float poweredUp = this.recievedPowerup ? 0 : 1;
            float angle = Vector2D.AngleBetweenPoints(this.body[0], this.body[1]);
            if (this.died)
            {
                this.body[0] += new Vector2D(0, 0) * snakeSpeed;
            }
            else if (angle == 90)
            {
                this.body[0] += new Vector2D(-1, 0) * snakeSpeed * poweredUp;
            }
            else if (angle == -90)
            {
                this.body[0] += new Vector2D(1, 0) * snakeSpeed * poweredUp;
            }
            else if (angle == 0)
            {
                this.body[0] += new Vector2D(0, 1) * snakeSpeed * poweredUp;
            }
            else if (angle == 180)
            {
                this.body[0] += new Vector2D(0, -1) * snakeSpeed * poweredUp;
            }
            if (this.body[0].Equals(this.body[1]))
            {
                this.body.RemoveAt(0);
            }
        }
    }
}