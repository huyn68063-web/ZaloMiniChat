using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ZaloMini.Client
{
    internal sealed class GradientPanel : Panel
    {
        public Color Color1 { get; set; } = Color.FromArgb(15, 18, 26);
        public Color Color2 { get; set; } = Color.FromArgb(0, 104, 255);
        public float Angle { get; set; } = 135F;

        public GradientPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle rect = ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            // Clear trước để không “đọng” màu khi resize
            e.Graphics.Clear(Color1);

            using (var brush = new LinearGradientBrush(rect, Color1, Color2, Angle))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }
    }

    internal sealed class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 18;
        public Color FillColor { get; set; } = Color.FromArgb(28, 30, 38);
        public Color BorderColor { get; set; } = Color.FromArgb(55, 255, 255, 255);
        public int BorderThickness { get; set; } = 1;

        private Rectangle _lastBounds;

        public RoundedPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.Transparent;
            _lastBounds = Bounds;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            _lastBounds = Bounds;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            Rectangle old = _lastBounds;
            base.OnLocationChanged(e);
            _lastBounds = Bounds;

            InvalidateParent(old, _lastBounds);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            Rectangle old = _lastBounds;
            base.OnSizeChanged(e);
            _lastBounds = Bounds;

            UpdateRegion();
            Invalidate();

            InvalidateParent(old, _lastBounds);
        }

        private void InvalidateParent(Rectangle oldBounds, Rectangle newBounds)
        {
            if (Parent == null)
            {
                return;
            }

            // Repaint cả vùng cũ + mới để không để lại “vệt”
            Parent.Invalidate(oldBounds, true);
            Parent.Invalidate(newBounds, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Transparent background; parent paints.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int r = Math.Max(8, CornerRadius);

            using (GraphicsPath path = CreateRoundedRectPath(rect, r))
            using (SolidBrush fill = new SolidBrush(FillColor))
            using (Pen border = new Pen(BorderColor, Math.Max(1, BorderThickness)))
            {
                e.Graphics.FillPath(fill, path);
                if (BorderThickness > 0)
                {
                    border.Alignment = PenAlignment.Inset;
                    border.LineJoin = LineJoin.Round;
                    e.Graphics.DrawPath(border, path);
                }
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, Width, Height), Math.Max(8, CornerRadius)))
            {
                Region = new Region(path);
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    internal sealed class GradientButton : Control
    {
        public Color Color1 { get; set; } = UiTheme.PrimaryColor;
        public Color Color2 { get; set; } = UiTheme.PrimaryHoverColor;
        public int CornerRadius { get; set; } = 22;

        private bool _hovered;

        public GradientButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            TabStop = true;

            ForeColor = Color.White;
            Font = new Font(UiTheme.BaseFont.FontFamily, 11F, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Height = 48;

            MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _hovered = false; Invalidate(); };
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int r = Math.Max(8, CornerRadius);

            Color c1 = _hovered ? Color2 : Color1;
            Color c2 = _hovered ? Color1 : Color2;

            using (GraphicsPath path = CreateRoundedRectPath(rect, r))
            using (var brush = new LinearGradientBrush(rect, c1, c2, 0F))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                rect,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                OnClick(EventArgs.Empty);
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    internal sealed class BorderlessTabControl : TabControl
    {
        private const int TcmAdjustRect = 0x1328;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == TcmAdjustRect && !DesignMode && m.LParam != IntPtr.Zero)
            {
                // Mở rộng vùng hiển thị để nuốt sạch viền TabControl (khung trắng)
                Rect rc = Marshal.PtrToStructure<Rect>(m.LParam);
                rc.Left -= 2;
                rc.Top -= 2;
                rc.Right += 2;
                rc.Bottom += 2;
                Marshal.StructureToPtr(rc, m.LParam, false);

                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }
    }
}