using System;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    public partial class FormSplash : Form
    {
        public FormSplash()
        {
            InitializeComponent();
        }

        private void FormSplash_Load(object sender, EventArgs e)
        {
        }

        private void timerSplash_Tick(object sender, EventArgs e)
        {
            timerSplash.Stop();
            Close();
        }
    }
}
