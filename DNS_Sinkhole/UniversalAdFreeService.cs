using Microsoft.Playwright;

namespace DNS_Sinkhole
{
    public class UniversalAdFreeService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        public async Task InitializeAsync(bool showUI = true)
        {
            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !showUI,
                Channel = "chrome"
            });

            _context = await _browser.NewContextAsync();

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
                    // Μέρος Α: Πατάμε τα κουμπιά 'Skip' αν εμφανιστούν
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

                    // Μέρος Β: Fast-Forward ΜΟΝΟ αν είναι όντως διαφήμιση
                    const videos = document.querySelectorAll('video');
                    
                    // Το YouTube βάζει την κλάση '.ad-showing' ΜΟΝΟ όταν παίζει διαφήμιση
                    const isAdPlaying = document.querySelector('.ad-showing') !== null;

                    videos.forEach(video => {
                        if (isAdPlaying) {
                            // Είναι διαφήμιση: Βάλτο στο x16 και πήγαινε στο τέλος
                            video.playbackRate = 16.0;
                            video.muted = true;
                            if (!isNaN(video.duration) && video.duration > 0) {
                                video.currentTime = video.duration;
                            }
                        } else {
                            // ΕΙΝΑΙ ΚΑΝΟΝΙΚΟ ΒΙΝΤΕΟ: 
                            // Αν είχε ξεμείνει στο x16 από πριν, επανέφερέ το στο κανονικό (x1)!
                            if (video.playbackRate === 16.0) {
                                video.playbackRate = 1.0;
                                video.muted = false;
                            }
                        }
                    });
                }, 100);
            ");

            _page = await _context.NewPageAsync();

            _context.Page += async (_, newPage) =>
            {
                if (newPage != _page)
                {
                    await newPage.CloseAsync();
                }
            };
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
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }
    }
}