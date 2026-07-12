using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    // Builds the detail view-model (DiffContext, Conflict, ...) for one change in a stacked diff.
    // This is the only per-source variation point: each consumer (working copy, commit, compare,
    // stash) supplies how a single file's diff is constructed; everything else is shared.
    public delegate object FileDiffDetailFactory(Models.Change change, bool isUnstaged, DiffContext previous);

    public class FileDiff : ObservableObject
    {
        public Models.Change Change
        {
            get;
        }

        public bool IsUnstaged
        {
            get;
        }

        public string Title
        {
            get;
        }

        public bool ShowUnstagedGroupHeader
        {
            get;
            internal set;
        }

        public bool ShowStagedGroupHeader
        {
            get;
            internal set;
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value) && value && _detail == null)
                    CreateDetail(null);
            }
        }

        public object Detail
        {
            get => _detail;
        }

        public bool IsBounded
        {
            get => _isBounded;
            private set => SetProperty(ref _isBounded, value);
        }

        public double MaxBodyHeight
        {
            get => _maxBodyHeight;
            private set => SetProperty(ref _maxBodyHeight, value);
        }

        public FileDiff(FileDiffDetailFactory createDetail, Models.Change change, bool isUnstaged, bool autoExpand, FileDiff previous)
        {
            _createDetail = createDetail;
            Change = change;
            IsUnstaged = isUnstaged;

            if (string.IsNullOrEmpty(change.OriginalPath))
                Title = change.Path;
            else
                Title = $"{change.OriginalPath} → {change.Path}";

            _isExpanded = previous?._isExpanded ?? autoExpand;
            if (_isExpanded)
                CreateDetail(previous?._detail as DiffContext);
        }

        private void CreateDetail(DiffContext previous)
        {
            var detail = _createDetail(Change, IsUnstaged, previous);
            if (detail is DiffContext diff)
            {
                diff.PropertyChanged += OnDiffPropertyChanged;
                _detail = diff;
                UpdateBodyHeight(diff);
            }
            else
            {
                // Non-text details (conflict editor, ...) manage their own height.
                _detail = detail;
                MaxBodyHeight = double.PositiveInfinity;
            }

            OnPropertyChanged(nameof(Detail));
        }

        internal readonly record struct BodyHeightDecision(bool IsBounded, double MaxBodyHeight);

        internal static BodyHeightDecision? ComputeBodyHeight(object content)
        {
            if (content is TextDiffContext text)
            {
                var lines = text is TwoSideTextDiff twoSide ? Math.Max(twoSide.Old.Count, twoSide.New.Count) : text.Data.Lines.Count;
                var bounded = lines > MaxFullExpandLines;
                return new BodyHeightDecision(bounded, bounded ? BoundedBodyHeight : double.PositiveInfinity);
            }

            if (content != null)
                return new BodyHeightDecision(false, NonTextBodyHeight);

            return null;
        }

        private void OnDiffPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiffContext.Content) && sender is DiffContext diff)
                UpdateBodyHeight(diff);
        }

        private void UpdateBodyHeight(DiffContext diff)
        {
            if (ComputeBodyHeight(diff.Content) is { } decision)
            {
                IsBounded = decision.IsBounded;
                MaxBodyHeight = decision.MaxBodyHeight;
            }
        }

        // Full expansion above this line count would defeat AvaloniaEdit's viewport virtualization.
        internal const int MaxFullExpandLines = 500;
        internal const double BoundedBodyHeight = 600;
        // Image/binary/submodule diffs have no line count; give them a fixed, readable pane height.
        internal const double NonTextBodyHeight = 400;

        private readonly FileDiffDetailFactory _createDetail;
        private bool _isExpanded;
        private object _detail = null;
        private bool _isBounded;
        private double _maxBodyHeight = BoundedBodyHeight;
    }

    public class MultipleDiffContext : ObservableObject
    {
        public List<FileDiff> Files
        {
            get;
        } = [];

        public int MoreCount
        {
            get;
        }

        public bool HasMore
        {
            get => MoreCount > 0;
        }

        public void CheckSettings()
        {
            foreach (var file in Files)
            {
                if (file.Detail is DiffContext diff)
                    diff.CheckSettings();
            }
        }

        public void IncrUnified()
        {
            foreach (var file in Files)
            {
                if (file.Detail is DiffContext diff)
                    diff.IncrUnified();
            }
        }

        public void DecrUnified()
        {
            foreach (var file in Files)
            {
                if (file.Detail is DiffContext diff)
                    diff.DecrUnified();
            }
        }

        // Shared selection-to-detail dispatch for read-only change sets (commits, compares, stashes):
        // one selected file shows its own diff, several show a stacked diff of the selection, and no
        // selection shows the whole visible set stacked. Consumers supply only how a change maps to a
        // DiffOption. The working copy keeps its own dispatcher (dual staged/unstaged lists, conflicts).
        public static object BuildDetail(string repoPath, Func<Models.Change, Models.DiffOption> makeOption, List<Models.Change> selected, List<Models.Change> visible, object previous)
        {
            // A lone selected file honors the global `UseFullTextDiff` preference; stacked files must
            // ignore it so one toggle cannot materialize every file at once.
            if (selected is { Count: 1 })
                return new DiffContext(repoPath, makeOption(selected[0]), previous as DiffContext);

            FileDiffDetailFactory factory = (change, _, prev) => new DiffContext(repoPath, makeOption(change), prev, true);

            if (selected is { Count: > 1 })
                return CreateFromSelection(factory, selected, previous);

            if (visible is { Count: > 0 })
                return CreateFromOrdered(factory, visible, previous);

            return null;
        }

        // Stack an explicit selection; click order is normalized back to list order.
        public static MultipleDiffContext CreateFromSelection(FileDiffDetailFactory createDetail, List<Models.Change> changes, object previous)
        {
            var sorted = new List<Models.Change>(changes);
            sorted.Sort((l, r) => Models.NumericSort.Compare(l.Path, r.Path));
            return CreateFromOrdered(createDetail, sorted, previous);
        }

        // Stack a list that is already in display order (e.g. the whole visible change set).
        public static MultipleDiffContext CreateFromOrdered(FileDiffDetailFactory createDetail, List<Models.Change> changes, object previous)
        {
            var files = new List<(Models.Change, bool)>(changes.Count);
            foreach (var c in changes)
                files.Add((c, false));

            return new MultipleDiffContext(createDetail, files, previous as MultipleDiffContext);
        }

        public MultipleDiffContext(FileDiffDetailFactory createDetail, List<(Models.Change Change, bool IsUnstaged)> changes, MultipleDiffContext previous = null)
        {
            var count = Math.Min(changes.Count, MaxFiles);
            MoreCount = changes.Count - count;

            Dictionary<string, FileDiff> prevFiles = null;
            if (previous != null)
            {
                prevFiles = new Dictionary<string, FileDiff>();
                foreach (var f in previous.Files)
                    prevFiles.TryAdd(GetKey(f.Change.Path, f.IsUnstaged), f);
            }

            var autoExpand = count <= MaxAutoExpandFiles;
            var hasUnstaged = false;
            var hasStaged = false;
            for (var i = 0; i < count; i++)
            {
                var (change, isUnstaged) = changes[i];

                FileDiff prev = null;
                prevFiles?.TryGetValue(GetKey(change.Path, isUnstaged), out prev);

                Files.Add(new FileDiff(createDetail, change, isUnstaged, autoExpand, prev));
                hasUnstaged |= isUnstaged;
                hasStaged |= !isUnstaged;
            }

            if (hasUnstaged && hasStaged)
            {
                var unstagedMarked = false;
                var stagedMarked = false;
                foreach (var f in Files)
                {
                    if (f.IsUnstaged && !unstagedMarked)
                    {
                        f.ShowUnstagedGroupHeader = true;
                        unstagedMarked = true;
                    }
                    else if (!f.IsUnstaged && !stagedMarked)
                    {
                        f.ShowStagedGroupHeader = true;
                        stagedMarked = true;
                    }
                }
            }
        }

        private static string GetKey(string path, bool isUnstaged)
        {
            return isUnstaged ? $"U::{path}" : $"S::{path}";
        }

        // Hard cap on stacked files; the surplus is surfaced via MoreCount ("and N more").
        private const int MaxFiles = 100;
        // Above this, files start collapsed and expand lazily to avoid materializing many editors at once.
        private const int MaxAutoExpandFiles = 10;
    }
}
