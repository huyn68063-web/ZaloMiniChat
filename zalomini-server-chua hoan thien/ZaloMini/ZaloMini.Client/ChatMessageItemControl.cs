using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    internal sealed class ChatMessageItemControl : UserControl
    {
        // Giống ảnh: bubble không co quá nhỏ, nhìn "pill" và đồng đều
        private const int MinBubbleWidth = 60;
        private const int MaxBubbleWidthCap = 520;

        private readonly BubblePanel _bubble;
        private readonly Label _lblBody;
        private readonly Label _lblMeta;

        private readonly Panel _fileRow;
        private readonly Label _lblFileIcon;
        private readonly Label _lblFileName;
        private readonly Label _lblFileSize;
        private readonly Button _btnDownload;

        private bool _isMe;

        public event Action DownloadClicked;

        public ChatMessageItemControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            BackColor = UiTheme.SurfaceColor;
            Margin = new Padding(0, 2, 0, 2);

            _bubble = new BubblePanel();
            _bubble.AutoSize = false;
            _bubble.CornerRadius = 8;
            _bubble.IsMe = false;

            _lblBody = new Label();
            _lblBody.AutoSize = false;
            _lblBody.BackColor = Color.Transparent;
            _lblBody.Font = new Font(UiTheme.BaseFont.FontFamily, UiTheme.BaseFont.Size + 1F, FontStyle.Regular);

            // Đổi từ MiddleCenter -> MiddleLeft để giống thời gian (bên trái)
            _lblBody.TextAlign = ContentAlignment.MiddleLeft;

            // Meta: chỉ giờ (hiển thị cho tin cuối cụm) -> nằm dưới, bên trái (giống bạn yêu cầu)
            _lblMeta = new Label();
            _lblMeta.AutoSize = false;
            _lblMeta.BackColor = Color.Transparent;
            _lblMeta.Font = new Font(UiTheme.BaseFont.FontFamily, 8.25F, FontStyle.Regular);
            _lblMeta.TextAlign = ContentAlignment.MiddleLeft;
            _lblMeta.Visible = false;

            _fileRow = new Panel();
            _fileRow.AutoSize = false;
            _fileRow.BackColor = Color.Transparent;

            _lblFileIcon = new Label();
            _lblFileIcon.AutoSize = true;
            _lblFileIcon.BackColor = Color.Transparent;
            _lblFileIcon.Text = "📎";
            _lblFileIcon.Font = new Font("Segoe UI Emoji", 12F, FontStyle.Regular);

            _lblFileName = new Label();
            _lblFileName.AutoSize = false;
            _lblFileName.BackColor = Color.Transparent;
            _lblFileName.Font = new Font(UiTheme.BaseFont.FontFamily, UiTheme.BaseFont.Size + 1F, FontStyle.Bold);

            _lblFileSize = new Label();
            _lblFileSize.AutoSize = true;
            _lblFileSize.BackColor = Color.Transparent;
            _lblFileSize.ForeColor = UiTheme.MutedTextColor;

            _btnDownload = new Button();
            _btnDownload.Text = "Tải về";
            _btnDownload.Height = 28;
            _btnDownload.Width = 72;
            _btnDownload.FlatStyle = FlatStyle.Flat;
            _btnDownload.FlatAppearance.BorderSize = 0;
            _btnDownload.BackColor = Color.FromArgb(240, 240, 240);
            _btnDownload.Cursor = Cursors.Hand;
            _btnDownload.Click += (s, e) => DownloadClicked?.Invoke();

            _fileRow.Controls.Add(_lblFileIcon);
            _fileRow.Controls.Add(_lblFileName);
            _fileRow.Controls.Add(_lblFileSize);
            _fileRow.Controls.Add(_btnDownload);

            _bubble.Controls.Add(_lblBody);
            _bubble.Controls.Add(_fileRow);
            _bubble.Controls.Add(_lblMeta);

            Controls.Add(_bubble);

            SizeChanged += (s, e) => LayoutBubble();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                Width = 560;
                SetText("18:18", "hello", isMe: true, showMeta: true);
            }
        }

        public void SetText(string meta, string text, bool isMe, bool showMeta)
        {
            _isMe = isMe;

            _lblBody.Text = text ?? string.Empty;
            _lblBody.Visible = true;

            _fileRow.Visible = false;

            _lblMeta.Text = meta ?? string.Empty;
            _lblMeta.Visible = showMeta;

            ApplyColors();
            ApplyPadding();
            LayoutBubble();
        }

        public void SetFile(string meta, string fileName, long fileSizeBytes, bool isMe, bool showMeta, bool showDownload)
        {
            _isMe = isMe;

            _lblBody.Text = string.Empty;
            _lblBody.Visible = false;

            _fileRow.Visible = true;
            _lblFileName.Text = fileName ?? "file";
            _lblFileSize.Text = FormatFileSize(fileSizeBytes);

            _btnDownload.Visible = showDownload;

            _lblMeta.Text = meta ?? string.Empty;
            _lblMeta.Visible = showMeta;

            ApplyColors();
            ApplyPadding();
            LayoutBubble();
        }

        public void SetMetaVisible(bool visible)
        {
            if (_lblMeta.Visible == visible)
            {
                return;
            }

            _lblMeta.Visible = visible;
            LayoutBubble();
        }

        private void ApplyColors()
        {
            // Outgoing: xanh Zalo; Incoming: trắng
            _bubble.FillColor = _isMe ? UiTheme.OutgoingBubbleColor : Color.White;
            _bubble.IsMe = _isMe;

            // Incoming có viền nhẹ; Outgoing không viền (giống ảnh)
            _bubble.BorderThickness = _isMe ? 0F : 1F;
            _bubble.BorderColor = Color.FromArgb(210, 225, 245);

            _lblBody.ForeColor = _isMe ? Color.White : Color.FromArgb(25, 25, 25);
            _lblFileName.ForeColor = _lblBody.ForeColor;

            // Meta: outgoing trắng mờ; incoming xám xanh nhẹ
            _lblMeta.ForeColor = _isMe
                ? Color.FromArgb(210, 255, 255, 255)
                : Color.FromArgb(120, 130, 145);

            _btnDownload.BackColor = Color.FromArgb(240, 240, 240);
            _btnDownload.ForeColor = Color.Black;
        }

        private void ApplyPadding()
        {
            // thấp hơn, gọn hơn
            _bubble.Padding = new Padding(10, 6, 10, 6);
        }

        private void LayoutBubble()
        {
            if (Width <= 0)
            {
                return;
            }

            int maxBubbleWidth = Math.Min(MaxBubbleWidthCap, Math.Max(220, Width - 60));
            int contentMaxWidth = Math.Max(80, maxBubbleWidth - _bubble.Padding.Horizontal);

            bool hasMeta = _lblMeta.Visible && !string.IsNullOrWhiteSpace(_lblMeta.Text);

            int metaW = 0;
            int metaH = 0;
            if (hasMeta)
            {
                Size ms = MeasureSingleLine(_lblMeta.Text, _lblMeta.Font);
                metaW = ms.Width;
                metaH = Math.Max(14, ms.Height);
            }

            int x0 = _bubble.Padding.Left;
            int y = _bubble.Padding.Top;

            // ===== FILE =====
            if (_fileRow.Visible)
            {
                Size nameSize = MeasureSingleLine(_lblFileName.Text, _lblFileName.Font);
                Size sizeSize = MeasureSingleLine(_lblFileSize.Text, _lblFileSize.Font);

                int iconW = _lblFileIcon.PreferredSize.Width;
                int textBlockW = Math.Max(nameSize.Width, sizeSize.Width);
                int btnW = _btnDownload.Visible ? _btnDownload.Width : 0;
                int gap = _btnDownload.Visible ? 10 : 0;

                int rowInnerW = iconW + 6 + textBlockW + gap + btnW;
                int innerW = Math.Max(rowInnerW, metaW);
                innerW = Math.Max(innerW, MinBubbleWidth - _bubble.Padding.Horizontal);
                innerW = Math.Min(innerW, contentMaxWidth);

                int bubbleW = innerW + _bubble.Padding.Horizontal;

                _fileRow.SetBounds(x0, y, innerW, 48);
                _lblFileIcon.Location = new Point(0, 0);

                int btnX = innerW - btnW;
                if (_btnDownload.Visible)
                {
                    _btnDownload.Location = new Point(btnX, 0);
                }

                int textLeft = _lblFileIcon.Right + 6;
                int textRight = _btnDownload.Visible ? (btnX - gap) : innerW;
                int nameMax = Math.Max(60, textRight - textLeft);

                _lblFileName.Location = new Point(textLeft, 0);
                _lblFileName.Size = new Size(nameMax, 22);

                _lblFileSize.Location = new Point(textLeft, 24);

                y = _fileRow.Bottom;

                if (hasMeta)
                {
                    y += 2;
                    _lblMeta.SetBounds(x0, y, innerW, metaH);
                    y = _lblMeta.Bottom;
                }

                int bubbleH = y + _bubble.Padding.Bottom;
                _bubble.Size = new Size(bubbleW, bubbleH);

                Height = _bubble.Height;

                int bx = _isMe ? (Width - _bubble.Width - 10) : 10;
                _bubble.Location = new Point(Math.Max(0, bx), 0);
                _bubble.Invalidate();
                return;
            }

            // ===== TEXT =====
            string bodyText = _lblBody.Text ?? string.Empty;

            Size bodySize;
            bool singleLine = bodyText.IndexOf('\n') < 0 && bodyText.IndexOf('\r') < 0;

            if (singleLine)
            {
                // Đo sát theo nội dung để bubble không dài dư
                Size one = MeasureSingleLine(bodyText, _lblBody.Font);

                // Clamp để không vượt max
                int w = Math.Min(one.Width, contentMaxWidth);
                bodySize = new Size(w, one.Height);
            }
            else
            {
                bodySize = MeasureMultiLine(bodyText, _lblBody.Font, contentMaxWidth);
            }

            int bodyW = bodySize.Width;
            int bodyH = bodySize.Height;

            int innerTextW = Math.Max(bodyW, metaW);
            innerTextW = Math.Max(innerTextW, MinBubbleWidth - _bubble.Padding.Horizontal);
            innerTextW = Math.Min(innerTextW, contentMaxWidth);

            int bubbleWidth = innerTextW + _bubble.Padding.Horizontal;

            _lblBody.SetBounds(x0, y, innerTextW, bodyH + 4); // thêm khoảng thở để chữ không sát xuống
            y = _lblBody.Bottom;

            // Thời gian nằm dưới, trong bubble, bên trái (giống bạn yêu cầu)
            if (hasMeta)
            {
                y += 2;
                _lblMeta.SetBounds(x0, y, innerTextW, metaH);
                y = _lblMeta.Bottom;
            }

            int bubbleHeight = y + _bubble.Padding.Bottom;
            _bubble.Size = new Size(bubbleWidth, bubbleHeight);

            Height = _bubble.Height;

            int x = _isMe ? (Width - _bubble.Width - 10) : 10;
            _bubble.Location = new Point(Math.Max(0, x), 0);

            _bubble.Invalidate();
        }

        private static Size MeasureSingleLine(string text, Font font)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new Size(0, 0);
            }

            return TextRenderer.MeasureText(
                text,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
        }

        private static Size MeasureMultiLine(string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new Size(0, 0);
            }

            Size s = TextRenderer.MeasureText(text, font, new Size(maxWidth, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.WordBreak);

            return new Size(Math.Min(s.Width, maxWidth), s.Height);
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
            if (bytes < 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.#} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.#} GB";
        }

        private sealed class BubblePanel : Panel
        {
            private Region _bubbleRegion;

            public Color FillColor { get; set; } = Color.LightGray;
            public int CornerRadius { get; set; } = 16;
            public bool IsMe { get; set; }

            public Color BorderColor { get; set; } = Color.FromArgb(210, 225, 245);
            public float BorderThickness { get; set; } = 1F;

            public BubblePanel()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw,
                    true);

                BackColor = Color.Transparent;
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                UpdateRegion();
                Invalidate();
            }

            private void UpdateRegion()
            {
                _bubbleRegion?.Dispose();

                if (Width <= 0 || Height <= 0)
                {
                    return;
                }

                // Region dùng rect full để không bị clip viền
                RectangleF rect = new RectangleF(0f, 0f, Width, Height);
                using (GraphicsPath path = CreateRoundedRectPath(rect, CornerRadius))
                {
                    _bubbleRegion = new Region(path);
                }

                Region = _bubbleRegion;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _bubbleRegion?.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                if (Parent != null)
                {
                    using (var brush = new SolidBrush(Parent.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, ClientRectangle);
                    }
                }
                else
                {
                    e.Graphics.Clear(Color.White);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

                if (Width <= 1 || Height <= 1)
                {
                    return;
                }

                // Vẽ inset 0.5f để nét bo mịn, không bị “răng cưa”
                RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

                using (GraphicsPath path = CreateRoundedRectPath(rect, CornerRadius))
                using (SolidBrush fill = new SolidBrush(FillColor))
                {
                    e.Graphics.FillPath(fill, path);

                    if (BorderThickness > 0.01F)
                    {
                        using (Pen pen = new Pen(BorderColor, BorderThickness))
                        {
                            pen.Alignment = PenAlignment.Inset;
                            pen.LineJoin = LineJoin.Round;
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }
            }

            private static GraphicsPath CreateRoundedRectPath(RectangleF rect, int radius)
            {
                var path = new GraphicsPath();

                float r = Math.Max(1f, radius);
                float maxR = Math.Min(rect.Width, rect.Height) / 2f;
                if (r > maxR)
                {
                    r = maxR;
                }

                float d = r * 2f;

                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();

                return path;
            }
        }
    }
}