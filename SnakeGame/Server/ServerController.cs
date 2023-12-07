﻿using System.Numerics;
using System.Xml;
using SnakeGame;
using Model;
using NetworkUtil;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace Server;

public class ServerController
{
    private World theWorld;
    private Random rand = new();

    public delegate void ServerUpdateHandler(IEnumerable<Snake> players, IEnumerable<PowerUp> powerups);
    public event ServerUpdateHandler? ServerUpdate;

    // settings
    private long msPerFrame = 34;
    private int maxPlayers = 50;
    private int maxPowerups = 20;
    private int RespawnRate = 100;
    private int size = 2000;

    private int nextPlayerID = 0;
    private int nextPowID = 0;
    private GameSettings gameSettings;

    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;

    string movementRequest = "none";
    int snakeSpeed = 6;


    public ServerController(GameSettings gameSettings)
    {
        this.gameSettings = gameSettings;
        this.size = gameSettings.UniverseSize;
        this.theWorld = new World(this.size);

        foreach (Wall wall in this.gameSettings.Walls)
        {
            this.theWorld.Walls[wall.wall] = wall;
        }

        for (int i = 0; i < maxPowerups; i++)
        {
            PowerUp powerUp = new PowerUp(i, this.PowerupSpawn(25), false);
            this.theWorld.PowerUps[i] = powerUp;
        }

        this.clients = new Dictionary<long, SocketState>();
    }

    public void BeginServer()
    {
        Console.WriteLine("Server started");
        Networking.StartServer(NewClientConnected, 11000);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects (see line 41)
    /// </summary>
    /// <param name="state">The SocketState representing the new client</param>
    private void NewClientConnected(SocketState state)
    {
        Console.WriteLine("Client Connected");
        if (state.ErrorOccurred)
            return;

        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ReceiveMessage;

        Networking.GetData(state);

        // Save the client state
        // Need to lock here because clients can disconnect at any time
        lock (clients)
        {
            clients[state.ID] = state;
        }
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a network action occurs (see lines 64-66)
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        // TODO : This was called when the client gets closed, idk why 
        //state.OnNetworkAction = ProcessMessage;

        ProcessMessage(state);

        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }


    private void ProcessMessage(SocketState state)
    {
        string totalData = state.GetData();

        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until we have processed all messages.
        // We may have received more than one.
        foreach (string p in parts)
        {
            // Ignore empty strings added by the regex splitter
            if (p.Length == 0)
                continue;
            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;
            Console.WriteLine("received message from client " + state.ID + ": \"" + p.Substring(0, p.Length - 1) + "\"");


            // I left it static for now
            double headX = rand.Next(-size / 2, size / 2);
            double headY = rand.Next(-size / 2, size / 2);

            //double tailX = headX - 120;
            //double tailY = headY;

            //Vector2D headVector = new Vector2D(headX, headY);
            //Vector2D tailVector = new Vector2D(tailX, tailY);

            //List<Vector2D> body = new List<Vector2D>();
            //body.Add(tailVector);
            //body.Add(headVector);

            //Vector2D dir = new Vector2D(1, 0);
            //Snake snake = new Snake((int)state.ID, body, dir, p, 0, false, true, false, true);



            int dirX = rand.Next(-1, 2);
            int dirY;
            //Console.WriteLine(dirX);
            if (dirX == 0)
            {
                dirY = rand.Next(-1, 1) + 1;
            }
            else
            {
                dirY = 0;
            }

            //Console.WriteLine(dirY);

            Vector2D dir = new Vector2D(dirX, dirY);
            Vector2D head = new Vector2D(headX, headY);
            Vector2D tail = new Vector2D(0, 0);

            if (dir.ToAngle() == 0)
            {
                tail = new Vector2D(headX, headY + 120);
            }
            else if (dir.ToAngle() == 90)
            {
                tail = new Vector2D(headX - 120, headY);
            }
            else if (dir.ToAngle() == 180)
            {
                tail = new Vector2D(headX, headY - 120);
            }
            else if (dir.ToAngle() == -90)
            {
                tail = new Vector2D(headX + 120, headY);
            }
            List<Vector2D> body = new List<Vector2D>();
            body.Add(tail);
            body.Add(head);
            Snake snake = new Snake((int)state.ID, body, dir, p, 0, false, true, false, true);

            this.theWorld.Snakes[(int)state.ID] = snake;

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);

            // Broadcast the message to all clients
            // Lock here beccause we can't have new connections 
            // adding while looping through the clients list.
            // We also need to remove any disconnected clients.
            //HashSet<long> disconnectedClients = new HashSet<long>();
            //lock (clients)
            //{
            //    foreach (SocketState client in clients.Values)
            //    {
            //        //TODO: change sending info
            //        if (!Networking.Send(client.TheSocket!, "Message from client " + state.ID + ": " + p))
            //            disconnectedClients.Add(client.ID);
            //    }
            //}

            //foreach (long id in disconnectedClients)
            //    RemoveClient(id);

            state.OnNetworkAction = CommandRequest;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(state.ID + "\n" + this.size + "\n");

            foreach (Wall wall in this.gameSettings.Walls)
            {
                string jsonString = JsonSerializer.Serialize(wall);

                stringBuilder.Append(jsonString + "\n");
            }

            foreach(PowerUp powerUp in this.theWorld.PowerUps.Values)
            {
                string jsonString = JsonSerializer.Serialize(powerUp);

                stringBuilder.Append(jsonString + "\n");
            }

            Console.WriteLine(stringBuilder.ToString());
            Networking.Send(state.TheSocket, stringBuilder.ToString());
        }
    }


