using System.Collections.Generic;

using SourceGit.Models;
using SourceGit.ViewModels;

using Xunit;

namespace SourceGit.Tests
{
    public class ComputeBodyHeightTests
    {
        [Fact]
        public void NullContent_ReturnsNull()
        {
            Assert.Null(FileDiff.ComputeBodyHeight(null));
        }

        [Fact]
        public void NonTextContent_GetsFixedHeight()
        {
            var decision = FileDiff.ComputeBodyHeight(new BinaryDiff());

            Assert.NotNull(decision);
            Assert.False(decision.Value.IsBounded);
            Assert.Equal(FileDiff.NonTextBodyHeight, decision.Value.MaxBodyHeight);
        }

        [Fact]
        public void CombinedDiff_AtLimit_IsUnbounded()
        {
            var diff = new CombinedTextDiff(MakeOption(), MakeTextDiff(FileDiff.MaxFullExpandLines));

            var decision = FileDiff.ComputeBodyHeight(diff);

            Assert.NotNull(decision);
            Assert.False(decision.Value.IsBounded);
            Assert.Equal(double.PositiveInfinity, decision.Value.MaxBodyHeight);
        }

        [Fact]
        public void CombinedDiff_OverLimit_IsBounded()
        {
            var diff = new CombinedTextDiff(MakeOption(), MakeTextDiff(FileDiff.MaxFullExpandLines + 1));

            var decision = FileDiff.ComputeBodyHeight(diff);

            Assert.NotNull(decision);
            Assert.True(decision.Value.IsBounded);
            Assert.Equal(FileDiff.BoundedBodyHeight, decision.Value.MaxBodyHeight);
        }

        // The two-side view must bound on max(Old, New), not the total line count,
        // so 501 one-sided lines trip the limit whichever side they land on.
        [Theory]
        [InlineData(TextDiffLineType.Deleted)]
        [InlineData(TextDiffLineType.Added)]
        public void TwoSideDiff_UsesLongerSide(TextDiffLineType lineType)
        {
            var textDiff = new TextDiff();
            for (var i = 0; i < FileDiff.MaxFullExpandLines + 1; i++)
            {
                var oldLine = lineType == TextDiffLineType.Deleted ? i + 1 : 0;
                var newLine = lineType == TextDiffLineType.Added ? i + 1 : 0;
                textDiff.Lines.Add(new TextDiffLine(lineType, $"line {i}", [], oldLine, newLine));
            }

            var diff = new TwoSideTextDiff(MakeOption(), textDiff);

            var decision = FileDiff.ComputeBodyHeight(diff);

            Assert.NotNull(decision);
            Assert.True(decision.Value.IsBounded);
        }

        private static DiffOption MakeOption()
        {
            var change = new Change { Path = "file.txt" };
            change.Set(ChangeState.None, ChangeState.Modified);
            return new DiffOption(change, true);
        }

        private static TextDiff MakeTextDiff(int lineCount)
        {
            var diff = new TextDiff();
            for (var i = 0; i < lineCount; i++)
                diff.Lines.Add(new TextDiffLine(TextDiffLineType.Added, $"line {i}", [], 0, i + 1));
            return diff;
        }
    }

    public class MultipleDiffContextTests
    {
        [Fact]
        public void Truncates_At_MaxFiles_And_Reports_Remainder()
        {
            var ctx = new MultipleDiffContext(null, MakeChanges(150, unstaged: true));

            Assert.Equal(100, ctx.Files.Count);
            Assert.Equal(50, ctx.MoreCount);
            Assert.True(ctx.HasMore);
        }

        [Fact]
        public void SmallSets_Have_No_Remainder()
        {
            var ctx = new MultipleDiffContext(null, MakeChanges(15, unstaged: true));

            Assert.Equal(15, ctx.Files.Count);
            Assert.Equal(0, ctx.MoreCount);
            Assert.False(ctx.HasMore);
        }

        [Fact]
        public void Above_AutoExpand_Limit_Files_Start_Collapsed()
        {
            var ctx = new MultipleDiffContext(null, MakeChanges(11, unstaged: true));

            Assert.All(ctx.Files, f => Assert.False(f.IsExpanded));
        }

        [Fact]
        public void Mixed_Groups_Mark_First_Of_Each_Section()
        {
            var changes = MakeChanges(6, unstaged: true);
            changes.AddRange(MakeChanges(6, unstaged: false));

            var ctx = new MultipleDiffContext(null, changes);

            Assert.True(ctx.Files[0].ShowUnstagedGroupHeader);
            Assert.True(ctx.Files[6].ShowStagedGroupHeader);
            Assert.Single(ctx.Files.FindAll(f => f.ShowUnstagedGroupHeader));
            Assert.Single(ctx.Files.FindAll(f => f.ShowStagedGroupHeader));
        }

        [Fact]
        public void Unmixed_Set_Gets_No_Section_Headers()
        {
            var ctx = new MultipleDiffContext(null, MakeChanges(12, unstaged: true));

            Assert.All(ctx.Files, f =>
            {
                Assert.False(f.ShowUnstagedGroupHeader);
                Assert.False(f.ShowStagedGroupHeader);
            });
        }

        [Fact]
        public void Renamed_File_Title_Shows_Both_Paths()
        {
            var change = new Change { Path = "old.txt\tnew.txt" };
            change.Set(ChangeState.Renamed);

            var ctx = new MultipleDiffContext(null, [(change, false), .. MakeChanges(11, unstaged: false)]);

            Assert.Equal("old.txt → new.txt", ctx.Files[0].Title);
        }

        // Above MaxAutoExpandFiles nothing expands, so a null owner is never dereferenced.
        private static List<(Change, bool)> MakeChanges(int count, bool unstaged)
        {
            var changes = new List<(Change, bool)>();
            for (var i = 0; i < count; i++)
            {
                var change = new Change { Path = $"dir/file_{i:D3}.txt" };
                if (unstaged)
                    change.Set(ChangeState.None, ChangeState.Modified);
                else
                    change.Set(ChangeState.Modified);
                changes.Add((change, unstaged));
            }
            return changes;
        }
    }
}
