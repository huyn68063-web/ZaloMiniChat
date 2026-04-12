using System;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormChat : Form
    {
        private readonly NetworkClient _network;
        private readonly string _username;
        private readonly NotifyIcon _notifyIcon;

        private readonly HashSet<string> _onlineUsers = new HashSet<string>();
        private readonly HashSet<string> _offlineUsers = new HashSet<string>();

        private const string InputPlaceholder = "Nhập tin nhắn...";

        // Offline demo users để test UX/UI (không cần server)
        private readonly string[] _demoUsers = new[] { "Quỳnh", "Huy", "Quang", "Hùng" };
        private readonly Random _rnd = new Random();

        // UI chat bubble container (thay cho RichTextBox)
        private FlowLayoutPanel _messageFlow;

        // Offline: lưu mapping fileId -> filePath để nút "Tải về" có dữ liệu copy ra
        private readonly Dictionary<string, string> _demoFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ===== Grouping state (Bước 2) =====
        private string _lastSender;
        private bool _lastIsMe;
        private DateTime _lastMessageAt = DateTime.MinValue;
        private DateTime _lastDay = DateTime.MinValue;
        private static readonly TimeSpan GroupWindow = TimeSpan.FromMinutes(2);

        private ChatMessageItemControl _typingItem;
        private Panel _lastStatusHost;
        private Label _lastStatusLabel;
        private ChatMessageItemControl _lastMessageControl;

        private Action<string> _onMessageReceivedHandler;
        private Action _onDisconnectedHandler;

        public FormChat(NetworkClient network, string username)
        {
            InitializeComponent();

            _network = network;
            _username = username;
            Text = "ZaloMini - " + username;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Information;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "ZaloMini";

            _onlineUsers.Add(username);

            ApplyTheme();
            SetupMessageFlow();
            SetupInputPlaceholder();

            if (_network != null)
            {
                _onMessageReceivedHandler = msg =>
                {
                    if (IsDisposed || Disposing)
                    {
                        return;
                    }

                    BeginInvoke((Action)(() => XuLyTinNhan(msg)));
                };

                _onDisconnectedHandler = () =>
                {
                    if (IsDisposed || Disposing)
                    {
                        return;
                    }

                    BeginInvoke((Action)(() =>
                    {
                        AddSystemLine("*** Mất kết nối Server ***");
                        MessageBox.Show(
                            "Mất kết nối Server!\nVui lòng khởi động lại.",
                            "Mất kết nối",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }));
                };

                _network.OnMessageReceived += _onMessageReceivedHandler;
                _network.OnDisconnected += _onDisconnectedHandler;
            }

            FormClosing += FormChat_FormClosing;
            Load += FormChat_Load;

            // Style danh sách user giống giao diện Zalo (avatar + online dot)
            lstUsers.DrawMode = DrawMode.OwnerDrawFixed;
            lstUsers.ItemHeight = 36;
            lstUsers.DrawItem += LstUsers_DrawItem;
        }

        private void ApplyTheme()
        {
            UiTheme.ApplyFormBase(this);
            UiTheme.ApplyHeader(panel1);

            UiTheme.ApplyIconButton(btnSettings, UiTheme.PrimaryColor);
            UiTheme.ApplySecondaryButton(btnSendFile);
            UiTheme.ApplyPrimaryButton(btnSend);

            tableBottom.BackColor = Color.FromArgb(245, 245, 245);
            progressBar.ForeColor = UiTheme.PrimaryColor;
        }

        private void SetupMessageFlow()
        {
            // Ẩn RichTextBox cũ để không phải sửa Designer
            rtbMessages.Visible = false;

            _messageFlow = new FlowLayoutPanel();
            _messageFlow.Dock = DockStyle.Fill;
            _messageFlow.AutoScroll = true;
            _messageFlow.FlowDirection = FlowDirection.TopDown;
            _messageFlow.WrapContents = false;
            _messageFlow.BackColor = UiTheme.SurfaceColor;
            _messageFlow.Padding = new Padding(12, 10, 12, 10);

            splitContainer1.Panel2.Controls.Add(_messageFlow);

            _messageFlow.BringToFront();
            panel1.BringToFront();
            progressBar.BringToFront();
            tableBottom.BringToFront();

            _messageFlow.SizeChanged += (s, e) => ResizeMessageItems();

            _messageFlow.TabStop = true;
            _messageFlow.MouseEnter += (s, e) => _messageFlow.Focus();
        }

        private void ResizeMessageItems()
        {
            if (_messageFlow == null)
            {
                return;
            }

            int w = Math.Max(220, _messageFlow.ClientSize.Width - 25);
            foreach (Control c in _messageFlow.Controls)
            {
                c.Width = w;
            }
        }

        private void SetupInputPlaceholder()
        {
            txtMessage.ForeColor = UiTheme.MutedTextColor;
            txtMessage.Text = InputPlaceholder;

            txtMessage.GotFocus += (s, e) =>
            {
                if (string.Equals(txtMessage.Text, InputPlaceholder, StringComparison.Ordinal))
                {
                    txtMessage.Text = "";
                    txtMessage.ForeColor = Color.Black;
                }
            };

            txtMessage.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMessage.Text))
                {
                    txtMessage.ForeColor = UiTheme.MutedTextColor;
                    txtMessage.Text = InputPlaceholder;
                }
            };
        }

        private void FormChat_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;

            splitContainer1.SplitterWidth = 1;
            splitContainer1.Panel1MinSize = 260;
            splitContainer1.SplitterDistance = Math.Max(320, splitContainer1.Panel1MinSize);

            progressBar.Visible = false;

            LoadLichSu();
            SeedOfflineUsersIfNeeded();
            RefreshUserList();

            // Separator "Hôm nay" (Bước 3)
            EnsureDateSeparator(DateTime.Now);

            if (_network != null && _network.IsConnected)
            {
                _network.Send($"ONLINE|{_username}");
            }
            else
            {
                AddSystemLine("Chế độ offline (UI demo) - Chưa kết nối Server");
            }
        }

        private void SeedOfflineUsersIfNeeded()
        {
            if (_network != null && _network.IsConnected)
            {
                return;
            }

            foreach (string u in _demoUsers)
            {
                if (!string.Equals(u, _username, StringComparison.OrdinalIgnoreCase))
                {
                    _onlineUsers.Add(u);
                }
            }
        }

        private void LstUsers_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= lstUsers.Items.Count)
            {
                return;
            }

            string rawText = lstUsers.Items[e.Index]?.ToString() ?? string.Empty;

            bool isMe = rawText.Contains("(Bạn)");
            bool isOffline = rawText.StartsWith("(Offline)", StringComparison.OrdinalIgnoreCase);

            string displayName = rawText;
            if (isOffline)
            {
                displayName = rawText.Substring("(Offline)".Length).Trim();
            }

            if (isMe)
            {
                displayName = rawText.Replace("(Bạn)", "").Trim();
            }

            Rectangle bounds = e.Bounds;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int avatarSize = 26;
            int avatarX = bounds.Left + 8;
            int avatarY = bounds.Top + (bounds.Height - avatarSize) / 2;

            Color avatarBg = Color.FromArgb(230, 240, 255);
            using (var brush = new SolidBrush(avatarBg))
            {
                e.Graphics.FillEllipse(brush, avatarX, avatarY, avatarSize, avatarSize);
            }

            string initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim().Substring(0, 1).ToUpperInvariant();
            using (var f = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold))
            using (var b = new SolidBrush(UiTheme.PrimaryColor))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(initial, f, b, new RectangleF(avatarX, avatarY, avatarSize, avatarSize), sf);
            }

            int dotSize = 8;
            int dotX = avatarX + avatarSize - dotSize + 1;
            int dotY = avatarY + avatarSize - dotSize + 1;
            Color dotColor = isOffline ? Color.Gray : Color.FromArgb(0, 200, 80);
            using (var brush = new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(brush, dotX, dotY, dotSize, dotSize);
            }

            int textX = avatarX + avatarSize + 10;
            Color nameColor = isOffline ? Color.Gray : Color.Black;
            Font nameFont = isMe ? new Font(e.Font, FontStyle.Bold) : e.Font;

            TextRenderer.DrawText(
                e.Graphics,
                isMe ? (displayName + " (Bạn)") : (isOffline ? ("(Offline) " + displayName) : displayName),
                nameFont,
                new Rectangle(textX, bounds.Top, bounds.Width - textX, bounds.Height),
                nameColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (isMe && !ReferenceEquals(nameFont, e.Font))
            {
                nameFont.Dispose();
            }

            e.DrawFocusRectangle();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_network != null)
                {
                    if (_onMessageReceivedHandler != null)
                    {
                        _network.OnMessageReceived -= _onMessageReceivedHandler;
                        _onMessageReceivedHandler = null;
                    }

                    if (_onDisconnectedHandler != null)
                    {
                        _network.OnDisconnected -= _onDisconnectedHandler;
                        _onDisconnectedHandler = null;
                    }

                    if (_network.IsConnected)
                    {
                        _network.Send($"OFFLINE|{_username}");
                    }

                    _network.Disconnect();
                }

                _notifyIcon?.Dispose();
            }
            catch
            {
            }
        }

        // ====== Bước 3: Separator badge (ngày / hôm nay) ======
        private void EnsureDateSeparator(DateTime now)
        {
            DateTime day = now.Date;
            if (_lastDay == day)
            {
                return;
            }

            _lastDay = day;

            string text = day == DateTime.Today ? "Hôm nay" : day.ToString("dd/MM/yyyy");
            AddBadgeSeparator(text);

            // separator cũng reset grouping
            ResetGrouping();
        }

        private void AddBadgeSeparator(string text)
        {
            if (_messageFlow == null)
            {
                return;
            }

            // Container full width để luôn căn giữa
            var container = new Panel();
            container.BackColor = Color.Transparent;
            container.Margin = new Padding(0, 12, 0, 12);
            container.Height = 32;
            container.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.BackColor = Color.Transparent;
            layout.ColumnCount = 3;
            layout.RowCount = 1;
            layout.Padding = new Padding(0);

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var left = new Panel();
            left.Dock = DockStyle.Fill;
            left.Height = 1;
            left.BackColor = Color.Gainsboro;
            left.Margin = new Padding(0, 15, 10, 0);

            var right = new Panel();
            right.Dock = DockStyle.Fill;
            right.Height = 1;
            right.BackColor = Color.Gainsboro;
            right.Margin = new Padding(10, 15, 0, 0);

            var badge = new Label();
            badge.AutoSize = true;
            badge.Text = text ?? string.Empty;
            badge.ForeColor = UiTheme.MutedTextColor;
            badge.BackColor = Color.FromArgb(245, 245, 245);
            badge.Padding = new Padding(10, 4, 10, 4);
            badge.Font = new Font(UiTheme.BaseFont, FontStyle.Regular);

            layout.Controls.Add(left, 0, 0);
            layout.Controls.Add(badge, 1, 0);
            layout.Controls.Add(right, 2, 0);

            container.Controls.Add(layout);

            _messageFlow.Controls.Add(container);
            ResizeMessageItems();
            ScrollToBottom();

            // separator reset grouping
            ResetGrouping();
        }

        private void AddSystemLine(string text)
        {
            if (_messageFlow == null)
            {
                return;
            }

            var host = new Panel();
            host.BackColor = Color.Transparent;
            host.Margin = new Padding(0, 8, 0, 8);
            host.Height = 26;
            host.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);

            var lbl = new Label();
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.ForeColor = UiTheme.MutedTextColor;
            lbl.Font = new Font(UiTheme.BaseFont, FontStyle.Italic);
            lbl.Text = text ?? string.Empty;

            host.Controls.Add(lbl);

            _messageFlow.Controls.Add(host);
            ResizeMessageItems();
            ScrollToBottom();

            ResetGrouping();
        }

        // ====== Bước 2: Grouping ======
        private void ResetGrouping()
        {
            _lastSender = null;
            _lastIsMe = false;
            _lastMessageAt = DateTime.MinValue;
            _lastMessageControl = null;
        }

        private bool ShouldShowHeader(string sender, bool isMe, DateTime now)
        {
            if (string.IsNullOrEmpty(_lastSender))
            {
                return true;
            }

            if (!string.Equals(_lastSender, sender, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_lastIsMe != isMe)
            {
                return true;
            }

            if (now - _lastMessageAt > GroupWindow)
            {
                return true;
            }

            return false;
        }

        private void UpdateGroupingState(string sender, bool isMe, DateTime now)
        {
            _lastSender = sender;
            _lastIsMe = isMe;
            _lastMessageAt = now;
        }

        private void AddTextBubble(string sender, string content, bool isMe)
        {
            if (_messageFlow == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            EnsureDateSeparator(now);

            bool showHeader = ShouldShowHeader(sender, isMe, now);
            bool isContinuation = !showHeader;

            // Meta giống ảnh: chỉ hiện ở tin cuối cụm
            // Outgoing: chỉ giờ; Incoming: "Tên HH:mm"
            string meta = isMe ? now.ToString("HH:mm") : (sender + " " + now.ToString("HH:mm"));

            // Nếu đang cùng cụm => tin trước không còn là "cuối cụm" nữa => ẩn meta của tin trước
            if (isContinuation && _lastMessageControl != null)
            {
                _lastMessageControl.SetMetaVisible(false);
                _lastMessageControl.Margin = new Padding(0, 1, 0, 2);
            }

            var item = new ChatMessageItemControl();
            item.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);
            item.Margin = showHeader ? new Padding(0, 6, 0, 2) : new Padding(0, 1, 0, 2);
            item.SetText(meta, content, isMe, showMeta: true);

            _messageFlow.Controls.Add(item);
            ResizeMessageItems();
            ScrollToBottom();

            _lastMessageControl = item;
            UpdateGroupingState(sender, isMe, now);
        }

        private void AddFileBubble(string sender, string fileName, long fileSizeBytes, bool isMe, string demoFileId, bool showDownload)
        {
            if (_messageFlow == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            EnsureDateSeparator(now);

            bool showHeader = ShouldShowHeader(sender, isMe, now);
            bool isContinuation = !showHeader;

            string meta = isMe ? now.ToString("HH:mm") : (sender + " " + now.ToString("HH:mm"));

            if (isContinuation && _lastMessageControl != null)
            {
                _lastMessageControl.SetMetaVisible(false);
                _lastMessageControl.Margin = new Padding(0, 1, 0, 2);
            }

            var item = new ChatMessageItemControl();
            item.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);
            item.Margin = showHeader ? new Padding(0, 6, 0, 2) : new Padding(0, 1, 0, 2);
            item.SetFile(meta, fileName, fileSizeBytes, isMe, showMeta: true, showDownload: showDownload);

            if (showDownload)
            {
                item.DownloadClicked += () =>
                {
                    if (string.IsNullOrEmpty(demoFileId) ||
                        !_demoFileMap.TryGetValue(demoFileId, out string srcPath) ||
                        !File.Exists(srcPath))
                    {
                        MessageBox.Show(
                            "Chưa có dữ liệu file để tải (đang offline demo / chưa nhận bytes từ server).",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.FileName = Path.GetFileName(srcPath);
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            File.Copy(srcPath, sfd.FileName, overwrite: true);
                            _notifyIcon.ShowBalloonTip(1500, "ZaloMini", "Tải file thành công!", ToolTipIcon.Info);
                        }
                    }
                };
            }

            _messageFlow.Controls.Add(item);
            ResizeMessageItems();
            ScrollToBottom();

            _lastMessageControl = item;
            UpdateGroupingState(sender, isMe, now);
        }

        private static bool TryParseLogLine(string line, out DateTime timestamp, out string sender, out string content)
{
    timestamp = DateTime.MinValue;
    sender = "";
    content = "";

    if (string.IsNullOrWhiteSpace(line))
    {
        return false;
    }

    int close = line.IndexOf(']');
    if (!line.StartsWith("[", StringComparison.Ordinal) || close <= 1)
    {
        return false;
    }

    string timePart = line.Substring(1, close - 1).Trim();
    string rest = line.Substring(close + 1).Trim();

    int colon = rest.IndexOf(':');
    if (colon <= 0)
    {
        return false;
    }

    sender = rest.Substring(0, colon).Trim();
    content = rest.Substring(colon + 1).Trim();

    if (DateTime.TryParseExact(timePart, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t1))
    {
        timestamp = DateTime.Today.AddHours(t1.Hour).AddMinutes(t1.Minute);
        return true;
    }

    if (DateTime.TryParseExact(timePart, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t2))
    {
        timestamp = t2;
        return true;
    }

    return false;
}

private void AddTextBubbleAt(string sender, string content, bool isMe, DateTime now)
{
    if (_messageFlow == null)
    {
        return;
    }

    EnsureDateSeparator(now);

    bool showHeader = ShouldShowHeader(sender, isMe, now);
    bool isContinuation = !showHeader;

    string meta = isMe ? now.ToString("HH:mm") : (sender + " " + now.ToString("HH:mm"));

    if (isContinuation && _lastMessageControl != null)
    {
        _lastMessageControl.SetMetaVisible(false);
        _lastMessageControl.Margin = new Padding(0, 1, 0, 2);
    }

    var item = new ChatMessageItemControl();
    item.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);
    item.Margin = showHeader ? new Padding(0, 6, 0, 2) : new Padding(0, 1, 0, 2);
    item.SetText(meta, content, isMe, showMeta: true);

    _messageFlow.Controls.Add(item);
    ResizeMessageItems();
    ScrollToBottom();

    _lastMessageControl = item;
    UpdateGroupingState(sender, isMe, now);
}

        private void XuLyTinNhan(string msg)
        {
            try
            {
                if (msg.StartsWith("MSG|", StringComparison.Ordinal))
                {
                    string[] parts = msg.Split('|');
                    if (parts.Length >= 3)
                    {
                        string sender = parts[1];
                        string content = string.Join("|", parts.Skip(2));
                        string time = DateTime.Now.ToString("HH:mm");

                        AddTextBubble(sender, content, isMe: false);
                        LuuLichSu($"[{time}] {sender}: {content}");

                        if (!ContainsFocus)
                        {
                            _notifyIcon.ShowBalloonTip(2000, sender, content, ToolTipIcon.Info);
                        }
                    }

                    return;
                }

                if (msg.StartsWith("ONLINE|", StringComparison.Ordinal))
                {
                    string user = msg.Substring(7).Trim();
                    if (!_onlineUsers.Contains(user) && user != _username)
                    {
                        _onlineUsers.Add(user);
                        _offlineUsers.Remove(user);
                        RefreshUserList();
                        AddSystemLine($"✅ {user} đã tham gia");
                        _notifyIcon.ShowBalloonTip(1500, "ZaloMini", $"✅ {user} vừa online!", ToolTipIcon.Info);
                    }

                    return;
                }

                if (msg.StartsWith("OFFLINE|", StringComparison.Ordinal))
                {
                    string user = msg.Substring(8).Trim();
                    if (_onlineUsers.Contains(user))
                    {
                        _onlineUsers.Remove(user);
                        _offlineUsers.Add(user);
                        RefreshUserList();
                        AddSystemLine($"❌ {user} đã rời");
                    }

                    return;
                }

                if (msg.StartsWith("USERLIST|", StringComparison.Ordinal))
                {
                    string userList = msg.Substring(9);
                    string[] users = userList.Split(',');
                    _onlineUsers.Clear();

                    foreach (string user in users)
                    {
                        string u = user.Trim();
                        if (!string.IsNullOrEmpty(u))
                        {
                            _onlineUsers.Add(u);
                        }
                    }

                    RefreshUserList();
                    ResetGrouping();
                    return;
                }

                if (msg.StartsWith("SYSTEM|", StringComparison.Ordinal))
                {
                    AddSystemLine(msg.Substring(7));
                    return;
                }

                if (msg.StartsWith("FILE|", StringComparison.Ordinal))
                {
                    HandleFileReceived(msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                AddSystemLine($"❌ Lỗi: {ex.Message}");
            }
        }

        private void HandleFileReceived(string msg)
        {
            string[] parts = msg.Split('|');
            if (parts.Length < 4)
            {
                return;
            }

            string sender = parts[1];
            string fileName = parts[2];
            long fileSize = long.Parse(parts[3]);

            string demoId = Guid.NewGuid().ToString("N");
            AddFileBubble(sender, fileName, fileSize, isMe: false, demoFileId: demoId, showDownload: true);
            _notifyIcon.ShowBalloonTip(2000, "ZaloMini", $"📎 {sender} gửi file: {fileName}", ToolTipIcon.Info);
        }

        private void RefreshUserList()
        {
            Invoke((Action)(() =>
            {
                lstUsers.BeginUpdate();
                try
                {
                    lstUsers.Items.Clear();

                    lstUsers.Items.Add($"{_username} (Bạn)");

                    foreach (var user in _onlineUsers.Where(u => u != _username).OrderBy(u => u))
                    {
                        lstUsers.Items.Add(user);
                    }

                    foreach (var user in _offlineUsers.OrderBy(u => u))
                    {
                        lstUsers.Items.Add($"(Offline) {user}");
                    }
                }
                finally
                {
                    lstUsers.EndUpdate();
                }
            }));
        }

        private void LoadLichSu()
        {
            try
            {
                string logPath = "chatlog_" + _username + ".txt";
                if (!File.Exists(logPath))
                {
                    return;
                }

                AddBadgeSeparator("Lịch sử chat");

                string[] lines = File.ReadAllLines(logPath, System.Text.Encoding.UTF8);
                int start = Math.Max(0, lines.Length - 30);

                ResetGrouping();
                _lastDay = DateTime.MinValue;

                for (int i = start; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (TryParseLogLine(line, out DateTime ts, out string sender, out string content))
                    {
                        bool isMe = string.Equals(sender, _username, StringComparison.OrdinalIgnoreCase);
                        AddTextBubbleAt(sender, content, isMe, ts);
                    }
                }

                ScrollToBottom();
            }
            catch
            {
            }
        }

        private void LuuLichSu(string text)
        {
            try
            {
                string logPath = "chatlog_" + _username + ".txt";
                File.AppendAllText(logPath, text + "\n", System.Text.Encoding.UTF8);
            }
            catch
            {
            }
        }
        private async void SimulateReplyIfOffline(string myText)
        {
            if (_network != null && _network.IsConnected)
            {
                return;
            }

            await Task.Delay(_rnd.Next(400, 900));

            string other = _demoUsers[_rnd.Next(_demoUsers.Length)];
            if (string.Equals(other, _username, StringComparison.OrdinalIgnoreCase))
            {
                other = "Bình";
            }

            ShowTyping(other);

            await Task.Delay(_rnd.Next(900, 1500));

            HideTyping();

            string reply = "Ok 👍";
            if (!string.IsNullOrWhiteSpace(myText))
            {
                reply = "Nhận được: " + myText;
            }

            AddTextBubble(other, reply, isMe: false);

            // Demo: có phản hồi coi như đã xem
            UpdateOutgoingStatus("Đã xem");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || string.Equals(text, InputPlaceholder, StringComparison.Ordinal))
            {
                return;
            }

            string time = DateTime.Now.ToString("HH:mm");

            if (_network == null || !_network.IsConnected)
            {
                AddTextBubble(_username, text, isMe: true);
                LuuLichSu($"[{time}] {_username}: {text}");
                txtMessage.Clear();
                txtMessage.Focus();

                SimulateReplyIfOffline(text);
                return;
            }

            _network.Send($"MSG|{_username}|{text}");
            AddTextBubble(_username, text, isMe: true);
            LuuLichSu($"[{time}] {_username}: {text}");
            txtMessage.Clear();
            txtMessage.Focus();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                btnSend_Click(sender, e);
                e.SuppressKeyPress = true;
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            bool offline = _network == null || !_network.IsConnected;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn file để gửi (Tối đa 50MB)";
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string filePath = ofd.FileName;
                string fileName = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;

                if (fileSize > 50 * 1024 * 1024)
                {
                    MessageBox.Show("❌ File quá lớn! Tối đa 50MB", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (offline)
                {
                    progressBar.Visible = true;
                    progressBar.Value = 0;

                    string fileId = Guid.NewGuid().ToString("N");
                    _demoFileMap[fileId] = filePath;

                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 60;
                    timer.Tick += (s, args) =>
                    {
                        progressBar.Value = Math.Min(100, progressBar.Value + 4);
                        if (progressBar.Value >= 100)
                        {
                            timer.Stop();
                            timer.Dispose();

                            progressBar.Visible = false;
                            progressBar.Value = 0;

                            AddFileBubble(_username, fileName, fileSize, isMe: true, demoFileId: fileId, showDownload: false);

                            string other = _demoUsers[_rnd.Next(_demoUsers.Length)];
                            if (string.Equals(other, _username, StringComparison.OrdinalIgnoreCase))
                            {
                                other = "Bình";
                            }

                            AddFileBubble(other, fileName, fileSize, isMe: false, demoFileId: fileId, showDownload: true);
                            _notifyIcon.ShowBalloonTip(1500, "ZaloMini", $"📎 {other} gửi file: {fileName}", ToolTipIcon.Info);
                        }
                    };

                    timer.Start();
                    return;
                }

                AddSystemLine($"📎 Bạn đang gửi: {fileName} ({fileSize / 1024} KB)");
                _network.Send($"FILE|{_username}|{fileName}|{fileSize}");

                Thread t = new Thread(() =>
                {
                    try
                    {
                        byte[] buffer = new byte[65536];
                        long totalSent = 0;

                        using (var fs = new FileStream(filePath, FileMode.Open))
                        {
                            int bytesRead;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                _network.SendBytes(buffer, bytesRead);
                                totalSent += bytesRead;
                                int pct = (int)(totalSent * 100 / fileSize);

                                Invoke((Action)(() =>
                                {
                                    progressBar.Visible = true;
                                    progressBar.Value = Math.Min(pct, 100);
                                }));
                            }
                        }

                        Invoke((Action)(() =>
                        {
                            AddSystemLine($"✅ Gửi {fileName} thành công!");
                            progressBar.Visible = false;
                            progressBar.Value = 0;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Invoke((Action)(() =>
                        {
                            AddSystemLine($"❌ Lỗi gửi: {ex.Message}");
                            progressBar.Visible = false;
                        }));
                    }
                });

                t.IsBackground = true;
                t.Start();
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            FormSettings settings = new FormSettings();
            settings.ShowDialog();
        }

        private void ScrollToBottom()
        {
            if (_messageFlow == null || _messageFlow.Controls.Count == 0)
            {
                return;
            }

            BeginInvoke((Action)(() =>
            {
                try
                {
                    _messageFlow.SuspendLayout();
                    _messageFlow.PerformLayout();

                    Control last = _messageFlow.Controls[_messageFlow.Controls.Count - 1];
                    _messageFlow.ScrollControlIntoView(last);
                }
                finally
                {
                    _messageFlow.ResumeLayout();
                }
            }));
        }

        private void SetOutgoingStatus(string text)
        {
            if (_messageFlow == null)
            {
                return;
            }

            if (_lastStatusHost != null)
            {
                _messageFlow.Controls.Remove(_lastStatusHost);
                _lastStatusHost.Dispose();
                _lastStatusHost = null;
                _lastStatusLabel = null;
            }

            _lastStatusHost = new Panel();
            _lastStatusHost.BackColor = Color.Transparent;
            _lastStatusHost.Height = 18;
            _lastStatusHost.Margin = new Padding(0, 0, 0, 6);
            _lastStatusHost.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);

            _lastStatusLabel = new Label();
            _lastStatusLabel.Dock = DockStyle.Fill;
            _lastStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            _lastStatusLabel.ForeColor = UiTheme.MutedTextColor;
            _lastStatusLabel.Font = new Font(UiTheme.BaseFont.FontFamily, 8.25F, FontStyle.Regular);
            _lastStatusLabel.Text = text ?? string.Empty;

            _lastStatusHost.Controls.Add(_lastStatusLabel);
            _messageFlow.Controls.Add(_lastStatusHost);

            ResizeMessageItems();
            ScrollToBottom();
        }

        private void UpdateOutgoingStatus(string text)
        {
            if (_lastStatusLabel == null)
            {
                return;
            }

            _lastStatusLabel.Text = text ?? string.Empty;
        }

        private void ShowTyping(string otherUser)
        {
            if (_messageFlow == null || _typingItem != null)
            {
                return;
            }

            _typingItem = new ChatMessageItemControl();
            _typingItem.Width = Math.Max(220, _messageFlow.ClientSize.Width - 25);
            _typingItem.Margin = new Padding(0, 2, 0, 6);
            _typingItem.SetText("", (otherUser ?? "Ai đó") + " đang nhập...", isMe: false, showMeta: false);

            _messageFlow.Controls.Add(_typingItem);

            // Nếu đang có status ở cuối, đảm bảo typing nằm trước status
            if (_lastStatusHost != null)
            {
                _messageFlow.Controls.SetChildIndex(_typingItem, _messageFlow.Controls.GetChildIndex(_lastStatusHost));
            }

            ResizeMessageItems();
            ScrollToBottom();
        }

        private void HideTyping()
        {
            if (_messageFlow == null || _typingItem == null)
            {
                return;
            }

            _messageFlow.Controls.Remove(_typingItem);
            _typingItem.Dispose();
            _typingItem = null;
        }
    }
}