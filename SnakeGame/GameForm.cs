using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnakeGame;

public sealed class GameForm : Form
{
    const int CellSize = 22;
    const int GridWidth = 28;
    const int GridHeight = 22;

    // Level/speed tuning
    const int FoodsPerLevel = 5;
    const int StartLevel = 1;
    const int StartIntervalMs = 160; // slower start
    const int IntervalDecreasePerLevelMs = 12;
    const int MinIntervalMs = 55; // max speed cap (lower = faster)

    readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = StartIntervalMs };
    readonly List<Point> _snake = new();
    Point _food;
    Point _direction = new(1, 0);
    Point _nextDirection = new(1, 0);
    int _score;
    int _foodsEaten;
    int _level;
    bool _gameOver;
    readonly Random _rng = new();

    public GameForm()
    {
        Text = "Snake — .NET";
        ClientSize = new Size(GridWidth * CellSize + 1, GridHeight * CellSize + 1 + 36);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 28, 36);
        KeyPreview = true;

        DoubleBuffered = true;

        _gameTimer.Tick += (_, _) => GameTick();
        ResetGame();
        _gameTimer.Start();

        KeyDown += OnKeyDown;
        Paint += OnPaint;
    }

    void ResetGame()
    {
        _snake.Clear();
        var start = new Point(GridWidth / 2, GridHeight / 2);
        _snake.Add(start);
        _snake.Add(new Point(start.X - 1, start.Y));
        _snake.Add(new Point(start.X - 2, start.Y));
        _direction = new Point(1, 0);
        _nextDirection = _direction;
        _score = 0;
        _foodsEaten = 0;
        _level = StartLevel;
        _gameOver = false;
        ApplySpeedForCurrentLevel();
        SpawnFood();
    }

    void ApplySpeedForCurrentLevel()
    {
        var interval = StartIntervalMs - (_level - StartLevel) * IntervalDecreasePerLevelMs;
        if (interval < MinIntervalMs) interval = MinIntervalMs;
        _gameTimer.Interval = interval;
        Text = $"Snake — .NET — Level: {_level}";
    }

    void SpawnFood()
    {
        var occupied = new HashSet<Point>(_snake);
        Point p;
        do
        {
            p = new Point(_rng.Next(GridWidth), _rng.Next(GridHeight));
        } while (occupied.Contains(p));
        _food = p;
    }

    void GameTick()
    {
        if (_gameOver)
            return;

        _direction = _nextDirection;
        var head = _snake[0];
        var newHead = new Point(head.X + _direction.X, head.Y + _direction.Y);

        if (newHead.X < 0 || newHead.X >= GridWidth || newHead.Y < 0 || newHead.Y >= GridHeight)
        {
            EndGame();
            return;
        }

        if (_snake.Contains(newHead))
        {
            EndGame();
            return;
        }

        _snake.Insert(0, newHead);

        if (newHead == _food)
        {
            _score += 10;
            _foodsEaten++;

            var computedLevel = StartLevel + (_foodsEaten / FoodsPerLevel);
            if (computedLevel != _level)
            {
                _level = computedLevel;
                ApplySpeedForCurrentLevel();
            }

            SpawnFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        Invalidate();
    }

    void EndGame()
    {
        _gameOver = true;
        Text = $"Snake — Game over — Score: {_score} — Level: {_level} (Space to restart)";
        Invalidate();
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_gameOver)
        {
            if (e.KeyCode == Keys.Space)
            {
                Text = "Snake — .NET";
                ResetGame();
                _gameTimer.Start();
                Invalidate();
            }
            return;
        }

        var d = _direction;
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.W:
                if (d.Y == 0) _nextDirection = new Point(0, -1);
                break;
            case Keys.Down:
            case Keys.S:
                if (d.Y == 0) _nextDirection = new Point(0, 1);
                break;
            case Keys.Left:
            case Keys.A:
                if (d.X == 0) _nextDirection = new Point(-1, 0);
                break;
            case Keys.Right:
            case Keys.D:
                if (d.X == 0) _nextDirection = new Point(1, 0);
                break;
        }
    }

    void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        var gridRect = new Rectangle(0, 0, GridWidth * CellSize, GridHeight * CellSize);
        using var gridBrush = new SolidBrush(Color.FromArgb(32, 38, 48));
        g.FillRectangle(gridBrush, gridRect);

        using var gridPen = new Pen(Color.FromArgb(45, 52, 64));
        for (var x = 0; x <= GridWidth; x++)
            g.DrawLine(gridPen, x * CellSize, 0, x * CellSize, GridHeight * CellSize);
        for (var y = 0; y <= GridHeight; y++)
            g.DrawLine(gridPen, 0, y * CellSize, GridWidth * CellSize, y * CellSize);

        var pad = 2;
        var foodRect = new Rectangle(_food.X * CellSize + pad, _food.Y * CellSize + pad,
            CellSize - pad * 2, CellSize - pad * 2);
        using (var foodBrush = new SolidBrush(Color.FromArgb(220, 80, 90)))
            g.FillEllipse(foodBrush, foodRect);

        for (var i = 0; i < _snake.Count; i++)
        {
            var seg = _snake[i];
            var r = new Rectangle(seg.X * CellSize + pad, seg.Y * CellSize + pad,
                CellSize - pad * 2, CellSize - pad * 2);
            var green = i == 0
                ? Color.FromArgb(120, 220, 140)
                : Color.FromArgb(70, 180, 100);
            using var b = new SolidBrush(green);
            g.FillRectangle(b, r);
        }

        var hudY = GridHeight * CellSize + 6;
        using var font = new Font("Segoe UI", 10f, FontStyle.Regular);
        using var hudBrush = new SolidBrush(Color.FromArgb(200, 210, 220));
        g.DrawString($"Score: {_score}   Level: {_level}   Speed: {_gameTimer.Interval}ms   Arrows/WASD   Space: restart", font, hudBrush, 8, hudY);

        if (_gameOver)
        {
            using var overlay = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
            g.FillRectangle(overlay, gridRect);
            using var bigFont = new Font("Segoe UI", 18f, FontStyle.Bold);
            using var centerBrush = new SolidBrush(Color.White);
            var msg = $"Game over — {_score} pts — Level {_level}";
            var sz = g.MeasureString(msg, bigFont);
            g.DrawString(msg, bigFont, centerBrush,
                (gridRect.Width - sz.Width) / 2f, (gridRect.Height - sz.Height) / 2f);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _gameTimer.Dispose();
        base.Dispose(disposing);
    }
}
