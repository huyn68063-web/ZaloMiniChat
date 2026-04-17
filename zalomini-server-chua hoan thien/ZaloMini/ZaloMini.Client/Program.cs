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

            // Create a single NetworkClient and connect before showing login
            var network = new NetworkClient();
            bool connected = network.Connect("127.0.0.1", 9000); // server listens on 9000

            if (!connected)
            {
                MessageBox.Show("Không thể kết nối đến server tại 127.0.0.1:9000. Ứng dụng sẽ đóng.", "Kết nối thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (true)
            {
                using (var login = new FormLogin(network))
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        network.Dispose();
                        return;
                    }

                    using (var chat = new FormChat(network, login.LoggedInUsername))
                    {
                        chat.ShowDialog();
                    }
                }
            }
        }
    }
}
