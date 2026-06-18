using DNS.Server;
using Microsoft.Playwright;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DNS_Sinkhole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Socket & Script - Security Hub";
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            builder.Services.AddSingleton<StatsStore>();
            builder.Services.AddSingleton<BlockListStore>();
            builder.Services.AddSingleton<AutoAdBlockEngine>();
            builder.Services.AddSingleton<DNSListUpdaterService>();
            var app = builder.Build();
            app.UseCors("AllowAngular");

            app.MapGet("/api/stats", (StatsStore stats) => new
            {
                totalQueries = stats.TotalQueries,
                blockedQueries = stats.BlockedQueries,
                recentBlocks = stats.RecentBlocks.ToArray()
            });

            _ = app.RunAsync("http://localhost:5000");
            var statsStore = app.Services.GetRequiredService<StatsStore>();
            var listStore = app.Services.GetRequiredService<BlockListStore>();
            var adBlockEngine = app.Services.GetRequiredService<AutoAdBlockEngine>();
            var updater = app.Services.GetRequiredService<DNSListUpdaterService>();
            var cts = new CancellationTokenSource();
            _ = updater.StartAsync(cts.Token);
            await adBlockEngine.InitializeAsync();
            var sinkholeResolver = new SinkholeResolver(adBlockEngine, listStore, statsStore);
            using DnsServer server = new DnsServer(sinkholeResolver);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[+] Το Web API τρέχει στο http://localhost:5000/api/stats");
            Console.WriteLine("[+] Ο DNS Server ακούει στην πόρτα 53 (0.0.0.0)...");
            Console.ResetColor();
            _ = Task.Run(() => server.Listen(53), cts.Token);

            while (true)
            {
                Console.WriteLine("\n-------------------------------------------------------");
                Console.WriteLine("[1] Κανονικός Ad-Free Browser (Persistent Profile)");
                Console.WriteLine("[2] Burner Browser Mode (Απόλυτη Ανωνυμία - RAM Only)");
                Console.WriteLine("[0] Έξοδος και κλείσιμο DNS Server");
                Console.Write("\nΕπίλεξε λειτουργία (0-2): ");

                string choice = Console.ReadLine()?.Trim() ?? "";

                if (choice == "0")
                {
                    break;
                }

                if (choice == "1" || choice == "2")
                {
                    await using (var adFreeService = new UniversalAdFreeService())
                    {
                        try
                        {
                            Console.WriteLine("\n[*] Αρχικοποίηση της μηχανής Chromium...");
                            bool isBurner = (choice == "2");

                            if (isBurner)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("\n[!] Ενεργοποίηση Burner Mode. Τα πάντα τρέχουν στη RAM.");
                                Console.ResetColor();
                            }

                            await adFreeService.InitializeAsync(showUI: true, burnerMode: isBurner);

                            Console.Write("\nΔώσε URL για να ανοίξει (Enter για YouTube): ");
                            string inputUrl = Console.ReadLine()?.Trim() ?? "";

                            if (string.IsNullOrWhiteSpace(inputUrl)) inputUrl = "https://www.youtube.com";
                            else if (!inputUrl.StartsWith("http")) inputUrl = "https://" + inputUrl;

                            await adFreeService.OpenSiteAsync(inputUrl);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n[+] Η σελίδα άνοιξε!");
                            Console.WriteLine("[*] Κλείσε το παράθυρο του Browser για να επιστρέψεις στο μενού...");
                            Console.ResetColor();

                            await adFreeService.WaitUntilClosedAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n[!] Προέκυψε σφάλμα: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine("\n[+] Ο Browser καθαρίστηκε από τη μνήμη.");
                }
            }

            Console.WriteLine("\n[+] Τερματισμός του DNS Server και του συστήματος...");
            cts.Cancel();
        }
    }
}