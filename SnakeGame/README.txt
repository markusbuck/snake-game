Project implemented by Kevin Soto-Miranda and Markus Buckwalter
Date: November 26, 2023

Our SnakeGame solution is set up by having a Model Project that contains a file for a PowerUp, a Snake, a Wall, and a World.
Each of the objects in the game work by using Vector2D objects which is contained in another project. 
The Game itself waits for user input in the view which is contained in the SnakeClient project, once user input is taken in,
The logic that controlls the game is in the GameController project. The GameController works with the NetworkingController,
as well as the various objects in the Model project to make the game function.

We started our project by first trying to initiate the handshake.
We got the protcol to work as expected and currently do not experience any bugs.
Upon completion of working out all the kinks in that section of the game, we then moved onto the the sending of the user input
to change the direction. When testing that we decided to move onto the drawing of the world, snakes, and powerups. There were
a lot of bugs throughout the whole process but we were able to work through them and get our game to a point that runs smoothly.
Our design decisions were made with the idea in mind that we would not be playing with a big amount of players.
