using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace The_second_semestr_work
{
    public partial class Form1 : Form
    {
        private const int GridSize = 25;
        private const int CellSize = 20;
        private const int WidthCells = GridSize;
        private const int HeightCells = GridSize;
        private Timer timer;
        private GameManager gameManager;
        public Form1()
        {
            InitializeComponent();
            this.ClientSize = new Size(WidthCells * CellSize, HeightCells * CellSize);
            this.Text = "Snake Game";
            gameManager = new GameManager(WidthCells, HeightCells, CellSize);
            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;
            timer = new Timer();
            timer.Interval = 150;
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            gameManager.Update();
            this.Invalidate();
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            gameManager.Draw(e.Graphics);
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                gameManager.Restart();
                return;
            }
            gameManager.HandleInput(e.KeyCode);
        }
    }
    interface IDrawable
    {
        void Draw(Graphics g);
    }
    interface IUpdatable
    {
        void Update();
    }
    abstract class GameObject : IDrawable, IUpdatable
    {
        public Point Position { get; set; }
        public abstract void Draw(Graphics g);
        public abstract void Update();
    }
    enum Direction { Up, Down, Left, Right }
    class Snake : GameObject
    {
        public List<Point> Body { get; private set; } = new List<Point>();
        public Direction CurrentDirection { get; set; } = Direction.Right;
        private int growPending = 0;
        private int maxWidth, maxHeight;
        public Snake(int startX, int startY, int width, int height)
        {
            Body.Add(new Point(startX, startY));
            maxWidth = width;
            maxHeight = height;
        }
        public void Grow(int n = 1)
        {
            growPending += n;
        }
        public Point Head => Body[0];

        public bool CheckCollision(Point p)
        {
            return Body.Contains(p);
        }
        public override void Update()
        {
            Point newHead = Head;
            switch (CurrentDirection)
            {
                case Direction.Up: newHead.Offset(0, -1); break;
                case Direction.Down: newHead.Offset(0, 1); break;
                case Direction.Left: newHead.Offset(-1, 0); break;
                case Direction.Right: newHead.Offset(1, 0); break;
            }
            if (newHead.X < 0) 
                newHead.X = maxWidth - 1;
            if (newHead.X >= maxWidth) 
                newHead.X = 0;
            if (newHead.Y < 0) 
                newHead.Y = maxHeight - 1;
            if (newHead.Y >= maxHeight) 
                newHead.Y = 0;
            Body.Insert(0, newHead);
            if (growPending > 0)
                growPending--;
            else
                Body.RemoveAt(Body.Count - 1);
        }
        public override void Draw(Graphics g)
        {
            foreach (var p in Body)
            {
                g.FillRectangle(Brushes.Green, p.X * 20, p.Y * 20, 20, 20);
                g.DrawRectangle(Pens.Black, p.X * 20, p.Y * 20, 20, 20);
            }
        }
    }
    class Food : GameObject
    {
        private int cellSize;
        public Food(int cellSize)
        {
            this.cellSize = cellSize;
        }
        public override void Update() { }
        public override void Draw(Graphics g)
        {
            g.FillEllipse(Brushes.Red, Position.X * cellSize, Position.Y * cellSize, cellSize, cellSize);
        }
    }
    class Obstacle : GameObject
    {
        private int cellSize;
        public Obstacle(Point position, int cellSize)
        {
            Position = position;
            this.cellSize = cellSize;
        }
        public override void Update() { }
        public override void Draw(Graphics g)
        {
            g.FillRectangle(Brushes.Gray, Position.X * cellSize, Position.Y * cellSize, cellSize, cellSize);
        }
    }
    class GameManager
    {
        private int widthCells, heightCells, cellSize;
        private Snake snake;
        private Food food;
        private List<Obstacle> obstacles = new List<Obstacle>();
        private Random rnd = new Random();
        private bool gameOver = false;
        private bool gameWin = false;
        public GameManager(int widthCells, int heightCells, int cellSize)
        {
            this.widthCells = widthCells;
            this.heightCells = heightCells;
            this.cellSize = cellSize;
            snake = new Snake(widthCells / 2, heightCells / 2, widthCells, heightCells);
            GenerateFood();
            GenerateObstacles(25);
        }
        private void GenerateObstacles(int count)
        {
            obstacles.Clear();
            for (int i = 0; i < count; i++)
            {
                Point p;
                do
                {
                    p = new Point(rnd.Next(widthCells), rnd.Next(heightCells));
                } 
                while (snake.CheckCollision(p) || obstacles.Any(o => o.Position == p));
                obstacles.Add(new Obstacle(p, cellSize));
            }
        }
        private void GenerateFood()
        {
            Point p;
            do
            {
                p = new Point(rnd.Next(widthCells), rnd.Next(heightCells));
            } 
            while (snake.CheckCollision(p) || obstacles.Any(o => o.Position == p));
            food = new Food(cellSize) { Position = p };
        }

        public void Update()
        {
            if (gameOver || gameWin) 
                return;
            snake.Update();
            if (snake.Body.Skip(1).Any(p => p == snake.Head) || obstacles.Any(o => o.Position == snake.Head))
            {
                gameOver = true;
                return;
            }
            if (snake.Head == food.Position)
            {
                snake.Grow();
                GenerateFood();
            }
            int totalCells = widthCells * heightCells;
            if (snake.Body.Count + obstacles.Count >= totalCells)
                gameWin = true;
        }
        public void Draw(Graphics g)
        {
            g.Clear(Color.Black);
            foreach (var obs in obstacles) 
                obs.Draw(g);
            food.Draw(g);
            snake.Draw(g);
            if (gameOver)
            {
                string msg = "GAME OVER";
                DrawCenteredString(g, msg, new Font("Arial", 32, FontStyle.Bold), Brushes.Red);
                DrawSubText(g, "Нажмите R для перезапуска", new Font("Arial", 16, FontStyle.Bold), Brushes.White);
            }
            else if (gameWin)
            {
                string msg = "YOU WIN!";
                DrawCenteredString(g, msg, new Font("Arial", 32, FontStyle.Bold), Brushes.Yellow);
                DrawSubText(g, "Нажмите R для перезапуска", new Font("Arial", 16, FontStyle.Bold), Brushes.White);
            }
        }
        private void DrawCenteredString(Graphics g, string text, Font font, Brush brush)
        {
            SizeF textSize = g.MeasureString(text, font);
            g.DrawString(text, font, brush,
                (widthCells * cellSize - textSize.Width) / 2,
                (heightCells * cellSize - textSize.Height) / 2);
        }
        private void DrawSubText(Graphics g, string text, Font font, Brush brush)
        {
            SizeF textSize = g.MeasureString(text, font);
            g.DrawString(text, font, brush,
                (widthCells * cellSize - textSize.Width) / 2,
                (heightCells * cellSize - textSize.Height) / 2 + 40);
        }
        public void HandleInput(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                    if (snake.CurrentDirection != Direction.Down) 
                        snake.CurrentDirection = Direction.Up;
                    break;
                case Keys.Down:
                    if (snake.CurrentDirection != Direction.Up) 
                        snake.CurrentDirection = Direction.Down;
                    break;
                case Keys.Left:
                    if (snake.CurrentDirection != Direction.Right) 
                        snake.CurrentDirection = Direction.Left;
                    break;
                case Keys.Right:
                    if (snake.CurrentDirection != Direction.Left) 
                        snake.CurrentDirection = Direction.Right;
                    break;
            }
        }
        public void Restart()
        {
            snake = new Snake(widthCells / 2, heightCells / 2, widthCells, heightCells);
            GenerateFood();
            GenerateObstacles(30);
            gameOver = false;
            gameWin = false;
        }
    }
}