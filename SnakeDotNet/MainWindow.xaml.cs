using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SnakeDotNet
{
    public partial class MainWindow : Window
    {
        private Snake Snake { get; set; }
        private bool _run = true;
        private bool _pause = false;
        private int _points;
        private Task _workerTask;
        private TaskCompletionSource _pausCompletionSource;

        public MainWindow()
        {
            InitializeComponent();

            KeyDown += MainWindow_KeyDown;
            
            _pausCompletionSource = new TaskCompletionSource();

            _workerTask = RunAsync();
        }

        private Task RunAsync()
        {
            try
            {
                _points = 0;
                pointsLabel.Content = _points;

                var initialSnake = new List<Rectangle>()
                {
                    GenerateNewLink(380, 220, System.Windows.Media.Brushes.Blue),
                    GenerateNewLink(390, 220, System.Windows.Media.Brushes.Blue),
                    GenerateNewLink(400,220, System.Windows.Media.Brushes.Green),
                };

                Snake = new Snake(setX: (rec, x) => Dispatcher.Invoke(() => Canvas.SetLeft(rec, x)),
                                  setY: (rec, y) => Dispatcher.Invoke(() => Canvas.SetTop(rec, y)),
                                  getX: rec => Dispatcher.Invoke(() => (int)Canvas.GetLeft(rec)),
                                  getY: rec => Dispatcher.Invoke(() => (int)Canvas.GetTop(rec)),
                                  addLink: rec => Dispatcher.Invoke(() => canvas.Children.Add(rec)),
                                  xMax: (int)Width,
                                  yMax: (int)Height,
                                  links: initialSnake);

                return Dispatcher.Invoke(async () =>
                {
                    foreach (var link in Snake.Links)
                    {
                        canvas.Children.Add(link);
                    }

                    var currentSnack = SpawSnack();

                    while (_run)
                    {
                        await Task.Delay(millisecondsDelay: 30);

                        if (_pause)
                        {
                            await _pausCompletionSource.Task;
                        }

                        Snake.MoveForwad();

                        var newHeadPosition = GetPosition(Snake.Links.Last());
                        var snackPosition = GetPosition(currentSnack);

                        if (newHeadPosition == snackPosition)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _points++;
                                pointsLabel.Content = _points;
                                canvas.Children.Remove(currentSnack);
                                currentSnack = SpawSnack();
                            });

                            Snake.Extend();
                        }

                        if (Snake.CheckCollision())
                        {
                            _run = false;
                            break;
                        }
                    }
                });
            }
            catch /*(Exception ex)*/
            {
                // TODO: Handle exceptions in a better way
                return Task.CompletedTask;
            }
        }

        private Rectangle SpawSnack()
        {
            var snack = new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = System.Windows.Media.Brushes.Red,
            };

            var rnd = new Random();

            int x, y;

            do
            {
                x = rnd.Next((int)this.Width - 10);
                y = rnd.Next((int)this.Height - 10);

                var tempX = x / 10.0;
                var tempY = y / 10.0;

                // Example : 189 --> 18.9 --> 190
                //
                // this makes sure the snak x an y Position is divisible 10 so
                // that we can easly check vor colision of the snake with the snack
                // by comparint just be x and y value of the head of the snake
                // with the snacks x and y value
                x = (int)(Math.Round(tempX) * 10); 
                y = (int)(Math.Round(tempY) * 10);

            } while (Snake.Links.Any(rect =>
              {
                  var pos = GetPosition(rect);

                  return pos.X == x && pos.Y == y;
              }));

            Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(snack, x);
                Canvas.SetTop(snack, y);
            });

            canvas.Children.Add(snack);

            return snack;
        }

        private Point GetPosition(Rectangle rectangle)
        {
            return Dispatcher.Invoke(() =>
            {
                var x = (int)Canvas.GetLeft(rectangle);
                var y = (int)Canvas.GetTop(rectangle);

                return new Point(x, y);
            });
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _pause = true;
                var menuCtrl = new MenuWindow(
                    menuWindow =>
                    {
                        // TODO: Shut down properly --> TaskCancelation (?)
                        Application.Current.Shutdown();
                    },
                    menuWindow =>
                    {
                        _pause = false;
                        _pausCompletionSource.SetResult();
                        _pausCompletionSource = new TaskCompletionSource();
                        menuWindow.Close();
                    });

                menuCtrl.ShowDialog();
            }
            else if (e.Key is Key.Up or Key.W)
            {
                Snake.CurrentDirection = Snake.UP_DIRECTION;
            }
            else if (e.Key is Key.Down or Key.S)
            {
                Snake.CurrentDirection = Snake.DOWN_DIRECTION;
            }
            else if (e.Key is Key.Left or Key.A)
            {
                Snake.CurrentDirection = Snake.LEFT_DIRECTION;
            }
            else if (e.Key is Key.Right or Key.D)
            {
                Snake.CurrentDirection = Snake.RIGHT_DIRECTION;
            }
            else if (e.Key == Key.Enter)
            {
                if (_run)
                    return;

                _run = false;
                canvas.Children.Clear();
                _points = 0;
                pointsLabel.Content = _points;
                await _workerTask;
                _run = true;
                _workerTask = RunAsync();
            }
        }

        private Rectangle GenerateNewLink(int x, int y, System.Windows.Media.Brush color)
        {
            var rect = new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = color,
            };

            Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
            });

            return rect;
        }
    }

    public class Point : IEquatable<Point>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public bool Equals(Point other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Point);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Point obj1, Point obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (obj1 is null)
            {
                return false;
            }

            if (obj2 is null)
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(Point obj1, Point obj2)
        {
            return !(obj1 == obj2);
        }
    }

    public class Snake
    {
        private readonly Action<Rectangle, int> setX;
        private readonly Action<Rectangle, int> setY;
        private readonly Func<Rectangle, int> getX;
        private readonly Func<Rectangle, int> getY;
        private readonly int xMax;
        private readonly int yMax;
        private readonly Action<Rectangle> addLink;
        private bool _extend = false;
        private bool _allowDirectionChange = true;

        public static readonly Point RIGHT_DIRECTION = new(x: 10, y: 0);
        public static readonly Point LEFT_DIRECTION = new(x: -10, y:0);
        public static readonly Point UP_DIRECTION = new(x: 0, y: -10);
        public static readonly Point DOWN_DIRECTION = new(x: 0, y: 10);

        private Point _currentDirection;
        public Point CurrentDirection 
        {
            get => _currentDirection;
            set
            {
                if (_currentDirection == null)
                {
                    _currentDirection = value;
                    return;
                }

                if (_currentDirection == value)
                    return;

                /* Don't allow the opposit direction */
                if (_currentDirection == RIGHT_DIRECTION && value == LEFT_DIRECTION)
                    return;

                if (_currentDirection == LEFT_DIRECTION && value == RIGHT_DIRECTION)
                    return;

                if (_currentDirection == UP_DIRECTION && value == DOWN_DIRECTION)
                    return;

                if (_currentDirection == DOWN_DIRECTION && value == UP_DIRECTION)
                    return;

                if (_allowDirectionChange)
                {
                    _allowDirectionChange = false;
                    _currentDirection = value;
                }
            }
        }

        public List<Rectangle> Links { get; set; }

        public Snake(Action<Rectangle, int> setX,
                     Action<Rectangle, int> setY,
                     Func<Rectangle, int> getX,
                     Func<Rectangle, int> getY,
                     Action<Rectangle> addLink,
                     int xMax,
                     int yMax,
                     List<Rectangle> links)
        {
            this.setX = setX;
            this.setY = setY;
            this.getX = getX;
            this.getY = getY;
            this.xMax = xMax;
            this.yMax = yMax;
            this.addLink = addLink;
            Links = links;

            CurrentDirection = RIGHT_DIRECTION;
        }

        private (int, int) BoundCheckXY(int currentX, int currentY)
        {
            int x = currentX, y = currentY;

            if (currentX < 0)
            {
                x = xMax;
            }
            else if (currentX >= xMax)
            {
                x = 0;
            }

            if (currentY < 0)
            {
                y = yMax;
            }
            else if (currentY >= yMax)
            {
                y = 0;
            }

            return (x, y);
        }

        public List<Rectangle> MoveForwad()
        {
            for (int i = 0; i < Links.Count - 1; i++)
            {
                var tail = Links[i];
                var tailNext = Links[i + 1];
                
                // TODO: Consider creating a model wich stores
                // the Rectangle and the position so we don't have
                // to read it here
                var tailX = getX(tailNext);
                var tailY = getY(tailNext);

                (tailX, tailY) = BoundCheckXY(tailX, tailY);

                setX(tail, tailX);
                setY(tail, tailY);
            }

            var head = Links.Last();
            var newHeadX = getX(head) + CurrentDirection.X;
            var newHeadY = getY(head) + CurrentDirection.Y;

            (newHeadX, newHeadY) = BoundCheckXY(newHeadX, newHeadY);

            setX(head, newHeadX);
            setY(head, newHeadY);

            if (_extend)
            {
                var currentTail = Links.First();
                var currentTailX = getX(currentTail);
                var currentTailY = getY(currentTail);

                var rect = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Fill = System.Windows.Media.Brushes.Blue,
                };

                setX(rect, currentTailX);
                setY(rect, currentTailY);

                addLink(rect);

                Links.Insert(0, rect);

                _extend = false;
            }

            /*
                Just allow one direction change for each movement.
                
                E.g. imagine the Delay in  the render task would be 2s.
                It would be possible to chachge the direction within this
                2s delay from e.g. left -> up -> right. Now when the render function
                runs and the snake would colide with itself so the checks for the
                opposite position in the getter of the CurrentDirection property
                would not work as expectet and would not handle the opposite
                direction restriction.
             */
            _allowDirectionChange = true;

            return Links;
        }

        public void Extend()
        {
            _extend = true;
        }

        public bool CheckCollision()
        {
            return Links.Any(link =>
            {
                var linkX = getX(link);
                var linkY = getY(link);

                var head = Links.Last();
                var headX = getX(head);
                var headY = getY(head);

                if (head == link)
                    return false;

                return headX == linkX && headY == linkY;
            });
        }
    }
}
