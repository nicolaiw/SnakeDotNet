﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private TaskCompletionSource _pauseCompletionSource;

        public MainWindow()
        {
            InitializeComponent();

            KeyDown += MainWindow_KeyDown;
            MouseDown += MainWindow_MouseDown;

            _pauseCompletionSource = new TaskCompletionSource();

            _workerTask = RunAsync();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _pause = true;
                DragMove();
            }

            _pause = false;
            _pauseCompletionSource.SetResult();
            _pauseCompletionSource = new TaskCompletionSource();
        }

        private Task RunAsync()
        {
            try
            {
                _points = 0;
                pointsLabel.Content = _points;

                var initialSnake = new List<Rectangle>();

                var startX = 400;
                var startY = 220;
                var snakeLength = 3; // Snake length without head

                for (int i = snakeLength; i > 0; i--)
                {
                    var x = startX - (i * 10);
                    var link = GenerateNewLink(x, startY, (SolidColorBrush)new BrushConverter().ConvertFrom("#293253"));
                    initialSnake.Add(link);
                }

                initialSnake.Add(GenerateNewLink(startX, startY, (SolidColorBrush)new BrushConverter().ConvertFrom("#6dd47e")));

                Snake = new Snake(setX: (rec, x) => Dispatcher.Invoke(() => Canvas.SetLeft(rec, x)),
                                  setY: (rec, y) => Dispatcher.Invoke(() => Canvas.SetTop(rec, y)),
                                  getX: rec => Dispatcher.Invoke(() => (int)Canvas.GetLeft(rec)),
                                  getY: rec => Dispatcher.Invoke(() => (int)Canvas.GetTop(rec)),
                                  addLink: rec => Dispatcher.Invoke(() => canvas.Children.Add(rec)),
                                  xMax: (int)canvas.Width,
                                  yMax: (int)canvas.Height,
                                  links: initialSnake);

                return Dispatcher.Invoke(async () =>
                {
                    foreach (var link in Snake.Links)
                    {
                        canvas.Children.Add(link);
                    }

                    var currentSnack = SpawnSnack();

                    while (_run)
                    {
                        await Task.Delay(millisecondsDelay: 30);

                        if (_pause)
                        {
                            await _pauseCompletionSource.Task;
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
                                currentSnack = SpawnSnack();
                            });

                            Snake.Extend();
                        }

                        if (Snake.CheckCollision())
                        {
                            _run = false;
                            break;
                        }

                        //Snake.Links.ForEach(link => System.Diagnostics.Debug.WriteLine(GetPosition(link).X + " "));
                    }
                });
            }
            catch /*(Exception ex)*/
            {
                // TODO: Handle exceptions in a better way
                return Task.CompletedTask;
            }
        }

        private Rectangle SpawnSnack()
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
                x = rnd.Next((int)canvas.Width - 10);
                y = rnd.Next((int)canvas.Height - 10);

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
                        _pauseCompletionSource.SetResult();
                        _pauseCompletionSource = new TaskCompletionSource();
                        menuWindow.Close();
                    });

                menuCtrl.Owner = this;
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
            else if (e.Key is Key.Enter)
            {
                if (_run)
                    return;

                _run = false;
                canvas.Children.Clear();
                canvas.Children.Add(pointsLabel);
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
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
        private int _currentTailIndex = 0;

        public static readonly Point RIGHT_DIRECTION = new(x: 10, y: 0);
        public static readonly Point LEFT_DIRECTION = new(x: -10, y: 0);
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

                if (_allowDirectionChange == false)
                    return;

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

                _allowDirectionChange = false;
                _currentDirection = value;
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
            // TODO: Consinder saving the Positions instead of
            //       reading it every time with getX() and getY()
            if (_extend)
            {
                var head = Links.Last();

                var currentHeadX = getX(head);
                var currentHeadY = getY(head);

                var rect = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#293253"),
                };

                setX(rect, currentHeadX);
                setY(rect, currentHeadY);

                addLink(rect);

                var newHeadX = currentHeadX + CurrentDirection.X;
                var newHeadY = currentHeadY + CurrentDirection.Y;

                (newHeadX, newHeadY) = BoundCheckXY(newHeadX, newHeadY);

                setX(head, newHeadX);
                setY(head, newHeadY);

                /*
                    We draw the new Link to the current head.
                    To ensure that this inserted link at the very front will not for example will be used
                    for the next tail (_currentTailIndex) we will insert it to the current index and than
                    adjust the _currentTailIdex.

                    HINT: The links are not sorted like they are rendered as we do not move
                          The whole snake while moving. See comment "Movement" below.

                    - [0][1][2][3][head] // initial Snake
                    - [0][1][2][3][new link [4]][Head] // Imagine 3 is the current tail index
                    - At the next move we would update the previously added link
                      but that would not be right. Therefore we do not insert the new link at the end of the
                      Link list but at the _currentTailIndex and then adjust the _currentTailIndex.
                      So the new link is rendered at the very front but will become the tail at last. 
                 */
                Links.Insert(_currentTailIndex, rect);

                _currentTailIndex = ++_currentTailIndex % (Links.Count - 1);

                _extend = false;
            }
            else
            {
                // Movement: Just replace the tail position with the currents
                //           head position and save the next link index as the
                //           new index of the tail.
                var tail = Links[_currentTailIndex];
                var head = Links.Last();

                var currentHeadX = getX(head);
                var currentHeadY = getY(head);

                setX(tail, currentHeadX);
                setY(tail, currentHeadY);

                var newHeadX = currentHeadX + CurrentDirection.X;
                var newHeadY = currentHeadY + CurrentDirection.Y;

                (newHeadX, newHeadY) = BoundCheckXY(newHeadX, newHeadY);

                setX(head, newHeadX);
                setY(head, newHeadY);

                _currentTailIndex = ++_currentTailIndex % (Links.Count - 1);
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
