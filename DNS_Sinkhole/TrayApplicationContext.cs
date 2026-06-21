namespace DNS_Sinkhole
{
    using System.Windows.Forms;
    using System.Drawing;

    // ... (υπόλοιπα usings)

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;

        public TrayApplicationContext(StatsStore stats)
        {
            _notifyIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Shield, // Μπορείς να βάλεις δικό σου .ico αρχείο
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Socket & Script - Security Hub"
            };

            _notifyIcon.ContextMenuStrip.Items.Add("Άνοιγμα Dashboard", null, (s, e) => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://localhost:4200") { UseShellExecute = true });
            });

            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            _notifyIcon.ContextMenuStrip.Items.Add("Έξοδος", null, (s, e) => {
                _notifyIcon.Visible = false;
                Application.Exit();
                Environment.Exit(0);
            });

            _notifyIcon.DoubleClick += (s, e) => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://localhost:4200") { UseShellExecute = true });
            };
        }

        public void ShowNotification(string title, string message)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
    }
}
