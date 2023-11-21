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

        private void UpdateCameFromServer(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups)
        {
            //Random r = new Random(); // ignore this for now

            // The server is not required to send updates about every object,
            // so we update our local copy of the world only for the objects that
            // the server gave us an update for.
            foreach (Snake play in players)
            {
                //while (r.Next() % 1000 != 0) ; // ignore this loop for now
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
            // Notify any listeners (the view) that a new game world has arrived from the server
            //UpdateArrived?.Invoke();

            // TODO: for whatever user inputs happened during the last frame, process them.
        }
    }
}

