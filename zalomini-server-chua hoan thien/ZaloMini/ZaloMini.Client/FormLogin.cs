using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using ZaloMini.Client.Models;

namespace ZaloMini.Client
{
    public partial class FormLogin : Form
    {
        private string _loggedInUsername = "";

        // Network client injected (optional for designer compatibility)
        private readonly NetworkClient _network;

        public string LoggedInUsername => _loggedInUsername;

        private GradientPanel _bg;
        private RoundedPanel _card;
        private Label _lblHeroTitle;
        private Label _lblHeroSubtitle;

        private Button _btnSwitchLogin;
        private Button _btnSwitchRegister;

        private RoundedPanel _inpLoginUser;
        private RoundedPanel _inpLoginPass;
        private RoundedPanel _inpRegUser;
        private RoundedPanel _inpRegPass;
        private RoundedPanel _inpRegConfirm;

        private Button _btnToggleLoginPass;
        private Button _btnToggleRegPass;
        private Button _btnToggleRegConfirm;

        private GradientButton _btnLoginModern;
        private GradientButton _btnRegisterModern;

        private bool _loginInProgress; // Biến đánh dấu trạng thái đang đăng nhập

        private static Rectangle? _lastLoginBounds;
        private static FormWindowState _lastLoginWindowState = FormWindowState.Normal;

        private static readonly Size DefaultWindowSize = new Size(1100, 760);
        private static readonly Size DefaultMinWindowSize = new Size(900, 650);
        private static readonly Size DefaultCardSize = new Size(560, 660);

        // Constructor - keep default parameter to remain designer-friendly
        public FormLogin(NetworkClient network = null)
        {
            InitializeComponent();

            _network = network;

            // Cho phép resize bình thường (không khóa)
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = DefaultMinWindowSize;

            // Không ép ClientSize cố định ở đây nữa (để bạn resize theo ý)
            // ClientSize = new Size(900, 650);

            ApplyTheme();
            ApplyModernUi();

            Resize += (s, e) => LayoutModernUi();

            // Điều hướng Enter giống ý bạn (xuống field tiếp theo, rồi xuống nút)
            txtUsername.KeyDown += TxtUsername_KeyDown;
            txtPassword.KeyDown += TxtPassword_KeyDown;

            txtRegUsername.KeyDown += TxtRegUsername_KeyDown;
            txtRegPassword.KeyDown += TxtRegPassword_KeyDown;
            txtRegConfirm.KeyDown += TxtRegConfirm_KeyDown;

            tabControl1.SelectedIndexChanged += (s, e) =>
            {
                UpdateHeroForTab();
                UpdateAcceptButtonForTab();
                UpdateSwitchButtons();
            };

            UpdateHeroForTab();
            UpdateAcceptButtonForTab();
            UpdateSwitchButtons();

            txtUsername.Focus();
            LayoutModernUi();

            Shown += FormLogin_Shown;
            FormClosing += FormLogin_FormClosing;
        }

        private void ApplyTheme()
        {
            UiTheme.ApplyFormBase(this);
            UiTheme.ApplyPrimaryButton(btnLogin);
            UiTheme.ApplyPrimaryButton(btnRegister);
        }

