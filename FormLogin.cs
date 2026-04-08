using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormLogin : Form
    {
        private static readonly Dictionary<string, string> Accounts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _loggedInUsername = "";

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

        public FormLogin()
        {
            InitializeComponent();

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

            // TabControl (ẩn header tab)
            tabControl1.Parent = _card;
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

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

        private RoundedPanel CreateInputHost(TextBox tb, bool isPassword, out Button toggle)
        {
            var host = new RoundedPanel();
            host.CornerRadius = 18;
            host.FillColor = Color.FromArgb(18, 20, 28);
            host.BorderColor = Color.FromArgb(55, 255, 255, 255);
            host.BorderThickness = 1;
            host.Height = 52;
            host.Padding = new Padding(14, 12, 14, 12);

            tb.Parent = host;
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = host.FillColor;
            tb.ForeColor = Color.White;
            tb.Font = new Font(UiTheme.BaseFont.FontFamily, 11F, FontStyle.Regular);
            tb.Location = new Point(14, 16);
            tb.Width = host.Width - host.Padding.Horizontal - 56;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            if (isPassword)
            {
                tb.UseSystemPasswordChar = true;
            }

             var toggleButton = new Button();
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

            if (!isPassword)
            {
                toggleButton.Visible = false;
            }

            host.Controls.Add(toggleButton);

            host.SizeChanged += (s, e) =>
            {
                int rightPad = host.Padding.Right;
                int leftPad = host.Padding.Left;
                int toggleW = toggleButton.Visible ? toggleButton.Width : 0;

                tb.Width = Math.Max(10, host.ClientSize.Width - leftPad - rightPad - toggleW);
            };

            toggle = toggleButton;
            return host;
        }

        private Label CreateCaption(string text)
        {
            var l = new Label();
            l.AutoSize = false;
            l.Height = 18;
            l.Text = text;
            l.ForeColor = Color.FromArgb(190, 255, 255, 255);
            l.Font = new Font(UiTheme.BaseFont.FontFamily, 9.5F, FontStyle.Regular);
            return l;
        }

        private void BuildLoginTab()
        {
            // Ẩn label cũ trong designer
            label1.Visible = false;
            label2.Visible = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 8;
            layout.Padding = new Padding(0, 6, 0, 0);
            layout.BackColor = tabPage1.BackColor;

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input user
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // caption pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // input pass
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // gap
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // button
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // filler

            _inpLoginUser = CreateInputHost(txtUsername, isPassword: false, out _);
            _inpLoginPass = CreateInputHost(txtPassword, isPassword: true, out _btnToggleLoginPass);

            _btnToggleLoginPass.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                _btnToggleLoginPass.Text = txtPassword.UseSystemPasswordChar ? "Hiện" : "Ẩn";
            };

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
            tabPage1.Controls.Add(layout);
        }

        private void BuildRegisterTab()
        {
            // Ẩn label cũ trong designer
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 12;
            layout.Padding = new Padding(0, 6, 0, 0);
            layout.BackColor = tabPage2.BackColor;

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

            _inpRegUser = CreateInputHost(txtRegUsername, isPassword: false, out _);
            _inpRegPass = CreateInputHost(txtRegPassword, isPassword: true, out _btnToggleRegPass);
            _inpRegConfirm = CreateInputHost(txtRegConfirm, isPassword: true, out _btnToggleRegConfirm);

            _btnToggleRegPass.Click += (s, e) =>
            {
                txtRegPassword.UseSystemPasswordChar = !txtRegPassword.UseSystemPasswordChar;
                _btnToggleRegPass.Text = txtRegPassword.UseSystemPasswordChar ? "Hiện" : "Ẩn";
            };

            _btnToggleRegConfirm.Click += (s, e) =>
            {
                txtRegConfirm.UseSystemPasswordChar = !txtRegConfirm.UseSystemPasswordChar;
                _btnToggleRegConfirm.Text = txtRegConfirm.UseSystemPasswordChar ? "Hiện" : "Ẩn";
            };

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
            tabPage2.Controls.Add(layout);
        }

        private void LayoutModernUi()
        {
            if (_bg == null || _card == null)
            {
                return;
            }

            // Fit card vào cửa sổ, chừa viền để không bị cắt
            int maxW = Math.Max(320, ClientSize.Width - 40);
            int maxH = Math.Max(420, ClientSize.Height - 40);

            _card.Size = new Size(Math.Min(420, maxW), Math.Min(520, maxH));
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

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (_loginInProgress)
            {
                return;
            }

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

            if (!Accounts.TryGetValue(username, out string savedPassword) ||
                !string.Equals(savedPassword, password, StringComparison.Ordinal))
            {
                MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!",
                    "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _loginInProgress = true;
            try
            {
                _loggedInUsername = username;
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _loginInProgress = false;
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

            if (Accounts.ContainsKey(username))
            {
                MessageBox.Show("Tài khoản đã tồn tại!",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Accounts[username] = password;

            MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        public string LoggedInUsername => _loggedInUsername;
    }
}