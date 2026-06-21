namespace DNS_Sinkhole
{
    public class TrayNotificator : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;

        public TrayNotificator()
        {
            _notifyIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Shield,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Socket & Script - Security Hub"
            };

            _notifyIcon.ContextMenuStrip.Items.Add("Open Dashboard", null, (s, e) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://localhost:4200") { UseShellExecute = true }));

            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => {
                _notifyIcon.Visible = false;
                Application.Exit();
                Environment.Exit(0);
            });
        }

        public void NotifyBlock(string domain)
        {
            _notifyIcon.ShowBalloonTip(2000, "Security Hub 🛡️", $"Blocked: {domain}", ToolTipIcon.Warning);
        }
    }
}
