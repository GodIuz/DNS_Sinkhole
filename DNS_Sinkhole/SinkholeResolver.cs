using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Net;

namespace DNS_Sinkhole
{
    public class SinkholeResolver : IRequestResolver
    {
        private readonly AutoAdBlockEngine _adBlockEngine;
        private readonly IRequestResolver _mullvadForwarder;

        public SinkholeResolver(AutoAdBlockEngine adBlockEngine, IPEndPoint upstreamDns)
        {
            _adBlockEngine = adBlockEngine;
            _mullvadForwarder = new UdpRequestResolver(upstreamDns);
        }

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Questions.Count == 0)
            {
                return Response.FromRequest(request);
            }

            string requestedDomain = request.Questions[0].Name.ToString();

            if (_adBlockEngine.IsBlocked(requestedDomain))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [X] SINKHOLED: {requestedDomain}");
                Console.ResetColor();

                IResponse response = Response.FromRequest(request);
                response.AnswerRecords.Add(new IPAddressResourceRecord(
                    request.Questions[0].Name,
                    IPAddress.Any));

                return response;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [>] ΠΡΟΩΘΗΣΗ (Mullvad): {requestedDomain}");
                Console.ResetColor();
                return await _mullvadForwarder.Resolve(request, cancellationToken);
            }
        }
    }
}