using DNS.Server;
using DNS.Protocol;

namespace DNS_Sinkhole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Socket & Script - Security Hub";
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

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
            builder.Services.AddSingleton<SinkholeResolver>();
            var app = builder.Build();
            app.UseCors("AllowAngular");

            app.MapPost("/dns-query", async (HttpContext context, SinkholeResolver resolver) =>
            {
                if (context.Request.ContentType != "application/dns-message")
                {
                    context.Response.StatusCode = 415;
                    return;
                }

                using var ms = new MemoryStream();

                await context.Request.Body.CopyToAsync(ms);
                byte[] requestBytes = ms.ToArray();

                try
                {
                    var dnsRequest = Request.FromArray(requestBytes);
                    var dnsResponse = await resolver.Resolve(dnsRequest);
                    byte[] responseBytes = dnsResponse.ToArray();

                    context.Response.ContentType = "application/dns-message";
                    context.Response.ContentLength = responseBytes.Length;
                    await context.Response.Body.WriteAsync(responseBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Σφάλμα στο DoH POST: {ex.Message}");
                    context.Response.StatusCode = 500;
                }
            });

            app.MapGet("/dns-query", async (HttpContext context, SinkholeResolver resolver) =>
            {
                var dnsParam = context.Request.Query["dns"].ToString();
                if (string.IsNullOrEmpty(dnsParam))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                try
                {
                    string padded = dnsParam.PadRight(dnsParam.Length + (4 - dnsParam.Length % 4) % 4, '=');
                    byte[] requestBytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));

                    var dnsRequest = Request.FromArray(requestBytes);
                    var dnsResponse = await resolver.Resolve(dnsRequest);
                    byte[] responseBytes = dnsResponse.ToArray();

                    context.Response.ContentType = "application/dns-message";
                    context.Response.ContentLength = responseBytes.Length;
                    await context.Response.Body.WriteAsync(responseBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Σφάλμα στο DoH GET: {ex.Message}");
                    context.Response.StatusCode = 400;
                }
            });

            app.MapGet("/api/stats", (StatsStore stats) => new
            {
                totalQueries = stats.TotalQueries,
                blockedQueries = stats.BlockedQueries,
                recentBlocks = stats.RecentBlocks.Reverse().ToArray()
            });

            app.MapGet("/api/stats/history", (StatsStore stats) =>
            {
                return stats.GetHistory(24);
            });

            app.MapGet("/api/stats/clients", (StatsStore stats) =>
            {
                return stats.GetTopClients(5);
            });

            app.MapGet("/api/logs", (StatsStore stats) =>
            {
                return stats.GetLiveLogs();
            });

            _ = app.RunAsync();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("==========================================");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("      SOCKET & SCRIPT - SECURITY HUB      ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("==========================================\n");
            Console.ResetColor();
            var statsStore = app.Services.GetRequiredService<StatsStore>();
            var listStore = app.Services.GetRequiredService<BlockListStore>();
            var adBlockEngine = app.Services.GetRequiredService<AutoAdBlockEngine>();
            var updater = app.Services.GetRequiredService<DNSListUpdaterService>();
            var sinkholeResolver = app.Services.GetRequiredService<SinkholeResolver>();

            var cts = new CancellationTokenSource();
            _ = updater.StartAsync(cts.Token);
            await adBlockEngine.InitializeAsync();

            using DnsServer server = new DnsServer(sinkholeResolver);
            var trayContext = new TrayApplicationContext(statsStore);
            Thread trayThread = new Thread(() => {
                Application.Run(trayContext);
            });
            trayThread.SetApartmentState(ApartmentState.STA);
            server.Responded += (sender, e) =>
            {
                if (e.Request.Questions.Count == 0) return;

                string domain = e.Request.Questions[0].Name.ToString();
                string clientIp = e.Remote.Address.ToString();
                if (clientIp == "::1") clientIp = "127.0.0.1";

                bool isBlocked = e.Response.AnswerRecords.Any(r => r is DNS.Protocol.ResourceRecords.IPAddressResourceRecord ipRec && ipRec.IPAddress.Equals(System.Net.IPAddress.Any));

                if (isBlocked)
                {
                    statsStore.AddBlockedClient(clientIp);
                    trayContext.ShowNotification("Security Hub 🛡️", $"Μπλοκαρίστηκε:\n{domain}");
                }

                statsStore.AddLiveLog(domain, clientIp, isBlocked);
            };
            trayThread.Start();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[+] Το Web API (Dashboard) τρέχει στο http://localhost:5000");
            Console.WriteLine("[+] Ο Secure DoH Server τρέχει στο https://localhost:5001/dns-query");
            Console.WriteLine("[+] Ο παραδοσιακός DNS ακούει στην πόρτα 53 (0.0.0.0)...");
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
                                Console.WriteLine("\n[!] Ενεργοποίηση Burner Mode. Τα πάντα τρέχουν στη RAM. Μηδενικό αποτύπωμα στον δίσκο.");
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

                    Console.WriteLine("\n[+] Ο Browser καθαρίστηκε από τη μνήμη. Επιστροφή στο κεντρικό μενού.");
                }
            }

            Console.WriteLine("\n[+] Τερματισμός του DNS Server και του συστήματος...");
            cts.Cancel();
        }
    }
}