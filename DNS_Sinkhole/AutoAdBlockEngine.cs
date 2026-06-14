
namespace DNS_Sinkhole
{
    public class AutoAdBlockEngine
    {
        private HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly HttpClient _httpClient = new();
        private const string LocalCacheFile = "blocklist_cache.txt";
        private const string BlocklistUrl = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";

        public async Task InitializeAsync()
        {
            Console.WriteLine("[*] Εκκίνηση αυτόματης μηχανής ad-block...");
            LoadFromLocalCache();
            await UpdateBlocklistFromInternetAsync();
            _ = StartAutoUpdateTimer(TimeSpan.FromHours(24));
        }

        public bool IsBlocked(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return false;
            domain = domain.TrimEnd('.');

            if (_blockedDomains.Contains(domain)) return true;

            string host = domain;
            int nextDot;
            while ((nextDot = host.IndexOf('.')) != -1)
            {
                host = host.Substring(nextDot + 1);
                if (_blockedDomains.Contains(host)) return true;
            }

            return false;
        }

        private async Task UpdateBlocklistFromInternetAsync()
        {
            try
            {
                Console.WriteLine($"[~] Αυτόματο κατέβασμα ενημερωμένης λίστας από: {BlocklistUrl}");
                string content = await _httpClient.GetStringAsync(BlocklistUrl);
                await File.WriteAllTextAsync(LocalCacheFile, content);
                ParseAndLoadRecords(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Αποτυχία αυτόματης ενημέρωσης: {ex.Message}. Θα χρησιμοποιηθούν τα παλιά δεδομένα.");
            }
        }

        private async Task StartAutoUpdateTimer(TimeSpan interval)
        {
            using PeriodicTimer timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync())
            {
                Console.WriteLine($"\n[*] {DateTime.Now}: Έναρξη προγραμματισμένης αυτόματης ανανέωσης λίστας...");
                await UpdateBlocklistFromInternetAsync();
            }
        }

        private void LoadFromLocalCache()
        {
            if (File.Exists(LocalCacheFile))
            {
                Console.WriteLine("[+] Βρέθηκε τοπικό αντίγραφο ασφαλείας. Φόρτωση...");
                string content = File.ReadAllText(LocalCacheFile);
                ParseAndLoadRecords(content);
            }
        }

        private void ParseAndLoadRecords(string rawContent)
        {
            var newDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using StringReader reader = new StringReader(rawContent);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    string domain = parts[1].Trim();
                    if (domain != "localhost")
                    {
                        newDomains.Add(domain);
                    }
                }
            }

            _blockedDomains = newDomains;
            Console.WriteLine($"[+] Η λίστα ενημερώθηκε αυτόματα! Ενεργά domains προστασίας: {_blockedDomains.Count}");
        }
    }
}