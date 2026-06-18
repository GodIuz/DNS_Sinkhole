using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Net;
using System.Collections.Concurrent;
using Microsoft.Toolkit.Uwp.Notifications;

namespace DNS_Sinkhole
{
    public class SinkholeResolver : IRequestResolver
    {
        private readonly AutoAdBlockEngine _adBlockEngine;
        private readonly BlockListStore _listStore;
        private readonly IRequestResolver _dohForwarder;
        private readonly StatsStore _statsStore;
        private static readonly ConcurrentDictionary<string, DateTime> _alertedDomains = new();

        public SinkholeResolver(AutoAdBlockEngine adBlockEngine, BlockListStore listStore, StatsStore statsStore)
        {
            _adBlockEngine = adBlockEngine;
            _listStore = listStore;
            _statsStore = statsStore;
            _dohForwarder = new DohRequestResolver("https://1.0.0.1/dns-query");
        }

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Questions.Count == 0)
            {
                return Response.FromRequest(request);
            }
            _statsStore.AddQuery();

            string requestedDomain = request.Questions[0].Name.ToString();
            string cleanDomain = requestedDomain.TrimEnd('.');

            if (_listStore.IsBlocked(cleanDomain))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [⛔] AUTO-BLOCK: {cleanDomain}");
                Console.ResetColor();
                _statsStore.AddBlocked(cleanDomain);
                _ = Task.Run(() => SendDesktopNotification(cleanDomain));
                return BlockRequest(request);
            }

            if (_adBlockEngine.IsBlocked(requestedDomain))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [X] SINKHOLED: {requestedDomain}");
                Console.ResetColor();
                _statsStore.AddBlocked(requestedDomain);
                return BlockRequest(request);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [🔒] ΠΡΟΩΘΗΣΗ (DoH/Cloudflare): {requestedDomain}");
            Console.ResetColor();

            return await _dohForwarder.Resolve(request, cancellationToken);
        }

        private IResponse BlockRequest(IRequest request)
        {
            IResponse response = Response.FromRequest(request);
            response.AnswerRecords.Add(new IPAddressResourceRecord(
                request.Questions[0].Name,
                IPAddress.Any));
            return response;
        }

        private void SendDesktopNotification(string domain)
        {
            if (_alertedDomains.TryGetValue(domain, out var lastAlertTime) && (DateTime.Now - lastAlertTime).TotalHours < 1)
            {
                return;
            }

            _alertedDomains[domain] = DateTime.Now;

            try
            {
                new ToastContentBuilder()
                    .AddText("Socket & Script - Security Hub 🛡️")
                    .AddText($"Αποτράπηκε κρυφή σύνδεση στο:")
                    .AddText(domain)
                    .Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Αποτυχία εμφάνισης Windows Notification: {ex.Message}");
            }
        }
    }
}