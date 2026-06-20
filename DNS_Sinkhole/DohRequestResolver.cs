using DNS.Client.RequestResolver;
using DNS.Protocol;
using System.Net.Http.Headers;

namespace DNS_Sinkhole
{
    public class DohRequestResolver : IRequestResolver
    {
        private readonly HttpClient _httpClient;
        private readonly string _dohEndpoint;

        public DohRequestResolver(string dohEndpoint)
        {
            _dohEndpoint = dohEndpoint;
            _httpClient = new HttpClient();
        }

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            byte[] requestBytes = request.ToArray();
            var content = new ByteArrayContent(requestBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _dohEndpoint)
            {
                Content = content
            };

            if (_dohEndpoint.Contains("1.1.1.1") || _dohEndpoint.Contains("1.0.0.1"))
            {
                httpRequest.Headers.Host = "cloudflare-dns.com";
            }

            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-message"));
            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();
            byte[] responseBytes = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            return Response.FromArray(responseBytes);
        }
    }
}