    private void CommandRequest(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        ProcessCommand(state);

        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }

    private void ProcessCommand(SocketState state)
    {

        string totalData = state.GetData();

        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until we have processed all messages.
        // We may have received more than one.
        foreach (string p in parts)
        {
            // Ignore empty strings added by the regex splitter
            if (p.Length == 0)
                continue;
            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;

            Snake snake = this.theWorld.Snakes[(int)state.ID];
            Vector2D head = snake.body[snake.body.Count - 1];

            if (p.Contains("up"))
            {
                //snake.dir.X = 0;
                //snake.dir.Y = -1;

                //snake.body.Add(new Vector2D(head.GetX(), head.GetY()));
                //movementRequest = "up";
                snake.ChangeDirection("up", this.snakeSpeed);
            }

            if (p.Contains("left"))
            {
                //snake.body.Add(new Vector2D());
                //snake.dir.X = -1;
                //snake.dir.Y = 0;
                //movementRequest = "left";
                snake.ChangeDirection("left", this.snakeSpeed);
            }

            if (p.Contains("down"))
            {
                //snake.body.Add(new Vector2D());
                //snake.dir.X = 0;
                //snake.dir.Y = 1;
                //movementRequest = "down";
                snake.ChangeDirection("down", this.snakeSpeed);
            }

            if (p.Contains("right"))
            {
                //snake.body.Add(new Vector2D());
                //snake.dir.X = 1;
                //snake.dir.Y = 0;
                //movementRequest = "right";
                snake.ChangeDirection("right", this.snakeSpeed);
            }

            if (p.Contains("none"))
            {
                movementRequest = "none";
            }

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);
        }
    }


    /// <summary>
    /// Removes a client from the clients dictionary
    /// </summary>
    /// <param name="id">The ID of the client</param>
    private void RemoveClient(long id)
    {
        Console.WriteLine("Client " + id + " disconnected");
        lock (clients)
        {
            lock (theWorld)
            {
                theWorld.Snakes.Remove((int)id);
            }
            clients.Remove(id);

        }
    }

    public void Run()
    {
        // Start a new timer to control the frame rate
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        while (true)
        {
            // wait until the next frame
            while (watch.ElapsedMilliseconds < msPerFrame)
            { /* empty loop body */}

            //Console.WriteLine(watch.ElapsedMilliseconds);
            watch.Restart();


            Update();

            ServerUpdate?.Invoke(theWorld.Snakes.Values, theWorld.PowerUps.Values);

        }
    }

    private void Update()
    {
        // cleanup the deactivated objects
        IEnumerable<int> playersToRemove = theWorld.Snakes.Values.Where(x => !x.alive).Select(x => x.snake);
        IEnumerable<int> powsToRemove = theWorld.PowerUps.Values.Where(x => x.died).Select(x => x.power);
        foreach (int i in playersToRemove)
        {
            //theWorld.Snakes.Remove(i);
        }
        foreach (int i in powsToRemove)
            theWorld.PowerUps.Remove(i);

        // add new objects back in
        int halfSize = size / 2;

        // TODO: This is an infinite loop which is why it only printed the frame rate once
        //while (theWorld.Snakes.Count < maxPlayers)
        //{
        //    // TODO: Figure out correct params to enter into the constructor
        //    //Snake p = new Snake(nextPlayerID++, -halfSize + rand.Next(size), -halfSize + rand.Next(size), rand.NextDouble() * 360);
        //    //theWorld.Snakes.Add(p.snake, p);
        //}

        //while (theWorld.PowerUps.Count < maxPowerups)
        //{
        //    PowerUp p = new PowerUp(nextPowID++, new Vector2D(-halfSize + rand.Next(size), -halfSize + rand.Next(size)), false);
        //    theWorld.PowerUps.Add(p.power, p);
        //}

        // move/update the existing objects in the world

        StringBuilder stringBuilder = new StringBuilder();

        lock (theWorld.Snakes.Values)
        {
            foreach (Snake p in theWorld.Snakes.Values)
            {

                //Console.WriteLine(p.body.Last().GetX() + " " + p.body.Last().GetY());
                bool isSnakeColliding = SnakeWallCollision(p) || this.SnakeCollisionSnake(p) || this.SnakeCollisionSelf(p);

                if (p.alive && isSnakeColliding)
                {
                    p.alive = false;
                    p.died = true;

                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    // wait until the next frame
                    while (watch.ElapsedMilliseconds < RespawnRate)
                    { /* empty loop body */}

                    watch.Stop();

                    Console.WriteLine("Snake Collision: " + p.snake + " " + p.body.Last().GetX() + " " + p.body.Last().GetY());

                    this.theWorld.Snakes[p.snake] = SnakeSpawn(p.snake, p.name);
                    stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
                    continue;
                }

                p.Step(this.snakeSpeed, movementRequest);

                bool isPowerupCollide = this.SnakePowerupCollision(p);
                if (isPowerupCollide)
                {
                    p.score++;
                    //p.RecievedPowerup();
                    Console.WriteLine("powerup");
                    Thread thread = new Thread(p.RecievedPowerup);
                    thread.Start();
                }

                movementRequest = "none";
                stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
            }
        }

        foreach (PowerUp p in theWorld.PowerUps.Values)
        {
            stringBuilder.Append(JsonSerializer.Serialize(p) + "\n");
        }


        foreach (SocketState state in this.clients.Values)
        {

            //Networking.Send(state.TheSocket, stringBuilder.ToString());
        }


        // Broadcast the message to all clients
        // Lock here beccause we can't have new connections 
        // adding while looping through the clients list.
        // We also need to remove any disconnected clients.
        HashSet<long> disconnectedClients = new HashSet<long>();
        lock (clients)
        {
            foreach (SocketState client in clients.Values)
            {
                //TODO: change sending info
                if (!Networking.Send(client.TheSocket!, stringBuilder.ToString()))
                    disconnectedClients.Add(client.ID);
            }
        }

        foreach (long id in disconnectedClients)
        {
            RemoveClient(id);
        }


    }

    private bool SnakeWallCollision(Snake snake)
    {
        foreach (Wall wall in this.theWorld.Walls.Values)
        {
            int snakeWidth = 10;
            int wallWidth = 25;
            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D head = snake.body.Last();
            double snakeHeadX = head.GetX();
            double snakeHeadY = head.GetY();

            if (p1.GetX() - wallWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + wallWidth + snakeWidth
                && p1.GetY() - wallWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + wallWidth + snakeWidth)
            {
                //Console.WriteLine("Collision detected");
                //Console.WriteLine(s + " Collision detected");
                return true;
            }
        }
        return false;
    }

    private bool SnakeBodyCollision(Snake snake, int bodyIndex)
    {
        foreach (Wall wall in this.theWorld.Walls.Values)
        {
            int snakeWidth = 10;
            int wallWidth = 25;
            string s = "";
            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D bodyPart = snake.body.ElementAt(bodyIndex);
            double snakeHeadX = bodyPart.GetX();
            double snakeHeadY = bodyPart.GetY();


            if (p1.GetX() - wallWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + wallWidth + snakeWidth
                && p1.GetY() - wallWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + wallWidth + snakeWidth)
            {
                //Console.WriteLine("Collision detected");
                //Console.WriteLine(s + " Collision detected");
                return true;
            }
        }

        return false;
    }

    private bool SnakeCollisionSelf(Snake snake)
    {
        int snakeWidth = 10;

        if(snake.body.Count < 5)
        {
            return false;
        }

        Vector2D head = snake.body.Last();

        for (int i = 0; i < snake.body.Count - 4; i++)
        {
            int j = i + 1;
            
            Vector2D p1 = snake.body[i];
            Vector2D p2 = snake.body[j];

            if ((p1.GetX() > p2.GetX()) || (p1.GetY() > p2.GetY()))
            {
                p1 = p2;
                p2 = p1;
            }

            double snakeHeadX = head.GetX();
            double snakeHeadY = head.GetY();


            if (p1.GetX()  - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX()  + snakeWidth
                && p1.GetY()  - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth)
            {
                //Console.WriteLine("Collision detected");
                //Console.WriteLine(s + " Collision detected");
                return true;
            }
            

        }

        return false;
    }

    private bool SnakeCollisionSnake(Snake snake)
    {
        foreach (Snake p in theWorld.Snakes.Values)
        {
            if(p.snake == snake.snake)
            {
                continue;
            }

            int snakeWidth = 10;

            string s = "";
            Vector2D p1;
            Vector2D p2;
            if (p.Equals(snake))
            {
                for (int i = 0; i < p.body.Count() - 2; i++)
                {
                    if ((p.body[i].GetX() < p.body[i + 1].GetX()) || (p.body[i].GetY() < p.body[i].GetY()))
                    {
                        p1 = p.body[i];
                        p2 = p.body[i + 1];
                    }
                    else
                    {
                        p1 = p.body[i + 1];
                        p2 = p.body[i];
                    }

                    Vector2D head = snake.body.Last();
                    double snakeHeadX = head.GetX();
                    double snakeHeadY = head.GetY();

                    if (p1.GetX() - snakeWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + snakeWidth + snakeWidth
                        && p1.GetY() - snakeWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth + snakeWidth)
                    {
                        Console.WriteLine("Collision detected");
                        Console.WriteLine(s + " Collision detected");
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < p.body.Count() - 1; i++)
                {
                    if ((p.body[i].GetX() < p.body[i + 1].GetX()) || (p.body[i].GetY() < p.body[i].GetY()))
                    {
                        p1 = p.body[i];
                        p2 = p.body[i + 1];
                    }
                    else
                    {
                        p1 = p.body[i + 1];
                        p2 = p.body[i];
                    }

                    Vector2D head = snake.body.Last();
                    double snakeHeadX = head.GetX();
                    double snakeHeadY = head.GetY();

                    if (p1.GetX() - snakeWidth - snakeWidth < snakeHeadX && snakeHeadX < p2.GetX() + snakeWidth + snakeWidth
                        && p1.GetY() - snakeWidth - snakeWidth < snakeHeadY && snakeHeadY < p2.GetY() + snakeWidth + snakeWidth)
                    {
                        Console.WriteLine("Collision detected");
                        Console.WriteLine(s + " Collision detected");
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private Snake SnakeSpawn(int ID, string name)
    {
        double headX = rand.Next(-size / 2, size / 2);
        double headY = rand.Next(-size / 2, size / 2);


        int dirX = rand.Next(-1, 2);
        int dirY;
        //Console.WriteLine(dirX);
        if (dirX == 0)
        {
            dirY = rand.Next(-1, 1) + 1;
        }
        else
        {
            dirY = 0;
        }

        //Console.WriteLine(dirY);

        Vector2D dir = new Vector2D(dirX, dirY);
        Vector2D head = new Vector2D(headX, headY);
        Vector2D tail = new Vector2D(0, 0);

        if (dir.ToAngle() == 0)
        {
            tail = new Vector2D(headX, headY + 120);
        }
        else if (dir.ToAngle() == 90)
        {
            tail = new Vector2D(headX - 120, headY);
        }
        else if (dir.ToAngle() == 180)
        {
            tail = new Vector2D(headX, headY - 120);
        }
        else if (dir.ToAngle() == -90)
        {
            tail = new Vector2D(headX + 120, headY);
        }
        List<Vector2D> body = new List<Vector2D>();
        body.Add(tail);
        body.Add(head);

        Snake snake = new Snake(ID, body, dir, name, 0, false, true, false, true);


        bool isSnakeCollidingWall = this.SnakeBodyCollision(snake, 0) ||
                                    this.SnakeBodyCollision(snake, 1) ||
                                    this.SnakeWallCollision(snake);

        while (!isSnakeCollidingWall)
        {
            headX = rand.Next(-size / 2, size / 2);
            headY = rand.Next(-size / 2, size / 2);


            dirX = rand.Next(-1, 2);

            //Console.WriteLine(dirX);
            if (dirX == 0)
            {
                dirY = rand.Next(-1, 1) + 1;
            }
            else
            {
                dirY = 0;
            }

            //Console.WriteLine(dirY);

            dir = new Vector2D(dirX, dirY);
            head = new Vector2D(headX, headY);
            tail = new Vector2D(0, 0);

            if (dir.ToAngle() == 0)
            {
                tail = new Vector2D(headX, headY + 120);
            }
            else if (dir.ToAngle() == 90)
            {
                tail = new Vector2D(headX - 120, headY);
            }
            else if (dir.ToAngle() == 180)
            {
                tail = new Vector2D(headX, headY - 120);
            }
            else if (dir.ToAngle() == -90)
            {
                tail = new Vector2D(headX + 120, headY);
            }
            body = new List<Vector2D>();
            body.Add(tail);
            body.Add(head);

            snake = new Snake(ID, body, dir, name, 0, false, true, false, true);

            
            isSnakeCollidingWall = this.SnakeBodyCollision(snake, 0)
                                || this.SnakeBodyCollision(snake, 1)
                                || this.SnakeWallCollision(snake);
        }

        return snake;
    }

    private Vector2D PowerupSpawn(int offset)
    {

        int powerupWidth = 10;
        //int snakeWidth = 10;
        int wallWidth = 25;


        foreach (Wall wall in this.theWorld.Walls.Values)
        {

            double locationX = rand.Next(-size / 2 + 100, size / 2 - 50);
            double locationY = rand.Next(-size / 2 + 100, size / 2 - 50);

            Vector2D p1;
            Vector2D p2;

            if ((wall.p1.GetX() < wall.p2.GetX()) || (wall.p1.GetY() < wall.p2.GetY()))
            {
                p1 = wall.p1;
                p2 = wall.p2;
            }
            else
            {
                p1 = wall.p2;
                p2 = wall.p1;
            }

            Vector2D location = new Vector2D(locationX, locationY);

            bool collision = false;

            while(!collision)
            {
                if (p1.GetX() - wallWidth - powerupWidth - offset < locationX && locationX < p2.GetX() + wallWidth + powerupWidth + offset
                && p1.GetY() - wallWidth - powerupWidth - offset < locationY && locationY < p2.GetY() + wallWidth + powerupWidth + offset)
                {
                    collision = true;
                }

                if (locationX > size || locationX < -size ||
                locationY > size || locationY < -size)
                {
                    collision = true;
                }

                locationX = rand.Next(-size / 2, size / 2);
                locationY = rand.Next(-size / 2, size / 2);
            }
            return location;

            
        }
        return new Vector2D();
    }

    private bool SnakePowerupCollision(Snake snake)
    {
        int snakeWidth = 10;
        int powerupWidth = 10;
        foreach(PowerUp powerUp in this.theWorld.PowerUps.Values)
        {
            if((powerUp.loc - snake.body.Last()).Length() < snakeWidth + powerupWidth)
            {
                powerUp.died = true;
                Console.WriteLine("Powerup Collision");
                return true;
            }
        }
        return false;
    }
}
