using System;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var splash = new FormSplash())
            {
                splash.ShowDialog();
            }

            while (true)
            {
                using (var login = new FormLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    using (var chat = new FormChat(null, login.LoggedInUsername))
                    {
                        chat.ShowDialog();
                    }
                }
            }
        }
    }
}