        private void ApplyModernUi()
        {
            SuspendLayout();

            // Ẩn header cũ (giữ lại để không đụng Designer)
            panelHeader.Visible = false;

            // Nền gradient
            _bg = new GradientPanel();
            _bg.Dock = DockStyle.Fill;
            _bg.Color1 = Color.FromArgb(13, 16, 24);
            _bg.Color2 = Color.FromArgb(0, 104, 255); // Zalo blue
            _bg.Angle = 135F;
            Controls.Add(_bg);
            _bg.BringToFront();

            // Card
            _card = new RoundedPanel();
            _card.CornerRadius = 24;
            _card.FillColor = Color.FromArgb(28, 30, 38);
            _card.BorderColor = Color.FromArgb(60, 255, 255, 255);
            _card.BorderThickness = 1;
            _card.Padding = new Padding(26, 22, 26, 22);
            _card.Size = new Size(420, 520);
            _bg.Controls.Add(_card);

            // Tiêu đề (theo tab)
            _lblHeroTitle = new Label();
            _lblHeroTitle.AutoSize = false;
            _lblHeroTitle.Height = 44;
            _lblHeroTitle.Dock = DockStyle.Top;
            _lblHeroTitle.Font = new Font(UiTheme.BaseFont.FontFamily, 22F, FontStyle.Bold);
            _lblHeroTitle.ForeColor = Color.White;
            _lblHeroTitle.TextAlign = ContentAlignment.MiddleLeft;

            _lblHeroSubtitle = new Label();
            _lblHeroSubtitle.AutoSize = false;
            _lblHeroSubtitle.Height = 22;
            _lblHeroSubtitle.Dock = DockStyle.Top;
            _lblHeroSubtitle.Font = new Font(UiTheme.BaseFont.FontFamily, 10F, FontStyle.Regular);
            _lblHeroSubtitle.ForeColor = Color.FromArgb(170, 255, 255, 255);
            _lblHeroSubtitle.TextAlign = ContentAlignment.MiddleLeft;

            // Switch Login/Register
            var switchHost = new RoundedPanel();
            switchHost.CornerRadius = 14;
            switchHost.FillColor = Color.FromArgb(20, 22, 30);
            switchHost.BorderColor = Color.FromArgb(40, 255, 255, 255);
            switchHost.BorderThickness = 1;
            switchHost.Height = 40;
            switchHost.Dock = DockStyle.Top;
            switchHost.Padding = new Padding(4);
            switchHost.Margin = new Padding(0, 16, 0, 14);

            _btnSwitchLogin = CreateSwitchButton("Đăng nhập");
            _btnSwitchRegister = CreateSwitchButton("Đăng ký");

            _btnSwitchLogin.Click += (s, e) => tabControl1.SelectedTab = tabPage1;
            _btnSwitchRegister.Click += (s, e) => tabControl1.SelectedTab = tabPage2;

            switchHost.Controls.Add(_btnSwitchRegister);
            switchHost.Controls.Add(_btnSwitchLogin);

            // TabControl (Ẩn header tab)
            // Replace TabControl mặc định bằng bản không viền để xóa khung trắng
            var oldTabs = tabControl1;
            var newTabs = new BorderlessTabControl();
            newTabs.Name = oldTabs.Name;
            newTabs.TabIndex = oldTabs.TabIndex;
            newTabs.Font = oldTabs.Font;

            newTabs.Controls.Add(tabPage1);
            newTabs.Controls.Add(tabPage2);

            // Gỡ control cũ khỏi parent (nếu có)
            if (oldTabs.Parent != null)
            {
                Control p = oldTabs.Parent;
                int idx = p.Controls.GetChildIndex(oldTabs);
                p.Controls.Remove(oldTabs);
                p.Controls.Add(newTabs);
                p.Controls.SetChildIndex(newTabs, idx);
            }

            oldTabs.Dispose();
            tabControl1 = newTabs;

            tabControl1.Parent = _card;
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.Padding = new Point(0, 0);
            tabControl1.Margin = new Padding(0);

            tabPage1.BackColor = _card.FillColor;
            tabPage2.BackColor = _card.FillColor;

            // Ẩn nút cũ trong tab (thay bằng button gradient)
            btnLogin.Visible = false;
            btnRegister.Visible = false;

            // Xây layout mới trong từng tab
            BuildLoginTab();
            BuildRegisterTab();

            _card.Controls.Add(tabControl1);
            _card.Controls.Add(switchHost);
            _card.Controls.Add(_lblHeroSubtitle);
            _card.Controls.Add(_lblHeroTitle);

            ResumeLayout();
        }

        private Button CreateSwitchButton(string text)
        {
            var b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Height = 36;
            b.Width = 180;
            b.Cursor = Cursors.Hand;
            b.Font = new Font(UiTheme.BaseFont.FontFamily, 10F, FontStyle.Bold);
            b.BackColor = Color.Transparent;
            b.ForeColor = Color.FromArgb(220, 255, 255, 255);

            // Bo tròn giống minh họa
            ApplyRoundedRegion(b, 16);

            return b;
        }

