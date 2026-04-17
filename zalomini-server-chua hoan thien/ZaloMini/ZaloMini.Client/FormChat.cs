using System;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Text;
using ZaloMini.Client.Models;

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

        // network packet handler and disconnect handler
        private Action<Packet> _onPacketReceivedHandler;
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
                // create a packet-based handler that marshals to UI thread
                _onPacketReceivedHandler = pkt =>
                {
                    if (IsDisposed || Disposing) return;
                    BeginInvoke((Action)(() => HandlePacket(pkt)));
                };

                _onDisconnectedHandler = () =>
                {
                    if (IsDisposed || Disposing) return;
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

                // subscribe to the new packet event
                _network.OnPacketReceived += _onPacketReceivedHandler;
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
                // use packet-based ONLINE notification if server expects it
                var p = new Packet { Type = PacketType.Status, Payload = "ONLINE|" + _username };
                // fire-and-forget
                _ = _network.SendPacketAsync(p);
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
                    if (_onPacketReceivedHandler != null)
                    {
                        _network.OnPacketReceived -= _onPacketReceivedHandler;
                        _onPacketReceivedHandler = null;
                    }

                    if (_onDisconnectedHandler != null)
                    {
                        _network.OnDisconnected -= _onDisconnectedHandler;
                        _onDisconnectedHandler = null;
                    }

                    if (_network.IsConnected)
                    {
                        var p = new Packet { Type = PacketType.Status, Payload = "OFFLINE|" + _username };
                        _ = _network.SendPacketAsync(p);
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

        // New packet-based handler
        private void HandlePacket(Packet packet)
        {
            if (packet == null) return;

            try
            {
                switch (packet.Type)
                {
                    case PacketType.Message:
                        {
                            if (string.IsNullOrEmpty(packet.Payload)) break;
                            var ser = new DataContractJsonSerializer(typeof(MessageDTO));
                            MessageDTO msg = null;
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(packet.Payload)))
                            {
                                msg = ser.ReadObject(ms) as MessageDTO;
                            }

                            if (msg != null)
                            {
                                bool isMe = string.Equals(msg.SenderId, _username, StringComparison.OrdinalIgnoreCase);
                                AddTextBubble(msg.SenderId, msg.Content, isMe);
                                LuuLichSu($"[{msg.Timestamp:HH:mm}] {msg.SenderId}: {msg.Content}");

                                if (!ContainsFocus && !isMe)
                                {
                                    _notifyIcon.ShowBalloonTip(2000, msg.SenderId, msg.Content, ToolTipIcon.Info);
                                }
                            }
                            break;
                        }

                    case PacketType.Status:
                        {
                            // support both JSON array and legacy string commands in payload
                            if (!string.IsNullOrEmpty(packet.Payload))
                            {
                                bool handled = false;
                                try
                                {
                                    var arrSer = new DataContractJsonSerializer(typeof(string[]));
                                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(packet.Payload)))
                                    {
                                        var list = arrSer.ReadObject(ms) as string[];
                                        if (list != null)
                                        {
                                            _onlineUsers.Clear();
                                            foreach (var u in list) if (!string.IsNullOrWhiteSpace(u)) _onlineUsers.Add(u);
                                            RefreshUserList();
                                            ResetGrouping();
                                            handled = true;
                                        }
                                    }
                                }
                                catch { handled = false; }

                                if (!handled)
                                {
                                    var payload = packet.Payload;
                                    if (payload.StartsWith("ONLINE|", StringComparison.Ordinal))
                                    {
                                        string user = payload.Substring("ONLINE|".Length).Trim();
                                        if (!_onlineUsers.Contains(user) && user != _username)
                                        {
                                            _onlineUsers.Add(user);
                                            _offlineUsers.Remove(user);
                                            RefreshUserList();
                                            AddSystemLine($"✅ {user} đã tham gia");
                                            _notifyIcon.ShowBalloonTip(1500, "ZaloMini", $"✅ {user} vừa online!", ToolTipIcon.Info);
                                        }
                                    }
                                    else if (payload.StartsWith("OFFLINE|", StringComparison.Ordinal))
                                    {
                                        string user = payload.Substring("OFFLINE|".Length).Trim();
                                        if (_onlineUsers.Contains(user))
                                        {
                                            _onlineUsers.Remove(user);
                                            _offlineUsers.Add(user);
                                            RefreshUserList();
                                            AddSystemLine($"❌ {user} đã rời");
                                        }
                                    }
                                    else if (payload.StartsWith("USERLIST|", StringComparison.Ordinal))
                                    {
                                        string userList = payload.Substring("USERLIST|".Length);
                                        string[] users = userList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        _onlineUsers.Clear();
                                        foreach (var u in users) { var t = u.Trim(); if (!string.IsNullOrEmpty(t)) _onlineUsers.Add(t); }
                                        RefreshUserList();
                                        ResetGrouping();
                                    }
                                }
                            }
                            break;
                        }

                    case PacketType.Login:
                    case PacketType.Logout:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                AddSystemLine($"❌ Packet handler error: {ex.Message}");
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

        private async void btnSend_Click(object sender, EventArgs e)
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
                return;
            }

            var msgDto = new MessageDTO
            {
                SenderId = _username,
                ReceiverId = string.Empty, // TODO: set selected recipient if UI supports it
                Content = text,
                Timestamp = DateTime.Now
            };

            string payload;
            var ser = new DataContractJsonSerializer(typeof(MessageDTO));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, msgDto);
                payload = Encoding.UTF8.GetString(ms.ToArray());
            }

            var packet = new Packet { Type = PacketType.Message, Payload = payload };

            try
            {
                await _network.SendPacketAsync(packet).ConfigureAwait(false);

                BeginInvoke((Action)(() =>
                {
                    AddTextBubble(_username, text, isMe: true);
                    LuuLichSu($"[{time}] {_username}: {text}");
                    txtMessage.Clear();
                    txtMessage.Focus();
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke((Action)(() => AddSystemLine($"❌ Lỗi gửi: {ex.Message}")));
            }
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

                //if (offline)
                //{
                //    progressBar.Visible = true;
                //    progressBar.Value = 0;

                //    string fileId = Guid.NewGuid().ToString("N");
                //    _demoFileMap[fileId] = filePath;

                //    var timer = new System.Windows.Forms.Timer();
                //    timer.Interval = 60;
                //    timer.Tick += (s, args) =>
                //    {
                //        progressBar.Value = Math.Min(100, progressBar.Value + 4);
                //        if (progressBar.Value >= 100)
                //        {
                //            timer.Stop();
                //            timer.Dispose();

                //            progressBar.Visible = false;
                //            progressBar.Value = 0;

                //            AddFileBubble(_username, fileName, fileSize, isMe: true, demoFileId: fileId, showDownload: false);

                //            string other = _demoUsers[_rnd.Next(_demoUsers.Length)];
                //            if (string.Equals(other, _username, StringComparison.OrdinalIgnoreCase))
                //            {
                //                other = "Bình";
                //            }

                //            AddFileBubble(other, fileName, fileSize, isMe: false, demoFileId: fileId, showDownload: true);
                //            _notifyIcon.ShowBalloonTip(1500, "ZaloMini", $"📎 {other} gửi file: {fileName}", ToolTipIcon.Info);
                //        }
                //    };

                //    timer.Start();
                //    return;
                //}

                AddSystemLine($"📎 Bạn đang gửi: {fileName} ({fileSize / 1024} KB)");

                // Prepare a file id and send a metadata packet first
                string fileId = Guid.NewGuid().ToString("N");
                var metaDto = new MessageDTO
                {
                    SenderId = _username,
                    ReceiverId = string.Empty, // TODO: set recipient if supported
                    Content = $"FILE_META|{fileId}|{fileName}|{fileSize}",
                    Timestamp = DateTime.UtcNow
                };

                // serialize meta DTO
                string metaPayload;
                var metaSer = new DataContractJsonSerializer(typeof(MessageDTO));
                using (var msMeta = new MemoryStream())
                {
                    metaSer.WriteObject(msMeta, metaDto);
                    metaPayload = Encoding.UTF8.GetString(msMeta.ToArray());
                }

                // send meta packet
                var metaPacket = new Packet { Type = PacketType.Message, Payload = metaPayload };
                try
                {
                    // fire-and-forget meta send; perform synchronously inside thread below as well
                    _network.SendPacketAsync(metaPacket).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    AddSystemLine($"❌ Lỗi gửi metadata file: {ex.Message}");
                    return;
                }

                Thread t = new Thread(() =>
                {
                    try
                    {
                        byte[] buffer = new byte[65536];
                        long totalSent = 0;

                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            int bytesRead;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                // copy exact bytes
                                byte[] chunk = new byte[bytesRead];
                                Array.Copy(buffer, 0, chunk, 0, bytesRead);

                                // base64-encode chunk and wrap into MessageDTO content with fileId
                                string base64 = Convert.ToBase64String(chunk);
                                var chunkDto = new MessageDTO
                                {
                                    SenderId = _username,
                                    ReceiverId = string.Empty, // TODO: set recipient if supported
                                    Content = $"FILE_CHUNK|{fileId}|{base64}",
                                    Timestamp = DateTime.UtcNow
                                };

                                // serialize chunk DTO
                                string payload;
                                var ser = new DataContractJsonSerializer(typeof(MessageDTO));
                                using (var ms = new MemoryStream())
                                {
                                    ser.WriteObject(ms, chunkDto);
                                    payload = Encoding.UTF8.GetString(ms.ToArray());
                                }

                                var packet = new Packet { Type = PacketType.Message, Payload = payload };

                                // send packet (synchronously wait inside this background thread)
                                _network.SendPacketAsync(packet).GetAwaiter().GetResult();

                                totalSent += bytesRead;
                                int pct = (int)(totalSent * 100 / fileSize);

                                Invoke((Action)(() =>
                                {
                                    progressBar.Visible = true;
                                    progressBar.Value = Math.Min(pct, 100);
                                }));
                            }
                        }

                        // send a final packet to indicate upload finished
                        var endDto = new MessageDTO
                        {
                            SenderId = _username,
                            ReceiverId = string.Empty,
                            Content = $"FILE_END|{fileId}|{fileName}|{fileSize}",
                            Timestamp = DateTime.UtcNow
                        };

                        string endPayload;
                        var endSer = new DataContractJsonSerializer(typeof(MessageDTO));
                        using (var msEnd = new MemoryStream())
                        {
                            endSer.WriteObject(msEnd, endDto);
                            endPayload = Encoding.UTF8.GetString(msEnd.ToArray());
                        }

                        var endPacket = new Packet { Type = PacketType.Message, Payload = endPayload };
                        _network.SendPacketAsync(endPacket).GetAwaiter().GetResult();

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

        private void rtbMessages_TextChanged(object sender, EventArgs e)
        {

        }
    }
}