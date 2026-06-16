using DNS.Server;
using System.Net;
using Microsoft.Playwright;

namespace DNS_Sinkhole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Socket & Script - Security Hub";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("==========================================");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("      SOCKET & SCRIPT - SECURITY HUB      ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("==========================================\n");
            Console.ResetColor();
            var listStore = new BlockListStore();
            var updater = new DNSListUpdaterService(listStore);
            var cts = new CancellationTokenSource();
            _ = updater.StartAsync(cts.Token);
            var adBlockEngine = new AutoAdBlockEngine();
            await adBlockEngine.InitializeAsync();
            IPEndPoint mullvadDns = new IPEndPoint(IPAddress.Parse("193.138.218.74"), 53);
            var sinkholeResolver = new SinkholeResolver(adBlockEngine, mullvadDns, listStore);
            using DnsServer server = new DnsServer(sinkholeResolver);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[+] Ο DNS Server είναι έτοιμος.");
            Console.WriteLine("[+] Ακούει στην πόρτα 53 (0.0.0.0)...");
            Console.ResetColor();
            Console.WriteLine("-------------------------------------------------------");
            _ = Task.Run(() => server.Listen(53));
            Console.WriteLine("\n[1] Κανονικός Ad-Free Browser");
            Console.WriteLine("[2] Burner Browser Mode (Απόλυτη Ανωνυμία - RAM Only)");
            Console.Write("\nΕπίλεξε λειτουργία (1-2): ");
            string choice = Console.ReadLine()?.Trim() ?? "";

            await using var adFreeService = new UniversalAdFreeService();

            try
            {
                Console.WriteLine("\n[*] Αρχικοποίηση της μηχανής Chromium...");
                bool isBurner = (choice == "2");

                if (isBurner)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[!] Ενεργοποίηση Burner Mode. Τα πάντα τρέχουν στη RAM. Μηδενικό αποτύπωμα στον δίσκο.");
                    Console.ResetColor();
                }

                await adFreeService.InitializeAsync(showUI: true, burnerMode: isBurner);
                Console.Write("\nΔώσε ένα URL για να ανοίξει (Enter για YouTube): ");
                string? inputUrl = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(inputUrl))
                {
                    inputUrl = "https://www.youtube.com";
                }
                else if (!inputUrl.StartsWith("http"))
                {
                    inputUrl = "https://" + inputUrl;
                }

                await adFreeService.OpenSiteAsync(inputUrl);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Η σελίδα άνοιξε!");
                Console.WriteLine("[+] Τα Pop-ups και οι in-video διαφημίσεις μπλοκάρονται από το Playwright.");
                Console.WriteLine("[+] Τα δικτυακά trackers κόβονται στο background από τον DNS Server (0.0.0.0).");
                Console.WriteLine("\n[*] Κλείσε το παράθυρο του Browser για να τερματιστεί η εφαρμογή...");
                Console.ResetColor();

                await adFreeService.WaitUntilClosedAsync();
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("closed"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[!!!] ΚΡΙΣΙΜΟ ΣΦΑΛΜΑ: {ex.GetType().Name}");
                Console.WriteLine($"[!!!] ΜΗΝΥΜΑ: {ex.Message}");
                Console.WriteLine($"[!!!] STACK TRACE: {ex.StackTrace}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[!] Προέκυψε σφάλμα στον Browser: {ex.Message}");
                Console.ResetColor();
            }
            Console.WriteLine("\n[+] Ο Browser έκλεισε. Τερματισμός του DNS Server και του συστήματος...");
            cts.Cancel();
        }
    }
}