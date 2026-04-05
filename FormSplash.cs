using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormSplash : Form
    {
        public FormSplash()
        {
            InitializeComponent();
            this.Resize += FormSplash_Resize;
            CenterControls();
        }

        private void FormSplash_Resize(object sender, EventArgs e)
        {
            CenterControls();
        }

        private void CenterControls()
        {
            // Căn giữa tất cả controls
            foreach (Control c in this.Controls)
            {
                c.Left = (this.ClientSize.Width - c.Width) / 2;
            }
        }

        private void FormSplash_Load(object sender, EventArgs e)
        {

        }

        private void timerSplash_Tick(object sender, EventArgs e)
        {
            timerSplash.Stop();
            this.Hide();
            var loginForm = new FormLogin();
            loginForm.Show();
        }
    }
}
