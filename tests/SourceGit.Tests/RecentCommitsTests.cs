using System.Collections.Generic;

using SourceGit.Models;

using Xunit;

namespace SourceGit.Tests
{
    public class RecentCommitsTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(20, 20)]
        [InlineData(150, 20)]
        public void Cap_Preserves_Order_And_Limits_Count(int total, int expected)
        {
            var commits = new List<Commit>();
            for (var i = 0; i < total; i++)
                commits.Add(new Commit { SHA = $"sha{i:D4}", Subject = $"commit {i}" });

            var recent = ViewModels.Repository.BuildRecentCommits(commits);

            Assert.Equal(expected, recent.Count);
            for (var i = 0; i < recent.Count; i++)
                Assert.Equal($"sha{i:D4}", recent[i].SHA);
        }
    }
}
