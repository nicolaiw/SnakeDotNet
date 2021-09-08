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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Snake Snake { get; set; }
        private bool _run = true;
        private bool _pause = false;
        private Task _workerTask;
        private TaskCompletionSource _pausCompletenSource;

        public MainWindow()
        {
            InitializeComponent();

            KeyDown += MainWindow_KeyDown;
            _pausCompletenSource = new TaskCompletionSource();

            _workerTask = RunAsync();
        }

        private Task RunAsync()
        {
            try
            {
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
                        this.canvas.Children.Add(link);
                    }

                    var currentSnack = SpawSnack();

                    while (_run)
                    {
                        await Task.Delay(millisecondsDelay: 30);

                        if (_pause)
                        {
                            await _pausCompletenSource.Task;
                        }

                        Snake.MoveForwad();

                        var newHeadPosition = GetPosition(Snake.Links.Last());
                        var snackPosition = GetPosition(currentSnack);

                        if (newHeadPosition.X == snackPosition.X && newHeadPosition.Y == snackPosition.Y)
                        {
                            Dispatcher.Invoke(() =>
                            {
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

                x = (int)(Math.Round(tempX) * 10); // 189 --> 18.9;
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
                        // TODO: sauber beenden --> TaskCancelation
                        Application.Current.Shutdown();
                    },
                    menuWindow =>
                    {
                        _pause = false;
                        _pausCompletenSource.SetResult();
                        _pausCompletenSource = new TaskCompletionSource();
                        menuWindow.Close();
                    });

                menuCtrl.ShowDialog();
            }
            else if (e.Key == Key.Up)
            {
                Snake.CurrentDirection = new Point(0, -10);
            }
            else if (e.Key == Key.Down)
            {
                Snake.CurrentDirection = new Point(0, 10);
            }
            else if (e.Key == Key.Left)
            {
                Snake.CurrentDirection = new Point(-10, 0);
            }
            else if (e.Key == Key.Right)
            {
                Snake.CurrentDirection = new Point(10, 0);
            }
            else if (e.Key == Key.Enter)
            {
                if (_run)
                    return;

                _run = false;
                canvas.Children.Clear();
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

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
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

        public Point CurrentDirection { get; set; }

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

            CurrentDirection = new Point(x: 10, y: 0); // right
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
