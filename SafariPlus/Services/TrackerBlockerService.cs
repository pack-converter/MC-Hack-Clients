using System;
using System.Collections.Generic;
using System.Linq;
using SafariPlus.Models;

namespace SafariPlus.Services
{
    public class TrackerBlockerService
    {
        private readonly List<TrackerInfo> _blockedTrackersHistory = new();
        private readonly HashSet<string> _knownTrackerDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "doubleclick.net",
            "google-analytics.com",
            "facebook.com/tr",
            "googlesyndication.com",
            "adservice.google.com",
            "scorecardresearch.com",
            "criteo.com",
            "quantserve.com",
            "outbrain.com",
            "taboola.com",
            "adnxs.com",
            "amazon-adsystem.com",
            "hotjar.com",
            "mixpanel.com",
            "segment.io",
            "clarity.ms",
            "trackersimulation.com",
            "adserver.net"
        };

        public TrackerBlockerService()
        {
            // Seed initial realistic Intelligent Tracking Protection data
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            RecordBlockedTracker("doubleclick.net", "Advertising");
            RecordBlockedTracker("doubleclick.net", "Advertising");
            RecordBlockedTracker("google-analytics.com", "Analytics");
            RecordBlockedTracker("facebook.com/tr", "Social Tracker");
            RecordBlockedTracker("hotjar.com", "Session Recorder");
            RecordBlockedTracker("criteo.com", "Retargeting Ad");
            RecordBlockedTracker("outbrain.com", "Content Recommendation Ad");
        }

        public (List<string> DetectedTrackers, int Count) AnalyzeAndFilterUrl(string url)
        {
            var detected = new List<string>();

            if (string.IsNullOrWhiteSpace(url)) return (detected, 0);

            foreach (var trackerDomain in _knownTrackerDomains)
            {
                if (url.Contains(trackerDomain, StringComparison.OrdinalIgnoreCase))
                {
                    detected.Add(trackerDomain);
                    RecordBlockedTracker(trackerDomain, DetermineCategory(trackerDomain));
                }
            }

            return (detected, detected.Count);
        }

        public int FilterHtmlContent(string html, out List<string> blockedDomains)
        {
            blockedDomains = new List<string>();
            if (string.IsNullOrWhiteSpace(html)) return 0;

            int count = 0;
            foreach (var tracker in _knownTrackerDomains)
            {
                if (html.Contains(tracker, StringComparison.OrdinalIgnoreCase))
                {
                    blockedDomains.Add(tracker);
                    RecordBlockedTracker(tracker, DetermineCategory(tracker));
                    count++;
                }
            }

            return count;
        }

        public void RecordBlockedTracker(string domain, string category)
        {
            lock (_blockedTrackersHistory)
            {
                var existing = _blockedTrackersHistory.FirstOrDefault(t => t.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.TimesBlocked++;
                    existing.LastBlocked = DateTime.Now;
                }
                else
                {
                    _blockedTrackersHistory.Add(new TrackerInfo
                    {
                        Domain = domain,
                        Category = category,
                        TimesBlocked = 1,
                        LastBlocked = DateTime.Now
                    });
                }
            }
        }

        public string StripTrackingParams(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            string[] trackingParams = { "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "fbclid", "gclid", "msclkid" };
            string resultUrl = url;

            foreach (var param in trackingParams)
            {
                if (resultUrl.Contains($"{param}="))
                {
                    // Clean parameter
                    int idx = resultUrl.IndexOf($"{param}=");
                    if (idx > 0)
                    {
                        int endIdx = resultUrl.IndexOf('&', idx);
                        if (endIdx != -1)
                        {
                            resultUrl = resultUrl.Remove(idx, endIdx - idx + 1);
                        }
                        else
                        {
                            // if it was preceding with ? or &
                            if (idx > 0 && (resultUrl[idx - 1] == '?' || resultUrl[idx - 1] == '&'))
                            {
                                resultUrl = resultUrl.Substring(0, idx - 1);
                            }
                        }
                    }
                }
            }

            return resultUrl;
        }

        private string DetermineCategory(string domain)
        {
            if (domain.Contains("analytics") || domain.Contains("mixpanel") || domain.Contains("hotjar") || domain.Contains("segment"))
                return "Analytics & Behavior";
            if (domain.Contains("facebook") || domain.Contains("social"))
                return "Social Media Fingerprinting";
            if (domain.Contains("ad") || domain.Contains("criteo") || domain.Contains("outbrain") || domain.Contains("doubleclick"))
                return "Cross-Site Advertising";

            return "Tracker & Telemetry";
        }

        public List<TrackerInfo> GetBlockedTrackersSummary()
        {
            lock (_blockedTrackersHistory)
            {
                return _blockedTrackersHistory.OrderByDescending(t => t.TimesBlocked).ToList();
            }
        }

        public int TotalBlockedCount => _blockedTrackersHistory.Sum(t => t.TimesBlocked);
    }
}
