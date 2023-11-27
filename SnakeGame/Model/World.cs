//Authors: Kevin Soto-Miranda 2023, Markus Buckwalter 2023.

using System;

namespace Model
{
    /// <summary>
    /// A class to represent the world in the snake game.
    /// </summary>
	public class World
	{
		public Dictionary<int, Snake> Snakes;
		public Dictionary<int, PowerUp> PowerUps;
		public Dictionary<int, Wall> Walls;
		public int Size { get; private set; }
        public int CurrentSnake { get; private set; }

        /// <summary>
        /// A constructor to create a world with a given size, and the ID of
        /// a snake in the world.
        /// </summary>
        /// <param name="_size">The size of a world.</param>
        /// <param name="currentSnake">The ID of a snake.</param>
		public World(int _size, int currentSnake)
		{
			Snakes = new Dictionary<int, Snake>();
			PowerUps = new Dictionary<int, PowerUp>();
			Walls = new Dictionary<int, Wall>();
			Size = _size;
            this.CurrentSnake = currentSnake;
		}

        /// <summary>
        /// This method is called to check if the players and powerups in the world are alive, if not
        /// they will be removed from the world.
        /// </summary>
        /// <param name="players">A list of the players in the world.</param>
        /// <param name="powerups"A list of powerups in the world.</param>
        public void UpdateCameFromServer(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups)
        {
            lock (this)
            {
                foreach (Snake play in players)
                {
                    if (!play.alive)
                        Snakes.Remove(play.snake);
                    else
                        Snakes[play.snake] = play;
                }

                foreach (PowerUp pow in powerups)
                {
                    if (pow.died)
                        PowerUps.Remove(pow.power);
                    else
                        PowerUps[pow.power] = pow;
                }
            }
        }
    }
}