        private static void TogglePasswordVisibility(TextBox textBox, Button toggleButton)
        {
            if (textBox == null || toggleButton == null)
            {
                return;
            }

            int selStart = textBox.SelectionStart;
            int selLen = textBox.SelectionLength;

            // Nếu PasswordChar đã set trong Designer, vẫn sẽ che dù UseSystemPasswordChar=false
            bool isMasked = textBox.UseSystemPasswordChar || textBox.PasswordChar != '\0';

            bool show = isMasked; // đang che -> chuyển sang hiện
            textBox.UseSystemPasswordChar = !show;

            // Luôn xóa PasswordChar để đảm bảo "Hiện" thật sự hiện
            textBox.PasswordChar = '\0';

            toggleButton.Text = show ? "Ẩn" : "Hiện";

            // Giữ lại caret/selection
            textBox.SelectionStart = Math.Min(selStart, textBox.TextLength);
            textBox.SelectionLength = Math.Min(selLen, Math.Max(0, textBox.TextLength - textBox.SelectionStart));
            textBox.Focus();
        }

        private RoundedPanel CreateInputHost(TextBox tb, bool isPassword, out Button toggle)
        {
            var host = new RoundedPanel();
            host.Dock = DockStyle.Fill;
            host.Margin = new Padding(0);
            host.CornerRadius = 18;
            host.FillColor = Color.FromArgb(18, 20, 28);
            host.BorderColor = Color.FromArgb(55, 255, 255, 255);
            host.BorderThickness = 1;

            // Giảm padding trên/dưới để ô thấp hơn + dễ canh giữa chữ
            host.Padding = new Padding(14, 8, 14, 8);

            tb.Parent = host;
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = host.FillColor;
            tb.ForeColor = Color.White;
            tb.Font = new Font(UiTheme.BaseFont.FontFamily, 11F, FontStyle.Regular);
            tb.Multiline = false; // giữ single-line để chữ sắc nét
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            tb.AutoSize = true;
            Button toggleButton = null;

            if (isPassword)
            {
                // Ép bỏ PasswordChar từ Designer để toggle "Hiện" hoạt động đúng
                tb.PasswordChar = '\0';
                tb.UseSystemPasswordChar = true;

                toggleButton = new Button();
                toggleButton.FlatStyle = FlatStyle.Flat;
                toggleButton.FlatAppearance.BorderSize = 0;
                toggleButton.Text = "Hiện";
                toggleButton.ForeColor = Color.FromArgb(200, 255, 255, 255);
                toggleButton.BackColor = host.FillColor;
                toggleButton.Cursor = Cursors.Hand;
                toggleButton.Width = 56;
                toggleButton.Dock = DockStyle.Right;
                toggleButton.Font = new Font(UiTheme.BaseFont.FontFamily, 9F, FontStyle.Bold);
                toggleButton.TextAlign = ContentAlignment.MiddleCenter;

                host.Controls.Add(toggleButton);
            }

            host.Controls.Add(tb);

            void LayoutTextBox()
            {
                int left = host.Padding.Left;
                int right = host.Padding.Right;

                int toggleW = toggleButton != null && toggleButton.Visible ? toggleButton.Width : 0;
                int gap = toggleW > 0 ? 6 : 0;

                int w = host.ClientSize.Width - left - right - toggleW - gap;
                tb.Width = Math.Max(10, w);

                int h = tb.PreferredHeight;
                int y = (host.ClientSize.Height - h) / 2;

                // Hạ xuống nhẹ 1px cho “vừa mắt”
                y += 1;

                tb.Location = new Point(left, Math.Max(0, y));
            }

            host.SizeChanged += (s, e) => LayoutTextBox();
            LayoutTextBox();

            toggle = toggleButton;
            return host;
        }

        private Label CreateCaption(string text)
        {
            var l = new Label();
            l.Dock = DockStyle.Fill;
            l.Margin = new Padding(0);
            l.AutoSize = false;
            l.Height = 18;
            l.Text = text;
            l.ForeColor = Color.FromArgb(190, 255, 255, 255);
            l.Font = new Font(UiTheme.BaseFont.FontFamily, 9.5F, FontStyle.Regular);
            l.TextAlign = ContentAlignment.BottomLeft;
            return l;
        }

