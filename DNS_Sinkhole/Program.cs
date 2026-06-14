using DNS.Server;
using System.Net;
using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace DNS_Sinkhole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Δικτυακό DNS Sinkhole & Ad-Free Browser σε .NET 8 ===");

            var adBlockEngine = new AutoAdBlockEngine();
            await adBlockEngine.InitializeAsync();

            IPEndPoint mullvadDns = new IPEndPoint(IPAddress.Parse("193.138.218.74"), 53);
            var sinkholeResolver = new SinkholeResolver(adBlockEngine, mullvadDns);
            using DnsServer server = new DnsServer(sinkholeResolver);

            Console.WriteLine("\n[+] Ο DNS Server είναι έτοιμος.");
            Console.WriteLine("[+] Ακούει στην πόρτα 53 (0.0.0.0)...");
            Console.WriteLine("-------------------------------------------------------");
            _ = Task.Run(() => server.Listen(53));


            await using var adFreeService = new UniversalAdFreeService();

            try
            {
                Console.WriteLine("[*] Αρχικοποίηση της μηχανής Chromium...");
                await adFreeService.InitializeAsync(showUI: true);

                Console.Write("\nΔώσε ένα URL για να ανοίξει (Enter για YouTube): ");
                string? inputUrl = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(inputUrl))
                {
                    inputUrl = "https://www.youtube.com";
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
        }
    }
}