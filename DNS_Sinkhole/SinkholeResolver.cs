using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Net;

namespace DNS_Sinkhole
{
    public class SinkholeResolver : IRequestResolver
    {
        private readonly AutoAdBlockEngine _adBlockEngine;
        private readonly BlockListStore _listStore;
        private readonly IRequestResolver _dohForwarder;
        private readonly StatsStore _statsStore;
        private readonly Dictionary<string, IPAddress> _localRecords;

        public SinkholeResolver(AutoAdBlockEngine adBlockEngine, BlockListStore listStore, StatsStore statsStore)
        {
            _adBlockEngine = adBlockEngine;
            _listStore = listStore;
            _statsStore = statsStore;
            _dohForwarder = new DohRequestResolver("https://1.0.0.1/dns-query");
            var piAddress = IPAddress.Parse("192.168.1.100");
            _localRecords = new Dictionary<string, IPAddress>(StringComparer.OrdinalIgnoreCase)
            {
                { "kyriakidis.dev", piAddress },
                { "drive.kyriakidis.dev", piAddress },
                { "status.kyriakidis.dev", piAddress },
                { "home.kyriakidis.dev", piAddress }
            };
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

            if (_localRecords.TryGetValue(cleanDomain, out var localIp))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [🏠] SPLIT-HORIZON: {cleanDomain} -> {localIp}");
                Console.ResetColor();

                return ReturnLocalIp(request, localIp);
            }

            if (_listStore.IsBlocked(cleanDomain))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [⛔] AUTO-BLOCK: {cleanDomain}");
                Console.ResetColor();
                _statsStore.AddBlocked(cleanDomain);
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

        private IResponse ReturnLocalIp(IRequest request, IPAddress ip)
        {
            IResponse response = Response.FromRequest(request);
            response.AnswerRecords.Add(new IPAddressResourceRecord(
                request.Questions[0].Name,
                ip));
            return response;
        }
    }
}