using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Model;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;



namespace SnakeGame;
public class WorldPanel : ScrollView,IDrawable
{

    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private GraphicsView graphicsView = new();
    private World theWorld;
    private IImage wall;
    private IImage background;
    private int viewSize = 900;
    private float playerX;
    private float playerY;


    private bool initializedForDrawing = false;

    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    public WorldPanel()
    {
        BackgroundColor = Colors.Black; 
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = 2000;
        graphicsView.WidthRequest = 2000;
        graphicsView.BackgroundColor = Colors.Black;
        this.Content = graphicsView;
    }

    public void SetWorld(World w)
    {
        theWorld = w;
    }


    private void InitializeDrawing()
    {
        wall = loadImage( "wallsprite.png" );
        background = loadImage( "background.png" );
        initializedForDrawing = true;
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        PowerUp p = o as PowerUp;
        int width = 16;

        canvas.FillColor = Colors.Red;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);

    }

    private void SnakeSegmentDrawer(object o, ICanvas canvas)
    {
        int segmentLength = Convert.ToInt32(o);
        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 10;
        //canvas.DrawLine(0, 0, 0, -segmentLength);
        canvas.DrawLine(0, 0, 0, -segmentLength);

        //canvas.FontColor = Colors.White;
        //canvas.FontSize = 18;
        //canvas.Font = Font.Default;
        //canvas.DrawString("Kevin:", 0, -segmentLength, 100,100,  HorizontalAlignment.Left, VerticalAlignment.Top);
    }

    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall wall = o as Wall;
        
        int width = 50;

        double xLength = Math.Abs(wall.p1.GetX() - wall.p2.GetX());
        double yLength = Math.Abs(wall.p1.GetY() - wall.p2.GetY());

        if (xLength > yLength)
        {
            int wallSegments = (int)xLength / 50;
            Vector2D leftPoint = (wall.p1.GetX() < wall.p2.GetX()) ? wall.p1 : wall.p2;
            for(int segment = 0; segment < wallSegments + 1; segment++)
            {
                float x = (float)leftPoint.GetX() - (width / 2) + segment * width;
                float y = (float)leftPoint.GetY() - (width / 2);

                canvas.DrawImage(this.wall, x, y, width, width);
                //Console.WriteLine(x + " " + y);
            }
        }

        else
        {
            int wallSegments = (int)yLength / 50;
            Vector2D topPoint = (wall.p1.GetY() > wall.p2.GetY()) ? wall.p1 : wall.p2;
            for (int segment = 0; segment < wallSegments; segment++)
            {
                float x = (float)topPoint.GetX() - (width / 2);
                float y = (float)topPoint.GetY() - (width / 2) - (segment * width);
                canvas.DrawImage(this.wall, x, y, width, width);
                //Console.WriteLine(x + " " + y);
            }
        }
        //canvas.DrawImage(this.wall, (float) -wall.p1.GetX() / 2, (float) -wall.p1.GetY() / 2, width, 50);
    }


    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if ( !initializedForDrawing )
            InitializeDrawing();

        // undo previous transformations from last frame
        canvas.ResetState();

        // example code for how to draw
        // (the image is not visible in the starter code)
        //canvas.DrawImage(wall, 0, 0, wall.Width, wall.Height);

        lock(theWorld)
        {
            if (theWorld != null)
            {
                if (theWorld.Snakes.ContainsKey(theWorld.CurrentSnake) && theWorld.Snakes[theWorld.CurrentSnake].alive)
                {
                    this.playerX = (float)theWorld.Snakes[theWorld.CurrentSnake].body[0].GetX();
                    this.playerY = (float)theWorld.Snakes[theWorld.CurrentSnake].body[0].GetY();
                }
                canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));

                canvas.DrawImage(background, (-theWorld.Size / 2), (-theWorld.Size / 2), theWorld.Size,
                  theWorld.Size);

                foreach (var powerup in theWorld.PowerUps.Values)
                {
                    DrawObjectWithTransform(canvas, powerup, powerup.loc.X, powerup.loc.Y, 0, PowerupDrawer);
                }

                foreach (var wall in theWorld.Walls.Values)
                {
                    DrawObjectWithTransform(canvas, wall, 0, 0, 0, WallDrawer);
                }

                foreach (var snake in theWorld.Snakes.Values)
                {

                    for (int i = 0; i < snake.body.Count -1; i++)
                    {
                        Vector2D p1 = snake.body[i + 1]; // tail
                        Vector2D p2 = snake.body[i];     // tip
                        Vector2D direction = snake.dir;
                        double segmentLengthX = Math.Abs(p2.GetX() - p1.GetX());
                        double segmentLengthY = Math.Abs(p2.GetY() - p1.GetY());

                        if (segmentLengthX > segmentLengthY)
                        {
                            double rotation = (direction.GetX() > 0) ? -90 : 90;
                            DrawObjectWithTransform(canvas, segmentLengthX, p1.GetX(), p1.GetY(), rotation, SnakeSegmentDrawer);
                        }
                        else
                        {
                            double rotation = (direction.GetY() > 0) ? 0 : -180;
                            DrawObjectWithTransform(canvas, segmentLengthY, p1.GetX(), p1.GetY(), rotation, SnakeSegmentDrawer);

                        }
                    }

                    //double segmentLength = 0;
                    //double segmentDirection = 0;

                    //Vector2D head = snake.body[1];
                    //Vector2D tail = snake.body[0];

                    //if (snake.dir.GetX() != 0.0)
                    //{
                    //    segmentLength = Math.Abs(head.GetX() - tail.GetX());

                    //    if(snake.dir.GetX() > 0)
                    //    {
                    //        segmentDirection = -90;
                    //    }
                    //    else
                    //    {
                    //        segmentDirection = 90;
                    //    }
                    //}
                    //else
                    //{
                    //    segmentLength = Math.Abs(head.GetY() - tail.GetY());

                    //    if (snake.dir.GetY() > 0)
                    //    {
                    //        segmentDirection = 0;
                    //    }
                    //    else
                    //    {
                    //        segmentDirection = -180;
                    //    }

                    //}


                    //foreach (var bodyPart in snake.body)
                    //{
                    //    Vector2D vector = new Vector2D(bodyPart.GetX(), bodyPart.GetY());
                    //    vector.Normalize();
                    //    segmentLength += bodyPart.Length();
                    //    segmentDirection += vector.ToAngle();
                    //}

                    //float angle = Vector2D.AngleBetweenPoints(snake.body[0], snake.body[1]);
                    double segmentX = snake.body[1].GetX();
                    double segmentY = snake.body[1].GetY();
                    //Console.WriteLine( "WorldPanel: " + segmentX + " " + segmentY);

                    //DrawObjectWithTransform(canvas, segmentLength, segmentX, segmentY, segmentDirection, SnakeSegmentDrawer);
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 18;
                    canvas.Font = Font.Default;
                    canvas.DrawString(snake.name + ": " + snake.score, (int)segmentX, (int)segmentY, 100, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
                }
            }
        } 
    }
}
