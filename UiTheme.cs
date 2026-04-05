using System.Drawing;
using System.Windows.Forms;

namespace ZaloMini.Client
{
    internal static class UiTheme
    {
        public static readonly Color PrimaryColor = Color.FromArgb(0, 104, 255);
        public static readonly Color PrimaryHoverColor = Color.FromArgb(0, 92, 230);

        public static readonly Color AppBackground = Color.WhiteSmoke;
        public static readonly Color SurfaceColor = Color.White;

        public static readonly Color IncomingBubbleColor = Color.FromArgb(235, 235, 235);
        public static readonly Color OutgoingBubbleColor = PrimaryColor;

        public static readonly Color IncomingTextColor = Color.Black;
        public static readonly Color OutgoingTextColor = Color.White;

        public static readonly Color MutedTextColor = Color.Gray;

        public static readonly Font BaseFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        public static void ApplyHeader(Panel panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.BackColor = PrimaryColor;
        }

        public static void ApplyPrimaryButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;

            button.MouseEnter += (s, e) => button.BackColor = PrimaryHoverColor;
            button.MouseLeave += (s, e) => button.BackColor = PrimaryColor;
        }

        public static void ApplySecondaryButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = Color.FromArgb(230, 230, 230);
            button.ForeColor = Color.Black;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;

            button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(220, 220, 220);
            button.MouseLeave += (s, e) => button.BackColor = Color.FromArgb(230, 230, 230);
        }

        public static void ApplyIconButton(Button button, Color backColor)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
        }

        public static void ApplyFormBase(Form form)
        {
            if (form == null)
            {
                return;
            }

            form.BackColor = SurfaceColor;
            form.Font = BaseFont;
        }

        public static void ApplyChatSurface(RichTextBox box)
        {
            if (box == null)
            {
                return;
            }

            box.BackColor = SurfaceColor;
            box.BorderStyle = BorderStyle.None;
        }
    }
}