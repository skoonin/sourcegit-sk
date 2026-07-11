using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SourceGit.Models
{
    public class Version
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonIgnore]
        public System.Version CurrentVersion { get; }

        [JsonIgnore]
        public string CurrentVersionStr => $"v{CurrentVersion.Major}.{CurrentVersion.Minor:D2}";

        [JsonIgnore]
        public bool IsNewVersion
        {
            get
            {
                // Fork tags look like v2026.15-sk; only the numeric part is comparable.
                var tag = TagName.Substring(1);
                var endOfNumeric = tag.IndexOf('-');
                if (endOfNumeric > 0)
                    tag = tag.Substring(0, endOfNumeric);

                return System.Version.TryParse(tag, out var latest) && CurrentVersion.CompareTo(latest) < 0;
            }
        }

        [JsonIgnore]
        public string ReleaseDateStr => DateTimeFormat.Format(PublishedAt, true);

        public Version()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            CurrentVersion = assembly.Version ?? new System.Version();
        }
    }

    public class AlreadyUpToDate;

    public class SelfUpdateFailed
    {
        public string Reason
        {
            get;
            private set;
        }

        public SelfUpdateFailed(Exception e)
        {
            if (e.InnerException is { } inner)
                Reason = inner.Message;
            else
                Reason = e.Message;
        }
    }
}
