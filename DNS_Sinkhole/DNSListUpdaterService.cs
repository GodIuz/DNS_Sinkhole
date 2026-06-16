

namespace DNS_Sinkhole
{
    public class DNSListUpdaterService
    {
        private readonly BlockListStore _store;
        private readonly HttpClient _httpClient;

        private readonly string[] _listUrls = new[]
        {
            "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
            "https://adaway.org/hosts.txt",
            "https://pgl.yoyo.org/adservers/serverlist.php?hostformat=hosts&showintro=0&mimetype=plaintext",
            "https://openphish.com/feed.txt"
        };

        public DNSListUpdaterService(BlockListStore store)
        {
            _store = store;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[i] Ο Auto-Updater Service ξεκίνησε.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[Background] Έναρξη προγραμματισμένης ενημέρωσης λιστών ασφαλείας...");
                    var newDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var url in _listUrls)
                    {
                        try
                        {
                            var response = await _httpClient.GetStringAsync(url, stoppingToken);
                            using var reader = new StringReader(response);
                            string? line;

                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                line = line.Trim();

                                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                                    continue;

                                if (line.StartsWith("0.0.0.0") || line.StartsWith("127.0.0.1"))
                                {
                                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length > 1)
                                    {
                                        newDomains.Add(parts[1].Trim());
                                    }
                                }
                                else
                                {
                                    newDomains.Add(line);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Αποτυχία λήψης λίστας από: {url}. Σφάλμα: {ex.Message}");
                        }
                    }

                    if (newDomains.Count > 0)
                    {
                        _store.UpdateList(newDomains);
                        Console.WriteLine($"[Background] Η λίστα ενημερώθηκε επιτυχώς! Συνολικά Domains στην άμυνα: {_store.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Κρίσιμο σφάλμα στον Auto-Updater: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}