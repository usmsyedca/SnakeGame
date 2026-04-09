using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnakeGame;

public sealed class GameForm : Form
{
    const int CellSize = 22;
    const int GridWidth = 28;
    const int GridHeight = 22;
    const int FlameDurationTicks = 10;

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
    int _flameTicksLeft;
    Point _flameDir = new(1, 0);

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
        _flameTicksLeft = 0;
        _flameDir = _direction;
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
        if (_flameTicksLeft > 0)
            _flameTicksLeft--;
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
            _flameTicksLeft = FlameDurationTicks;
            _flameDir = _direction;

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

        g.SmoothingMode = SmoothingMode.AntiAlias;
        DrawSnake(g);
        if (_flameTicksLeft > 0 && !_gameOver)
            DrawFlame(g, _snake[0], _flameDir, _flameTicksLeft);

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

    static RectangleF CellRect(Point cell, float inset)
    {
        return new RectangleF(
            cell.X * CellSize + inset,
            cell.Y * CellSize + inset,
            CellSize - inset * 2f,
            CellSize - inset * 2f
        );
    }

    static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2f;
        if (d <= 0.01f)
        {
            path.AddRectangle(r);
            path.CloseFigure();
            return path;
        }

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    void DrawSnake(Graphics g)
    {
        if (_snake.Count == 0)
            return;

        // Palette tuned for a "scaled" look on dark grid.
        var outline = Color.FromArgb(24, 10, 12);
        var bodyDark = Color.FromArgb(32, 118, 74);
        var bodyMid = Color.FromArgb(48, 156, 92);
        var bodyLight = Color.FromArgb(118, 214, 132);
        var headDark = Color.FromArgb(26, 102, 66);
        var headLight = Color.FromArgb(144, 232, 156);

        var inset = 1.75f;
        for (var i = _snake.Count - 1; i >= 0; i--)
        {
            var cell = _snake[i];
            var r = CellRect(cell, inset);

            // Slight taper near tail to feel less "blocky".
            var t = _snake.Count <= 2 ? 0f : (float)i / (_snake.Count - 1);
            var taper = 1.0f + 2.2f * t; // head bigger, tail smaller
            var rr = new RectangleF(
                r.X + taper * 0.25f,
                r.Y + taper * 0.25f,
                Math.Max(3f, r.Width - taper * 0.5f),
                Math.Max(3f, r.Height - taper * 0.5f)
            );

            using var path = RoundedRect(rr, radius: rr.Width * 0.38f);
            using var pen = new Pen(outline, 1.2f) { LineJoin = LineJoin.Round };

            if (i == 0)
            {
                // Head
                var dir = _direction;
                var angle = dir.X == 1 ? 0f : dir.X == -1 ? 180f : dir.Y == 1 ? 90f : 270f;
                using var headBrush = new LinearGradientBrush(rr, headLight, headDark, angle);
                g.FillPath(headBrush, path);
                g.DrawPath(pen, path);
                DrawHeadDetails(g, rr, dir);
            }
            else
            {
                // Body segment "scale" sheen.
                using var segBrush = new LinearGradientBrush(rr, bodyLight, bodyDark, 90f);
                var cb = new ColorBlend
                {
                    Positions = [0f, 0.55f, 1f],
                    Colors = [bodyLight, bodyMid, bodyDark]
                };
                segBrush.InterpolationColors = cb;
                g.FillPath(segBrush, path);
                g.DrawPath(pen, path);

                // Subtle scale texture: tiny highlight arcs.
                using var scalePen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f);
                var cx = rr.X + rr.Width / 2f;
                var cy = rr.Y + rr.Height / 2f;
                var rad = rr.Width * 0.36f;
                g.DrawArc(scalePen, cx - rad, cy - rad, rad * 2, rad * 2, 210, 60);
                g.DrawArc(scalePen, cx - rad * 0.9f, cy - rad * 0.1f, rad * 1.8f, rad * 1.8f, 245, 45);
            }
        }
    }

    static void DrawHeadDetails(Graphics g, RectangleF headRect, Point dir)
    {
        // Eyes placement depends on direction.
        var eyeWhite = Color.FromArgb(235, 245, 245);
        var pupil = Color.FromArgb(25, 25, 25);
        var mouth = Color.FromArgb(120, 12, 12);

        // Local coordinates (0..1) then mapped into rect.
        (float ex1, float ey1, float ex2, float ey2, float mx1, float my1, float mx2, float my2) loc = dir switch
        {
            { X: 1, Y: 0 } => (0.62f, 0.32f, 0.62f, 0.68f, 0.80f, 0.42f, 0.92f, 0.58f),
            { X: -1, Y: 0 } => (0.38f, 0.32f, 0.38f, 0.68f, 0.08f, 0.42f, 0.20f, 0.58f),
            { X: 0, Y: 1 } => (0.32f, 0.62f, 0.68f, 0.62f, 0.42f, 0.80f, 0.58f, 0.92f),
            _ => (0.32f, 0.38f, 0.68f, 0.38f, 0.42f, 0.08f, 0.58f, 0.20f),
        };

        PointF P(float x, float y) => new(headRect.X + x * headRect.Width, headRect.Y + y * headRect.Height);

        var eyeR = Math.Max(2.4f, headRect.Width * 0.12f);
        var pupilR = eyeR * 0.45f;
        var e1 = P(loc.ex1, loc.ey1);
        var e2 = P(loc.ex2, loc.ey2);

        using (var b = new SolidBrush(eyeWhite))
        {
            g.FillEllipse(b, e1.X - eyeR, e1.Y - eyeR, eyeR * 2, eyeR * 2);
            g.FillEllipse(b, e2.X - eyeR, e2.Y - eyeR, eyeR * 2, eyeR * 2);
        }
        using (var b = new SolidBrush(pupil))
        {
            g.FillEllipse(b, e1.X - pupilR, e1.Y - pupilR, pupilR * 2, pupilR * 2);
            g.FillEllipse(b, e2.X - pupilR, e2.Y - pupilR, pupilR * 2, pupilR * 2);
        }

        // Mouth hint near the front.
        var m1 = P(loc.mx1, loc.my1);
        var m2 = P(loc.mx2, loc.my2);
        using var mouthPen = new Pen(Color.FromArgb(140, mouth), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(mouthPen, m1, m2);
    }

    void DrawFlame(Graphics g, Point headCell, Point dir, int ticksLeft)
    {
        var t = 1f - (ticksLeft / (float)FlameDurationTicks); // 0..1
        var intensity = 1f - t; // 1..0
        var alpha = (int)(200 * intensity);
        if (alpha < 0) alpha = 0;
        if (alpha > 220) alpha = 220;

        var head = CellRect(headCell, inset: 1.6f);
        var cx = head.X + head.Width / 2f;
        var cy = head.Y + head.Height / 2f;

        var front = dir switch
        {
            { X: 1, Y: 0 } => new PointF(head.Right, cy),
            { X: -1, Y: 0 } => new PointF(head.Left, cy),
            { X: 0, Y: 1 } => new PointF(cx, head.Bottom),
            _ => new PointF(cx, head.Top),
        };

        // Flame length scales with intensity and a touch of flicker.
        var flicker = (float)(0.75 + 0.25 * Math.Sin(Environment.TickCount / 35.0));
        var length = (CellSize * (1.15f + 0.7f * intensity)) * flicker;
        var width = CellSize * (0.48f + 0.18f * intensity);

        // Build a teardrop-ish flame along direction.
        var tip = new PointF(front.X + dir.X * length, front.Y + dir.Y * length);
        var ortho = new PointF(-dir.Y, dir.X);

        PointF O(float along, float off)
        {
            return new PointF(
                front.X + dir.X * along + ortho.X * off,
                front.Y + dir.Y * along + ortho.Y * off
            );
        }

        // Outer flame
        using (var outer = new GraphicsPath())
        {
            outer.AddBezier(O(0, 0), O(length * 0.25f, width * 0.85f), O(length * 0.6f, width * 0.35f), tip);
            outer.AddBezier(tip, O(length * 0.6f, -width * 0.35f), O(length * 0.25f, -width * 0.85f), O(0, 0));
            outer.CloseFigure();

            using var pgb = new PathGradientBrush(outer);
            pgb.CenterPoint = O(length * 0.25f, 0);
            pgb.CenterColor = Color.FromArgb(alpha, 255, 245, 120);
            pgb.SurroundColors = [Color.FromArgb(alpha, 255, 110, 30)];
            g.FillPath(pgb, outer);
        }

        // Inner flame
        var innerAlpha = (int)(alpha * 0.92f);
        using (var inner = new GraphicsPath())
        {
            var innerLen = length * 0.72f;
            var innerW = width * 0.55f;
            var innerTip = new PointF(front.X + dir.X * innerLen, front.Y + dir.Y * innerLen);
            inner.AddBezier(O(0, 0), O(innerLen * 0.28f, innerW), O(innerLen * 0.62f, innerW * 0.35f), innerTip);
            inner.AddBezier(innerTip, O(innerLen * 0.62f, -innerW * 0.35f), O(innerLen * 0.28f, -innerW), O(0, 0));
            inner.CloseFigure();

            using var pgb = new PathGradientBrush(inner);
            pgb.CenterPoint = O(innerLen * 0.24f, 0);
            pgb.CenterColor = Color.FromArgb(innerAlpha, 255, 255, 220);
            pgb.SurroundColors = [Color.FromArgb(innerAlpha, 255, 170, 50)];
            g.FillPath(pgb, inner);
        }

        // Small embers.
        var emberCount = 3 + (int)(3 * intensity);
        using var emberBrush = new SolidBrush(Color.FromArgb((int)(alpha * 0.65f), 255, 210, 90));
        for (var i = 0; i < emberCount; i++)
        {
            var along = length * (0.25f + 0.65f * (float)_rng.NextDouble());
            var off = width * (float)(_rng.NextDouble() - 0.5) * 0.8f;
            var p = O(along, off);
            var r = 1.2f + 1.8f * (float)_rng.NextDouble();
            g.FillEllipse(emberBrush, p.X - r, p.Y - r, r * 2, r * 2);
        }
    }
}