        private void BuildLoginTab()
        {
            // Ẩn label cũ trong designer
            label1.Visible = false;
            label2.Visible = false;

            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;

            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // button

            _inpLoginUser = CreateInputHost(txtUsername, isPassword: false, out _);
            _inpLoginPass = CreateInputHost(txtPassword, isPassword: true, out _btnToggleLoginPass);

            _btnToggleLoginPass.Click += (s, e) => TogglePasswordVisibility(txtPassword, _btnToggleLoginPass);

            _btnLoginModern = new GradientButton();
            _btnLoginModern.Text = "Đăng nhập";
            _btnLoginModern.Dock = DockStyle.Fill;
            _btnLoginModern.Click += (s, e) => btnLogin_Click(s, e);

            layout.Controls.Add(CreateCaption("Tên đăng nhập"), 0, 0);
            layout.Controls.Add(_inpLoginUser, 0, 1);
            layout.Controls.Add(new Panel { Height = 14, Dock = DockStyle.Fill }, 0, 2);
            layout.Controls.Add(CreateCaption("Mật khẩu"), 0, 3);
            layout.Controls.Add(_inpLoginPass, 0, 4);
            layout.Controls.Add(new Panel { Height = 18, Dock = DockStyle.Fill }, 0, 5);
            layout.Controls.Add(_btnLoginModern, 0, 6);

            tabPage1.Controls.Clear();
            tabPage1.Controls.Add(WrapTabContentCentered(layout));
        }

        private void BuildRegisterTab()
        {
            // Ẩn label cũ trong designer
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;

            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.ColumnCount = 1;
            layout.RowCount = 10;
            layout.Padding = new Padding(0, 6, 0, 0);
            layout.Margin = new Padding(0);
            layout.BackColor = tabPage2.BackColor;

            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption confirm
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input confirm
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // button

            _inpRegUser = CreateInputHost(txtRegUsername, isPassword: false, out _);
            _inpRegPass = CreateInputHost(txtRegPassword, isPassword: true, out _btnToggleRegPass);
            _inpRegConfirm = CreateInputHost(txtRegConfirm, isPassword: true, out _btnToggleRegConfirm);

            _btnToggleRegPass.Click += (s, e) => TogglePasswordVisibility(txtRegPassword, _btnToggleRegPass);
            _btnToggleRegConfirm.Click += (s, e) => TogglePasswordVisibility(txtRegConfirm, _btnToggleRegConfirm);

            _btnRegisterModern = new GradientButton();
            _btnRegisterModern.Text = "Đăng ký";
            _btnRegisterModern.Dock = DockStyle.Fill;
            _btnRegisterModern.Click += (s, e) => btnRegister_Click(s, e);

            layout.Controls.Add(CreateCaption("Tên đăng nhập"), 0, 0);
            layout.Controls.Add(_inpRegUser, 0, 1);
            layout.Controls.Add(new Panel { Height = 12, Dock = DockStyle.Fill }, 0, 2);

            layout.Controls.Add(CreateCaption("Mật khẩu"), 0, 3);
            layout.Controls.Add(_inpRegPass, 0, 4);
            layout.Controls.Add(new Panel { Height = 12, Dock = DockStyle.Fill }, 0, 5);

            layout.Controls.Add(CreateCaption("Nhập lại mật khẩu"), 0, 6);
            layout.Controls.Add(_inpRegConfirm, 0, 7);
            layout.Controls.Add(new Panel { Height = 16, Dock = DockStyle.Fill }, 0, 8);

            layout.Controls.Add(_btnRegisterModern, 0, 9);

            tabPage2.Controls.Clear();
            tabPage2.Controls.Add(WrapTabContentCentered(layout));
        }

