using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SafariPlus.Models;
using SafariPlus.Services;
using Xunit;

namespace SafariPlus.Tests
{
    public class ReaderEngineTests
    {
        [Fact]
        public void ExtractReaderContent_ValidArticleHtml_ReturnsSuccessAndExtractedData()
        {
            var engine = new ReaderEngine();
            string sampleHtml = @"
                <!DOCTYPE html>
                <html>
                <head><title>Test Tech Article</title></head>
                <body>
                    <nav>Navigation links to ignore</nav>
                    <article>
                        <h1>Revolution in Modern Web Browsers</h1>
                        <p class='byline'>By Jane Developer</p>
                        <p>Modern web browsers are evolving with glassmorphic designs, unified search bars, and intelligent tracking protection engines built using .NET 10 and Blazor interactive components.</p>
                        <p>With Safari Plus, developers can experience rich reading lists, web note-taking snippets, and instant distraction-free article modes built on C#.</p>
                    </article>
                    <script src='https://doubleclick.net/tracker.js'></script>
                </body>
                </html>";

            var result = engine.ExtractReaderContent(sampleHtml, "https://example.com/article");

            Assert.True(result.IsSuccess);
            Assert.Contains("Revolution in Modern Web Browsers", result.Title);
            Assert.Contains("Jane Developer", result.Byline);
            Assert.Contains("glassmorphic designs", result.ContentHtml);
            Assert.True(result.WordCount > 20);
        }

        [Fact]
        public void ExtractReaderContent_EmptyHtml_ReturnsFailure()
        {
            var engine = new ReaderEngine();
            var result = engine.ExtractReaderContent("", "https://example.com");

            Assert.False(result.IsSuccess);
        }
    }

    public class TrackerBlockerServiceTests
    {
        [Fact]
        public void StripTrackingParams_UrlWithUtmAndFbclid_StripsParameters()
        {
            var blocker = new TrackerBlockerService();
            string dirtyUrl = "https://example.com/page?utm_source=twitter&fbclid=123456";

            string cleanUrl = blocker.StripTrackingParams(dirtyUrl);

            Assert.DoesNotContain("utm_source", cleanUrl);
            Assert.DoesNotContain("fbclid", cleanUrl);
        }

        [Fact]
        public void AnalyzeAndFilterUrl_UrlWithKnownTracker_DetectsAndIncrementsCount()
        {
            var blocker = new TrackerBlockerService();
            string trackerUrl = "https://example.com/script?ref=google-analytics.com";

            var (detected, count) = blocker.AnalyzeAndFilterUrl(trackerUrl);

            Assert.True(count > 0);
            Assert.Contains("google-analytics.com", detected);
        }
    }

    public class BrowserStateTests
    {
        private BrowserState CreateBrowserState()
        {
            var http = new HttpClient();
            var fetcher = new WebFetcherService(http);
            var readerEngine = new ReaderEngine();
            var trackerBlocker = new TrackerBlockerService();
            return new BrowserState(fetcher, readerEngine, trackerBlocker);
        }

        [Fact]
        public void AddTab_NewTabCreated_ActiveTabSet()
        {
            var state = CreateBrowserState();
            int initialCount = state.Tabs.Count;

            state.AddTab("about:start");

            Assert.Equal(initialCount + 1, state.Tabs.Count);
            Assert.Equal("about:start", state.ActiveTab.Url);
        }

        [Fact]
        public void CloseTab_MultipleTabs_ClosesActiveTabAndActivatesAdjacent()
        {
            var state = CreateBrowserState();
            state.AddTab("apple.com");
            state.AddTab("wikipedia.org");

            var tabToClose = state.ActiveTab;
            state.CloseTab(tabToClose);

            Assert.DoesNotContain(tabToClose, state.Tabs);
            Assert.NotNull(state.ActiveTab);
        }

        [Fact]
        public void AddBookmark_ValidUrl_AddsToBookmarksList()
        {
            var state = CreateBrowserState();
            string url = "https://github.com";

            state.AddBookmark("GitHub", url);

            Assert.Contains(state.Bookmarks, b => b.Url == url);
        }

        [Fact]
        public void AddQuickNote_ValidNote_AddsToQuickNotes()
        {
            var state = CreateBrowserState();

            state.AddQuickNote("Test Note", "This is a quick test note content.", "apple.com");

            Assert.Contains(state.QuickNotes, n => n.Title == "Test Note");
        }
    }
}
