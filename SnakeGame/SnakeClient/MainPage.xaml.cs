using GameController;
namespace SnakeGame;

public partial class MainPage : ContentPage
{
    GameController.GameController gameController;
    public MainPage()
    {
        InitializeComponent();
        gameController = new GameController.GameController();
        gameController.Connected += HandleConnected;
        gameController.Error += ShowError;
        gameController.Updated += OnFrame;
        worldPanel.SetWorld(gameController.world);
    }

    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            // Move up
            gameController.SendCommand("up");
        }
        else if (text == "a")
        {
            // Move left
            gameController.SendCommand("left");
        }
        else if (text == "s")
        {
            // Move down
            gameController.SendCommand("down");
        }
        else if (text == "d")
        {
            // Move right
            gameController.SendCommand("right");
        }
        entry.Text = "";
    }

    private void HandleConnected()
    {
        gameController.StartSend(nameText.Text);
    }

    private void ShowError(string err)
    {
        // Show the error
        Dispatcher.Dispatch(() => DisplayAlert("Error", err, "OK"));

        // Then re-enable the controlls so the user can reconnect
        Dispatcher.Dispatch(
          () =>
          {
              connectButton.IsEnabled = true;
              serverText.IsEnabled = true;
          });
    }

    private void NetworkErrorHandler()
    {
        DisplayAlert("Error", "Disconnected from server", "OK");
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        connectButton.IsEnabled = false;
        serverText.IsEnabled = false;

        gameController.JoinServer(serverText.Text);
        keyboardHack.Focus();
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        worldPanel.SetWorld(gameController.world);
        //gameController.world.UpdateCameFromServer(gameController.world.Snakes.Values, gameController.world.PowerUps.Values);
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}