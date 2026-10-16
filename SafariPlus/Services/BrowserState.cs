using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafariPlus.Models;

namespace SafariPlus.Services
{
    public class BrowserState
    {
        private readonly WebFetcherService _fetcher;
        private readonly ReaderEngine _readerEngine;
        private readonly TrackerBlockerService _trackerBlocker;

        public event Action? OnChange;

        public List<TabModel> Tabs { get; } = new();
        public TabModel ActiveTab { get; private set; } = new();

        public List<BookmarkModel> Bookmarks { get; } = new();
        public List<ReadingListItem> ReadingList { get; } = new();
        public List<QuickNote> QuickNotes { get; } = new();
        public List<HistoryItem> History { get; } = new();
        public List<PasswordCredential> Passwords { get; } = new();

        public ReaderThemeSettings ReaderSettings { get; } = new();
        public TrackerBlockerService TrackerBlocker => _trackerBlocker;

        public bool IsSidebarOpen { get; set; } = true;
        public string ActiveSidebarTab { get; set; } = "favorites"; // favorites, readinglist, notes, history, privacy, passwords
        public string AddressBarInput { get; set; } = string.Empty;

        public BrowserState(WebFetcherService fetcher, ReaderEngine readerEngine, TrackerBlockerService trackerBlocker)
        {
            _fetcher = fetcher;
            _readerEngine = readerEngine;
            _trackerBlocker = trackerBlocker;

            // Seed initial state
            SeedInitialState();

            // Create initial default tab
            var initialTab = new TabModel
            {
                Title = "Start Page",
                Url = "about:start",
                DisplayUrl = "",
                Favicon = "🧭",
                IsActive = true
            };
            Tabs.Add(initialTab);
            ActiveTab = initialTab;
        }

        private void SeedInitialState()
        {
            // Initial Bookmarks / Favorites
            Bookmarks.Add(new BookmarkModel { Title = "Apple", Url = "apple.com", Icon = "", Category = "Favorites" });
            Bookmarks.Add(new BookmarkModel { Title = "Tech News", Url = "techcrunch.com", Icon = "📰", Category = "Favorites" });
            Bookmarks.Add(new BookmarkModel { Title = "Wikipedia", Url = "wikipedia.org", Icon = "🌐", Category = "Favorites" });
            Bookmarks.Add(new BookmarkModel { Title = "GitHub", Url = "github.com", Icon = "💻", Category = "Favorites" });

            // Initial Reading List
            ReadingList.Add(new ReadingListItem
            {
                Title = "Apple Vision Pro: Welcome to Spatial Computing",
                Url = "apple.com",
                Domain = "apple.com",
                PreviewText = "Apple Vision Pro seamlessly blends digital content with your physical space.",
                IsRead = false
            });
            ReadingList.Add(new ReadingListItem
            {
                Title = "The Evolution of Modern Browsers in C# and .NET 10",
                Url = "techcrunch.com",
                Domain = "techcrunch.com",
                PreviewText = "Web browsers are undergoing a major shift toward glassmorphic UI designs and privacy-first architectures.",
                IsRead = true
            });

            // Initial Quick Notes
            QuickNotes.Add(new QuickNote
            {
                Title = "Safari Plus Ideas",
                Content = "Features to test: Reader Mode, Glassmorphic top bar, Tracker protection report, and local Password Vault.",
                SourceUrl = "about:start",
                Color = "#e3f2fd"
            });

            // Initial History
            History.Add(new HistoryItem { Title = "Start Page", Url = "about:start", VisitedAt = DateTime.Now.AddMinutes(-10) });
            History.Add(new HistoryItem { Title = "Apple - Vision Pro & Mac", Url = "apple.com", VisitedAt = DateTime.Now.AddMinutes(-5) });

            // Initial Credentials
            Passwords.Add(new PasswordCredential { Website = "apple.com", Username = "user@icloud.com", Password = "••••••••••••" });
            Passwords.Add(new PasswordCredential { Website = "github.com", Username = "developer", Password = "••••••••••••" });
        }

        public async Task NavigateToUrlAsync(string inputUrl)
        {
            if (string.IsNullOrWhiteSpace(inputUrl)) return;

            string targetUrl = inputUrl.Trim();

            // Treat search queries vs URLs
            if (!targetUrl.Contains('.') && !targetUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase) && !targetUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                targetUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(targetUrl)}";
            }

            // Strip tracking query params
            targetUrl = _trackerBlocker.StripTrackingParams(targetUrl);

            ActiveTab.IsLoading = true;
            ActiveTab.Url = targetUrl;
            ActiveTab.DisplayUrl = targetUrl;
            AddressBarInput = targetUrl;
            NotifyStateChanged();

            var (title, html, isSuccess, errorMsg) = await _fetcher.FetchWebPageAsync(targetUrl);

            ActiveTab.IsLoading = false;
            ActiveTab.RawHtml = html;
            ActiveTab.Title = title;
            ActiveTab.Favicon = GetFaviconForUrl(targetUrl);

            // Analyze trackers in URL & content
            var (detectedTrackers, count) = _trackerBlocker.AnalyzeAndFilterUrl(targetUrl);
            int contentTrackers = _trackerBlocker.FilterHtmlContent(html, out _);
            ActiveTab.TrackersBlocked = count + contentTrackers;
            ActiveTab.TrackersDetected = detectedTrackers;

            // Extract Reader Mode
            var readerResult = _readerEngine.ExtractReaderContent(html, targetUrl);
            ActiveTab.IsReaderAvailable = readerResult.IsSuccess;
            if (readerResult.IsSuccess)
            {
                ActiveTab.ReaderTitle = readerResult.Title;
                ActiveTab.ReaderByline = readerResult.Byline;
                ActiveTab.ReaderContentHtml = readerResult.ContentHtml;
            }

