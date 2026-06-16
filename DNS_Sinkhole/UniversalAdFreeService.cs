using Microsoft.Playwright;

namespace DNS_Sinkhole
{
    public class UniversalAdFreeService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        public async Task InitializeAsync(bool showUI = true, bool burnerMode = false)
        {
            _playwright = await Playwright.CreateAsync();

            if (burnerMode)
            {
                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = !showUI,
                    Channel = "chrome",
                    Args = new[]
                    {
                        "--disable-application-cache",
                        "--disable-offline-load-stale-cache",
                        "--disk-cache-size=1",
                        "--media-cache-size=1"
                    }
                };

                _browser = await _playwright.Chromium.LaunchAsync(launchOptions);

                var contextOptions = new BrowserNewContextOptions();
                contextOptions.ServiceWorkers = ServiceWorkerPolicy.Block;
                _context = await _browser.NewContextAsync(contextOptions);
                _page = await _context.NewPageAsync();
            }
            else
            {
                string userDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SocketScriptBrowser");

                var persistentOptions = new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = !showUI,
                    Channel = "chrome"
                };

                _context = await _playwright.Chromium.LaunchPersistentContextAsync(userDataDir, persistentOptions);
                _page = await _context.NewPageAsync();
                var allPages = new List<IPage>(_context.Pages);

                foreach (var p in allPages)
                {
                    if (p != _page && !p.IsClosed)
                    {
                        await p.CloseAsync();
                    }
                }

                await _context.AddInitScriptAsync(@"
                window.open = function() { return null; };
            ");

                await _context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined
                });
            ");

                await _context.AddInitScriptAsync(@"
                setInterval(() => {
                    const skipKeywords = ['skip ad', 'παράλειψη', 'close ad', 'κλείσιμο'];
                    const clickableElements = document.querySelectorAll('button, div, span, a');
                    
                    for (let el of clickableElements) {
                        const text = (el.innerText || '').toLowerCase().trim();
                        if (skipKeywords.some(kw => text === kw || text.includes(kw)) && el.offsetParent !== null) {
                            if (text.length < 25 && !el.hasAttribute('data-clicked')) { 
                                el.setAttribute('data-clicked', 'true');
                                el.click(); 
                            }
                        }
                    }

                    const videos = document.querySelectorAll('video');
                    const isAdPlaying = document.querySelector('.ad-showing') !== null;

                    videos.forEach(video => {
                        if (isAdPlaying) {
                            video.playbackRate = 16.0;
                            video.muted = true;
                            if (!isNaN(video.duration) && video.duration > 0) {
                                video.currentTime = video.duration;
                            }
                        } else {
                            if (video.playbackRate === 16.0) {
                                video.playbackRate = 1.0;
                                video.muted = false;
                            }
                        }
                    });
                }, 100);
            ");

                await _context.AddInitScriptAsync(@"
                document.addEventListener('DOMContentLoaded', () => {
                    const style = document.createElement('style');
                    style.innerHTML = `
                        iframe[src*=""ads""],
                        iframe[id*=""google_ads""],
                        div[id*=""banner""],
                        div[class*=""ad-container""],
                        div[class*=""ad-slot""],
                        ins.adsbygoogle {
                            display: none !important;
                            width: 0px !important;
                            height: 0px !important;
                        }
                    `;
                    document.head.appendChild(style);
                });
            ");

                _context.Page += async (_, newPage) =>
                {
                    if (newPage != _page)
                    {
                        await newPage.CloseAsync();
                    }
                };
            }
        }

        public async Task OpenSiteAsync(string url)
        {
            if (_page == null) throw new InvalidOperationException("Πρέπει να καλέσεις την InitializeAsync().");

            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
        }

        public async Task WaitUntilClosedAsync()
        {
            if (_page != null && !_page.IsClosed)
            {
                var tcs = new TaskCompletionSource();
                _page.Close += (_, _) => tcs.TrySetResult();

                try
                {
                    await tcs.Task;
                }
                catch (Exception) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }
    }
}