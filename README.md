Project implemented by Kevin Soto-Miranda and Markus Buckwalter
Date: December 7, 2023

IMPORTANT: To properly use and test our server the settings.xml file directory must be in the directory for the project
like /.../game-codemurray_game/SnakeGame/Server/bin/Debug/net7.0/settings.xml
This is to ensure the settings can be read and deserialized to setup the world. To do this you can copy
and paste the settings.xml file given into the proper folder. 

Our Server is setup to favor a smaller user. The server might not be able to handle over a certain amount of users.
The smaller amount of players that utilize our server the better. This performance is caused by the use of locks to help
manage race conditions when running different clients on seperate threads. The overall performance of our game
works as expected. There are no extra features as we were limited on time and wanted to ensure our basic funtionality was
working. The server controller is where the main functionality of our server can be found. Using model obejcts while working
with the server controller we were able to get a working server side of the game.
