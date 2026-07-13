using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class SubmoduleRevisionCompare : ObservableObject
    {
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public Models.Commit Base
        {
            get => _base;
            private set => SetProperty(ref _base, value);
        }

        public Models.Commit To
        {
            get => _to;
            private set => SetProperty(ref _to, value);
        }

        public int TotalChanges
        {
            get => _totalChanges;
            private set => SetProperty(ref _totalChanges, value);
        }

        public List<Models.Change> VisibleChanges
        {
            get => _visibleChanges;
            private set => SetProperty(ref _visibleChanges, value);
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                    UpdateDetail();
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    RefreshVisible();
            }
        }

        public object DetailContext
        {
            get => _detailContext;
            private set => SetProperty(ref _detailContext, value);
        }

        public SubmoduleRevisionCompare(Models.SubmoduleDiff diff)
        {
            _repo = diff.FullPath;
            _base = diff.Old.Commit;
            _to = diff.New.Commit;

            Refresh();
        }

        public void Swap()
        {
            (Base, To) = (To, Base);
            Refresh();
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        public string GetAbsPath(string path)
        {
            return Native.OS.GetAbsPath(_repo, path);
        }

        public void OpenInExternalDiffTool(Models.Change change)
        {
            new Commands.DiffTool(_repo, new Models.DiffOption(_base.SHA, _to.SHA, change)).Open();
        }

        public async Task<bool> SaveChangesAsPatchAsync(List<Models.Change> changes, string saveTo)
        {
            return await Commands.SaveChangesAsPatch.ProcessRevisionCompareChangesAsync(_repo, changes, _base.SHA, _to.SHA, saveTo);
        }

        private void Refresh()
        {
            IsLoading = true;
            VisibleChanges = [];
            SelectedChanges = [];

            Task.Run(async () =>
            {
                _changes = await new Commands.CompareRevisions(_repo, _base.SHA, _to.SHA)
                    .ReadAsync()
                    .ConfigureAwait(false);

                var visible = _changes;
                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    visible = new List<Models.Change>();
                    foreach (var c in _changes)
                    {
                        if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                            visible.Add(c);
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    TotalChanges = _changes.Count;
                    VisibleChanges = visible;
                    IsLoading = false;

                    // No auto-selected file: an empty selection shows the whole comparison as a stacked diff.
                    SelectedChanges = [];
                });
            });
        }

        private void RefreshVisible()
        {
            if (_changes == null)
                return;

            if (string.IsNullOrEmpty(_searchFilter))
            {
                VisibleChanges = _changes;
            }
            else
            {
                var visible = new List<Models.Change>();
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }

                VisibleChanges = visible;
            }

            // The whole-set stack tracks the filtered list, but rebuilding it per keystroke would spawn
            // a git diff per visible file; coalesce until typing pauses.
            if (_selectedChanges is not { Count: > 0 })
            {
                _filterDebounce ??= new Debouncer(TimeSpan.FromMilliseconds(300), () =>
                {
                    if (_selectedChanges is not { Count: > 0 })
                        UpdateDetail();
                });
                _filterDebounce.Trigger();
            }
        }

        private void UpdateDetail()
        {
            DetailContext = MultipleDiffContext.BuildDetail(_repo, MakeDiffOption, _selectedChanges, _visibleChanges, _detailContext);
        }

        private Models.DiffOption MakeDiffOption(Models.Change change)
        {
            return new Models.DiffOption(_base.SHA, _to.SHA, change);
        }

        private string _repo;
        private bool _isLoading = true;
        private Models.Commit _base = null;
        private Models.Commit _to = null;
        private int _totalChanges = 0;
        private List<Models.Change> _changes = null;
        private List<Models.Change> _visibleChanges = null;
        private List<Models.Change> _selectedChanges = null;
        private string _searchFilter = string.Empty;
        private object _detailContext = null;
        private Debouncer _filterDebounce = null;
    }
}
