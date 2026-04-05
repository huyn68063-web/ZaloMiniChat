using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormLogin : Form
    {
        private static readonly Dictionary<string, string> Accounts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _loggedInUsername = "";

        public FormLogin()
        {
            InitializeComponent();
            ApplyTheme();

            this.Resize += FormLogin_Resize;

            // Gắn event handler cho phần đăng nhập
            this.txtUsername.KeyDown += TxtUsername_KeyDown;
            this.txtPassword.KeyDown += TxtPassword_KeyDown;

            // Gắn event handler cho phần đăng ký
            this.txtRegUsername.KeyDown += TxtRegUsername_KeyDown;
            this.txtRegPassword.KeyDown += TxtRegPassword_KeyDown;
            this.txtRegConfirm.KeyDown += TxtRegConfirm_KeyDown;
        }

        private void ApplyTheme()
        {
            UiTheme.ApplyFormBase(this);
            UiTheme.ApplyHeader(panelHeader);
            UiTheme.ApplyPrimaryButton(btnLogin);
            UiTheme.ApplyPrimaryButton(btnRegister);
        }

        private void FormLogin_Resize(object sender, EventArgs e)
        {
            panelHeader.Width = this.ClientSize.Width;
            tabControl1.Left = (this.ClientSize.Width - tabControl1.Width) / 2;
            tabControl1.Top = panelHeader.Height + 20;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
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

            if (!Accounts.TryGetValue(username, out string savedPassword) || !string.Equals(savedPassword, password, StringComparison.Ordinal))
            {
                MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!",
                    "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _loggedInUsername = username;

            // Chưa kết nối server: truyền null vào FormChat để chạy UI sườn.
            this.Hide();
            using (var chatForm = new FormChat(null, _loggedInUsername))
            {
                chatForm.ShowDialog();
            }

            this.Show();
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
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(sender, e);
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
                btnRegister_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}