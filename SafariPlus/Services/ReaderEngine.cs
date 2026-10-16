using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace SafariPlus.Services
{
    public class ReaderResult
    {
        public bool IsSuccess { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Byline { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public string TextSummary { get; set; } = string.Empty;
        public int WordCount { get; set; } = 0;
        public int EstimatedReadingTimeMinutes { get; set; } = 0;
    }

    public class ReaderEngine
    {
        public ReaderResult ExtractReaderContent(string html, string url)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return new ReaderResult { IsSuccess = false };
            }

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract title & byline BEFORE stripping attributes/cleaning
                string title = ExtractTitle(doc);
                string byline = ExtractByline(doc);

                // Remove unwanted tags: script, style, iframe, nav, footer, header, form, svg, ads
                RemoveUnwantedNodes(doc);

                // Find main article container node
                HtmlNode? mainNode = FindMainContentNode(doc);

                if (mainNode == null)
                {
                    return new ReaderResult { IsSuccess = false };
                }

                // Clean extracted HTML
                CleanContentNode(mainNode);

                string contentHtml = mainNode.InnerHtml;
                string plainText = mainNode.InnerText;
                plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

                int wordCount = plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                int readingTime = (int)Math.Ceiling(wordCount / 200.0);

                return new ReaderResult
                {
                    IsSuccess = !string.IsNullOrWhiteSpace(plainText) && wordCount > 30,
                    Title = string.IsNullOrWhiteSpace(title) ? "Article View" : title,
                    Byline = byline,
                    ContentHtml = contentHtml,
                    TextSummary = plainText.Length > 250 ? plainText.Substring(0, 250) + "..." : plainText,
                    WordCount = wordCount,
                    EstimatedReadingTimeMinutes = readingTime > 0 ? readingTime : 1
                };
            }
            catch
            {
                return new ReaderResult { IsSuccess = false };
            }
        }

        private string ExtractTitle(HtmlDocument doc)
        {
            var h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(h1)) return HtmlEntity.DeEntitize(h1);

            var titleNode = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(titleNode)) return HtmlEntity.DeEntitize(titleNode);

            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(ogTitle)) return HtmlEntity.DeEntitize(ogTitle);

            return "Web Article";
        }

        private string ExtractByline(HtmlDocument doc)
        {
            var authorMeta = doc.DocumentNode.SelectSingleNode("//meta[@name='author']")?.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(authorMeta)) return $"By {authorMeta}";

            var bylineNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'byline') or contains(@class, 'author')]");
            if (bylineNode != null && !string.IsNullOrWhiteSpace(bylineNode.InnerText))
            {
                return HtmlEntity.DeEntitize(bylineNode.InnerText.Trim());
            }

            return "Safari Reader";
        }

        private void RemoveUnwantedNodes(HtmlDocument doc)
        {
            string[] tagsToRemove = { "script", "style", "noscript", "iframe", "svg", "nav", "footer", "header", "form", "aside" };
            foreach (var tag in tagsToRemove)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
                if (nodes != null)
                {
                    foreach (var node in nodes.ToList())
                    {
                        node.Remove();
                    }
                }
            }

            // Remove ads/sidebar elements by class or id pattern
            var adNodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'ad') or contains(@class, 'sidebar') or contains(@class, 'comments') or contains(@id, 'banner') or contains(@class, 'social')]");
            if (adNodes != null)
            {
                foreach (var node in adNodes.ToList())
                {
                    if (node.Name != "article" && node.Name != "main" && node.Name != "body")
                    {
                        node.Remove();
                    }
                }
            }
        }

        private HtmlNode? FindMainContentNode(HtmlDocument doc)
        {
            // 1. Check <article>
            var article = doc.DocumentNode.SelectSingleNode("//article");
            if (article != null && article.InnerText.Length > 200) return article;

            // 2. Check <main>
            var main = doc.DocumentNode.SelectSingleNode("//main");
            if (main != null && main.InnerText.Length > 200) return main;

            // 3. Find div or section with highest text density/length
            var contentCandidates = doc.DocumentNode.SelectNodes("//div | //section");
            if (contentCandidates != null)
            {
                HtmlNode? bestCandidate = null;
                int maxTextLength = 0;

                foreach (var candidate in contentCandidates)
                {
                    string text = candidate.InnerText.Trim();
                    if (text.Length > maxTextLength)
                    {
                        maxTextLength = text.Length;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate != null && maxTextLength > 150)
                {
                    return bestCandidate;
                }
            }

            return doc.DocumentNode.SelectSingleNode("//body");
        }

        private void CleanContentNode(HtmlNode node)
        {
            // Clean inline style and event attributes
            var allNodes = node.SelectNodes(".//*");
            if (allNodes != null)
            {
                foreach (var child in allNodes)
                {
                    child.Attributes.Remove("style");
                    child.Attributes.Remove("onclick");
                    child.Attributes.Remove("onload");
                    child.Attributes.Remove("class");
                    child.Attributes.Remove("id");
                }
            }
        }
    }
}