        private void LayoutModernUi()
        {
            if (_bg == null || _card == null)
            {
                return;
            }

            // Giữ card cố định (không co giãn) để luôn giống ảnh
            _card.Size = DefaultCardSize;
            _card.Left = (ClientSize.Width - _card.Width) / 2;
            _card.Top = (ClientSize.Height - _card.Height) / 2;

            // Card scale theo cửa sổ, nhưng có min/max để luôn cân đối
            int outerPad = 40; // khoảng trống quanh card
            int maxW = Math.Max(320, ClientSize.Width - outerPad * 2);
            int maxH = Math.Max(420, ClientSize.Height - outerPad * 2);

            // Scale theo tỉ lệ cửa sổ (cân đối), clamp để không quá to/nhỏ
            int cardW = (int)(ClientSize.Width * 0.55f);
            int cardH = (int)(ClientSize.Height * 0.85f);

            cardW = Math.Max(480, Math.Min(cardW, 680));
            cardH = Math.Max(560, Math.Min(cardH, 760));

            cardW = Math.Min(cardW, maxW);
            cardH = Math.Min(cardH, maxH);

            _card.Size = new Size(cardW, cardH);
            _card.Left = (ClientSize.Width - _card.Width) / 2;
            _card.Top = (ClientSize.Height - _card.Height) / 2;

            // Chia đều 2 nút switch + canh giữa theo chiều dọc trong switchHost
            if (_btnSwitchLogin != null && _btnSwitchRegister != null)
            {
                Control switchHost = _btnSwitchLogin.Parent;
                if (switchHost != null)
                {
                    int px = 4;
                    int w = (switchHost.ClientSize.Width - px * 2) / 2;
                    int top = (switchHost.ClientSize.Height - _btnSwitchLogin.Height) / 2;

                    _btnSwitchLogin.Width = w;
                    _btnSwitchRegister.Width = w;

                    _btnSwitchLogin.Left = px;
                    _btnSwitchRegister.Left = px + w;

                    _btnSwitchLogin.Top = top;
                    _btnSwitchRegister.Top = top;
                }
            }

            _bg?.Invalidate(true);
        }

        private void UpdateHeroForTab()
        {
            if (_lblHeroTitle == null || _lblHeroSubtitle == null)
            {
                return;
            }

            if (tabControl1.SelectedTab == tabPage2)
            {
                _lblHeroTitle.Text = "Bắt đầu miễn phí";
                _lblHeroSubtitle.Text = "Miễn phí mãi mãi. Không cần thẻ.";
            }
            else
            {
                _lblHeroTitle.Text = "Chào mừng trở lại!";
                _lblHeroSubtitle.Text = "Rất vui được gặp lại bạn.";
            }
        }

        private void UpdateSwitchButtons()
        {
            bool isRegister = tabControl1.SelectedTab == tabPage2;

            SetSwitchStyle(_btnSwitchLogin, active: !isRegister);
            SetSwitchStyle(_btnSwitchRegister, active: isRegister);
        }

        private static void SetSwitchStyle(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = active ? Color.FromArgb(0, 104, 255) : Color.Transparent;
            button.ForeColor = active ? Color.White : Color.FromArgb(220, 255, 255, 255);
        }

        private void UpdateAcceptButtonForTab()
        {
            // Vì Enter đang dùng để điều hướng xuống field/nút, không dùng AcceptButton
            AcceptButton = null;
        }

        // Updated login handler: perform network handshake and wait for server reply
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            // Prevent re-entrancy
            // (if you previously had _loginInProgress boolean, reuse it; otherwise local)
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_network == null || !_network.IsConnected)
            {
                MessageBox.Show("Chưa kết nối tới Server. Vui lòng kiểm tra cài đặt và thử lại.",
                    "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Build LoginRequest
            var loginReq = new LoginRequest { Username = username, Password = password };

            string payload;
            var ser = new DataContractJsonSerializer(typeof(LoginRequest));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, loginReq);
                payload = Encoding.UTF8.GetString(ms.ToArray());
            }

            var packet = new Packet
            {
                Type = PacketType.Login,
                Payload = payload,
                RequestId = Guid.NewGuid().ToString("N")
            };

