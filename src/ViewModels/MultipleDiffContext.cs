using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
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

        public FileDiff(WorkingCopy owner, Models.Change change, bool isUnstaged, bool autoExpand, FileDiff previous)
        {
            _owner = owner;
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
            if (Change.IsConflicted)
            {
                _detail = new Conflict(_owner.Repository, _owner, Change);
                MaxBodyHeight = double.PositiveInfinity;
            }
            else
            {
                // Ignore the global `UseFullTextDiff` preference so one toggle cannot materialize every stacked file.
                var diff = new DiffContext(_owner.Repository.FullPath, new Models.DiffOption(Change, IsUnstaged), previous, true);
                diff.PropertyChanged += OnDiffPropertyChanged;
                _detail = diff;
                UpdateBodyHeight(diff);
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
        private const double NonTextBodyHeight = 400;

        private readonly WorkingCopy _owner;
        private bool _isExpanded;
        private object _detail = null;
        private bool _isBounded = false;
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

        public MultipleDiffContext(WorkingCopy owner, List<(Models.Change Change, bool IsUnstaged)> changes, MultipleDiffContext previous = null)
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

                Files.Add(new FileDiff(owner, change, isUnstaged, autoExpand, prev));
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

        private const int MaxFiles = 100;
        private const int MaxAutoExpandFiles = 10;
    }
}