            // Record History
            RecordHistory(title, targetUrl);

            NotifyStateChanged();
        }

        public void AddTab(string url = "about:start")
        {
            foreach (var t in Tabs) t.IsActive = false;

            var newTab = new TabModel
            {
                Title = "New Tab",
                Url = url,
                DisplayUrl = url == "about:start" ? "" : url,
                Favicon = "🧭",
                IsActive = true
            };

            Tabs.Add(newTab);
            ActiveTab = newTab;
            AddressBarInput = newTab.DisplayUrl;

            if (url != "about:start")
            {
                _ = NavigateToUrlAsync(url);
            }
            else
            {
                NotifyStateChanged();
            }
        }

        public void SelectTab(TabModel tab)
        {
            foreach (var t in Tabs) t.IsActive = false;
            tab.IsActive = true;
            ActiveTab = tab;
            AddressBarInput = tab.Url == "about:start" ? "" : tab.Url;
            NotifyStateChanged();
        }

        public void CloseTab(TabModel tab)
        {
            if (Tabs.Count <= 1)
            {
                // Reset sole tab to start page
                tab.Title = "Start Page";
                tab.Url = "about:start";
                tab.DisplayUrl = "";
                tab.IsReaderMode = false;
                tab.RawHtml = "";
                AddressBarInput = "";
                NotifyStateChanged();
                return;
            }

            int index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);

            if (tab.IsActive)
            {
                int nextIdx = Math.Min(index, Tabs.Count - 1);
                SelectTab(Tabs[nextIdx]);
            }
            else
            {
                NotifyStateChanged();
            }
        }

        public void DuplicateTab(TabModel tab)
        {
            var dup = new TabModel
            {
                Title = tab.Title,
                Url = tab.Url,
                DisplayUrl = tab.DisplayUrl,
                Favicon = tab.Favicon,
                RawHtml = tab.RawHtml,
                IsReaderAvailable = tab.IsReaderAvailable,
                ReaderTitle = tab.ReaderTitle,
                ReaderByline = tab.ReaderByline,
                ReaderContentHtml = tab.ReaderContentHtml,
                TrackersBlocked = tab.TrackersBlocked
            };

            Tabs.Add(dup);
            SelectTab(dup);
        }

        public void TogglePinTab(TabModel tab)
        {
            tab.IsPinned = !tab.IsPinned;
            NotifyStateChanged();
        }

        public void ToggleReaderMode()
        {
            if (ActiveTab.IsReaderAvailable)
            {
                ActiveTab.IsReaderMode = !ActiveTab.IsReaderMode;
                NotifyStateChanged();
            }
        }

        public void AddToReadingList(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!ReadingList.Any(r => r.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                ReadingList.Add(new ReadingListItem
                {
                    Title = string.IsNullOrWhiteSpace(title) ? url : title,
                    Url = url,
                    Domain = _fetcher.ExtractDomain(url),
                    PreviewText = ActiveTab.ReaderTitle ?? "Saved article from " + url,
                    IsRead = false
                });
                NotifyStateChanged();
            }
        }

        public void RemoveFromReadingList(string id)
        {
            ReadingList.RemoveAll(r => r.Id == id);
            NotifyStateChanged();
        }

        public void ToggleReadingListRead(string id)
        {
            var item = ReadingList.FirstOrDefault(r => r.Id == id);
            if (item != null)
            {
                item.IsRead = !item.IsRead;
                NotifyStateChanged();
            }
        }

        public void AddQuickNote(string title, string content, string sourceUrl = "", string color = "#fff9c4")
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content)) return;

            QuickNotes.Add(new QuickNote
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Web Snippet" : title,
                Content = content,
                SourceUrl = sourceUrl,
                Color = color
            });
            NotifyStateChanged();
        }

        public void DeleteQuickNote(string id)
        {
            QuickNotes.RemoveAll(n => n.Id == id);
            NotifyStateChanged();
        }

        public void AddBookmark(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!Bookmarks.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                Bookmarks.Add(new BookmarkModel
                {
                    Title = string.IsNullOrWhiteSpace(title) ? url : title,
                    Url = url,
                    Icon = GetFaviconForUrl(url),
                    Category = "Favorites"
                });
                NotifyStateChanged();
            }
        }

        public void RemoveBookmark(string id)
        {
            Bookmarks.RemoveAll(b => b.Id == id);
            NotifyStateChanged();
        }

        public void SavePasswordCredential(string website, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(website)) return;
            Passwords.Add(new PasswordCredential
            {
                Website = website,
                Username = username,
                Password = password
            });
            NotifyStateChanged();
        }

        public void DeletePasswordCredential(string id)
        {
            Passwords.RemoveAll(p => p.Id == id);
            NotifyStateChanged();
        }

        private void RecordHistory(string title, string url)
        {
            if (url == "about:start") return;
            History.Insert(0, new HistoryItem
            {
                Title = title,
                Url = url,
                VisitedAt = DateTime.Now
            });

            if (History.Count > 100)
            {
                History.RemoveAt(History.Count - 1);
            }
        }

        private string GetFaviconForUrl(string url)
        {
            if (url.Contains("apple.com", StringComparison.OrdinalIgnoreCase)) return "";
            if (url.Contains("techcrunch.com", StringComparison.OrdinalIgnoreCase)) return "📰";
            if (url.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase)) return "🌐";
            if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase)) return "💻";
            if (url.Contains("google.com", StringComparison.OrdinalIgnoreCase)) return "🔍";
            return "📄";
        }

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
