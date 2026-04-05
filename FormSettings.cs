using System;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormSettings : Form
    {
        private string _configPath = "server.config";

        public FormSettings()
        {
            InitializeComponent();
            ApplyTheme();
            LoadSettings();
        }

        private void ApplyTheme()
        {
            UiTheme.ApplyFormBase(this);
            UiTheme.ApplyHeader(panel1);
            UiTheme.ApplyPrimaryButton(btnSave);
            UiTheme.ApplySecondaryButton(btnBrowse);
        }

        private void LoadSettings()
        {
            try
            {
                // Đọc cài đặt thư mục lưu file
                if (!string.IsNullOrEmpty(Properties.Settings.Default.SaveFolder))
                    txtSaveFolder.Text = Properties.Settings.Default.SaveFolder;
                else
                    txtSaveFolder.Text = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ZaloMini");

                // Đọc cài đặt Server
                if (System.IO.File.Exists(_configPath))
                {
                    string[] lines = System.IO.File.ReadAllLines(_configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("IP="))
                        {
                            if (this.Controls.ContainsKey("txtServerIP"))
                                this.Controls["txtServerIP"].Text = line.Substring(3).Trim();
                        }
                        if (line.StartsWith("PORT="))
                        {
                            if (this.Controls.ContainsKey("txtServerPort"))
                                this.Controls["txtServerPort"].Text = line.Substring(5).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠️ Lỗi đọc cài đặt: " + ex.Message);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Chọn thư mục lưu file nhận";
            if (fbd.ShowDialog() == DialogResult.OK)
                txtSaveFolder.Text = fbd.SelectedPath;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSaveFolder.Text))
            {
                MessageBox.Show("⚠️ Vui lòng chọn thư mục!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Lưu thư mục
                Properties.Settings.Default.SaveFolder = txtSaveFolder.Text;
                Properties.Settings.Default.Save();

                // Lưu cài đặt Server (nếu có textbox)
                if (this.Controls.ContainsKey("txtServerIP") &&
                    this.Controls.ContainsKey("txtServerPort"))
                {
                    string ip = this.Controls["txtServerIP"].Text.Trim();
                    string port = this.Controls["txtServerPort"].Text.Trim();

                    if (!IsValidIP(ip))
                    {
                        MessageBox.Show("❌ IP không hợp lệ! (ví dụ: 192.168.1.1)",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (!int.TryParse(port, out int portNum) || portNum < 1 || portNum > 65535)
                    {
                        MessageBox.Show("❌ Port phải là số từ 1 đến 65535!",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Ghi server.config
                    string[] configLines = new string[]
                    {
                        $"IP={ip}",
                        $"PORT={port}"
                    };
                    System.IO.File.WriteAllLines(_configPath, configLines);
                }

                MessageBox.Show("✅ Đã lưu cài đặt!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidIP(string ip)
        {
            if (System.Net.IPAddress.TryParse(ip, out _))
                return true;
            return false;
        }
    }
}