using System;
using System.Collections.Generic;

namespace SafariPlus.Models
{
    public class TabModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "New Tab";
        public string Url { get; set; } = "about:start";
        public string DisplayUrl { get; set; } = "about:start";
        public string Favicon { get; set; } = "🧭";
        public bool IsActive { get; set; } = false;
        public bool IsPinned { get; set; } = false;
        public bool IsLoading { get; set; } = false;
        public bool IsReaderAvailable { get; set; } = false;
        public bool IsReaderMode { get; set; } = false;
        public string RawHtml { get; set; } = string.Empty;
        public string ReaderTitle { get; set; } = string.Empty;
        public string ReaderByline { get; set; } = string.Empty;
        public string ReaderContentHtml { get; set; } = string.Empty;
        public int TrackersBlocked { get; set; } = 0;
        public List<string> TrackersDetected { get; set; } = new();
        public DateTime LoadedAt { get; set; } = DateTime.Now;
    }

    public class BookmarkModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "⭐";
        public string Category { get; set; } = "Favorites";
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class ReadingListItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class QuickNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string Color { get; set; } = "#fff9c4"; // yellow sticky note
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class HistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime VisitedAt { get; set; } = DateTime.Now;
    }

    public class TrackerInfo
    {
        public string Domain { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Analytics, Social, AdNetwork, Fingerprinting
        public int TimesBlocked { get; set; } = 0;
        public DateTime LastBlocked { get; set; } = DateTime.Now;
    }

    public class ReaderThemeSettings
    {
        public string Theme { get; set; } = "light"; // light, sepia, dark, night
        public int FontSize { get; set; } = 18; // px
        public string FontFamily { get; set; } = "San Francisco, -apple-system, Georgia, serif";
    }

    public class PasswordCredential
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Website { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}