            var tcs = new TaskCompletionSource<LoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            Action<Packet> handler = null;
            handler = (p) =>
            {
                try
                {
                    if (p == null) return;
                    if (p.Type != PacketType.Login) return;

                    // deserialize LoginResult
                    var resSer = new DataContractJsonSerializer(typeof(LoginResult));
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(p.Payload ?? string.Empty)))
                    {
                        var loginRes = resSer.ReadObject(ms) as LoginResult;
                        if (loginRes != null)
                        {
                            tcs.TrySetResult(loginRes);
                        }
                        else
                        {
                            tcs.TrySetException(new Exception("Invalid LoginResult from server"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            _network.OnPacketReceived += handler;

            try
            {
                // Send login packet
                await _network.SendPacketAsync(packet).ConfigureAwait(false);

                // Wait for response or timeout (8s)
                var delayTask = Task.Delay(TimeSpan.FromSeconds(8));
                var completed = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);

                if (completed == delayTask)
                {
                    BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show("Timeout khi chờ phản hồi từ Server.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                var loginResult = await tcs.Task.ConfigureAwait(false);

                if (loginResult == null)
                {
                    BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show("Phản hồi đăng nhập không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                if (!loginResult.IsSuccess)
                {
                    BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show("Đăng nhập thất bại: " + (loginResult.ErrorMessage ?? "Unknown"), "Đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                    return;
                }

                // Success
                _loggedInUsername = username;

                BeginInvoke((Action)(() =>
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke((Action)(() =>
                {
                    MessageBox.Show("Lỗi khi đăng nhập: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
            finally
            {
                // Unsubscribe handler
                _network.OnPacketReceived -= handler;
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtRegUsername.Text.Trim();
            string password = txtRegPassword.Text;
            string confirm = txtRegConfirm.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("Mật khẩu không trùng khớp!",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_network == null || !_network.IsConnected)
            {
                MessageBox.Show("Chưa kết nối tới Server. Vui lòng kiểm tra cài đặt và thử lại.",
                    "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If you implement server-side registration, send a registration packet here.
            MessageBox.Show("Đăng ký trên client đã bị vô hiệu hoá. Vui lòng đăng ký trên Server hoặc liên hệ quản trị viên.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

            tabControl1.SelectedTab = tabPage1;
            txtRegUsername.Clear();
            txtRegPassword.Clear();
            txtRegConfirm.Clear();

            txtUsername.Focus();
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtPassword.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_btnLoginModern != null)
                {
                    _btnLoginModern.Focus();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtRegUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtRegPassword.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtRegPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtRegConfirm.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtRegConfirm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_btnRegisterModern != null)
                {
                    _btnRegisterModern.Focus();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private static void ApplyRoundedRegion(Control control, int cornerRadius)
        {
            if (control == null)
            {
                return;
            }

            void Apply()
            {
                if (control.Width <= 0 || control.Height <= 0)
                {
                    return;
                }

                int r = Math.Max(8, cornerRadius);
                int d = r * 2;

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    var rect = new Rectangle(0, 0, control.Width, control.Height);

                    path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                    path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                    path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                    path.CloseFigure();

                    control.Region = new Region(path);
                }
            }

            control.SizeChanged += (s, e) => Apply();
            Apply();
        }

        private Control WrapTabContentCentered(Control content)
        {
            if (content == null)
            {
                return new Panel { Dock = DockStyle.Fill };
            }

            content.Dock = DockStyle.Top;
            content.Margin = new Padding(0);

            var host = new TableLayoutPanel();
            host.Dock = DockStyle.Fill;
            host.Margin = new Padding(0);
            host.Padding = new Padding(0);
            host.BackColor = Color.Transparent;

            host.ColumnCount = 1;
            host.RowCount = 3;

            host.ColumnStyles.Clear();
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            host.RowStyles.Clear();
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            host.Controls.Add(content, 0, 1);

            return host;
        }

        private void FormLogin_Shown(object sender, EventArgs e)
        {
            // Cần Manual để tự đặt Location chính xác
            StartPosition = FormStartPosition.Manual;

            // Đưa về Normal trước để set Size/Location không bị sai
            if (WindowState != FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
            }

            // Giữ kích thước lần trước (nếu có), nhưng KHÔNG giữ vị trí
            if (_lastLoginBounds.HasValue)
            {
                Size = _lastLoginBounds.Value.Size;
            }
            else
            {
                Size = DefaultWindowSize;
            }

            // Luôn center khi hiện form (replaced missing CenterOnCurrentScreen())
            CenterOnCurrentScreen();

            // Khôi phục trạng thái lần trước (nếu từng maximize)
            if (_lastLoginWindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }

            LayoutModernUi();
        }

        private void CenterOnCurrentScreen()
        {
            // Center the form on the screen that contains most of this form
            var scr = Screen.FromControl(this);
            var wa = scr.WorkingArea;

            int x = wa.Left + (wa.Width - Width) / 2;
            int y = wa.Top + (wa.Height - Height) / 2;

            // Ensure we don't position outside working area
            x = Math.Max(wa.Left, Math.Min(x, wa.Right - Width));
            y = Math.Max(wa.Top, Math.Min(y, wa.Bottom - Height));

            Location = new Point(x, y);
        }

        private void FormLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            _lastLoginBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            _lastLoginWindowState = WindowState == FormWindowState.Minimized
                ? FormWindowState.Normal
                : WindowState;
        }
    }
}