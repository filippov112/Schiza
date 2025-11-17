using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using Schiza.Other;
using Schiza.Services;

namespace Schiza.Elements.Tree
{
    public class TreeModel : ViewModel
    {
        public TreeModel(Dispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            Items = new ObservableCollection<TreeElement>();
        }
        public ObservableCollection<TreeElement> AllItems { get; set; } = [];
        private ObservableCollection<TreeElement> _items = [];
        private TreeElement? _selectedItem;
        private bool skip_all_messagess = false;
        private FileSystemWatcher? _watcher = null;
        private string _watcher_path = string.Empty;
        private Dispatcher? _uiDispatcher = null;


        private List<string> _expandedPaths = new List<string>();
        private List<string> _selectedPaths = new List<string>();

        private void SaveTreeState()
        {
            _expandedPaths.Clear();
            _selectedPaths.Clear();
            foreach (var item in AllItems)
            {
                if (item.IsExpanded)
                    _expandedPaths.Add(item.FullPath);
                if (item.IsSelected)
                    _selectedPaths.Add(item.FullPath);
            }
        }

        private void RestoreTreeState()
        {
            foreach (var item in AllItems)
            {
                item.IsExpanded = _expandedPaths.Contains(item.FullPath);
                item.IsSelected = _selectedPaths.Contains(item.FullPath);
            }
        }

        /// <summary>
        /// Обновление структуры проекта
        /// </summary>
        public void Refresh()
        {
            StartWatching(Program.Storage.ProjectFolder);
            LoadProject(Program.Storage.ProjectFolder);
        }
        /// <summary>
        /// Синхронизация изменений в файловой системе
        /// </summary>
        /// <param name="path"></param>
        private void StartWatching(string path)
        {
            if (_watcher_path == path)
                return;
            else
            {
                _watcher_path = path;
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileChanged;
                    _watcher.Deleted -= OnFileChanged;
                    _watcher.Renamed -= OnFileRenamed;
                    _watcher.Dispose();
                }
                _watcher = new FileSystemWatcher(path);
                _watcher.IncludeSubdirectories = true;
                _watcher.Created += OnFileChanged;
                _watcher.Deleted += OnFileChanged;
                _watcher.Renamed += OnFileRenamed;
                _watcher.EnableRaisingEvents = true;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) => _uiDispatcher?.Invoke(Refresh);
        private void OnFileRenamed(object sender, RenamedEventArgs e) => _uiDispatcher?.Invoke(Refresh);

        public ObservableCollection<TreeElement> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Загрузка структуры проекта из файловой системы
        /// </summary>
        /// <param name="rootPath"></param>
        public void LoadProject(string rootPath)
        {
            if (rootPath == string.Empty || !Directory.Exists(rootPath))
                return;
            SaveTreeState();
            skip_all_messagess = false;
            AllItems.Clear();
            Items.Clear();

            var rootItem = CreateTreeItem(null, new DirectoryInfo(rootPath));
            Items.Add(rootItem);
            RestoreTreeState();
            OnPropertyChanged();
        }

        /// <summary>
        /// Служебный каталог, который не нужно отображать в дереве
        /// </summary>
        private string _serviceDir => Path.Combine(Program.Storage.ProjectFolder, StorageService.LocalConfigFolder);

        /// <summary>
        /// Рекурсивный перебор проекта
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private TreeElement CreateTreeItem(TreeElement? parent, FileSystemInfo info)
        {
            var item = new TreeElement
            {
                Name = info.Name,
                FullPath = info.FullName,
                Type = info is DirectoryInfo ? ItemType.Folder : ItemType.File,
                Children = [],
                Parent = parent
            };
            AllItems.Add(item);

            if (info is DirectoryInfo directory)
            {
                try
                {
                    foreach (var dir in directory.GetDirectories())
                    {
                        if (Path.Combine(dir.FullName) == _serviceDir)
                            continue;
                        if ((dir.Attributes & FileAttributes.Hidden) == 0)
                        {
                            var child = CreateTreeItem(item, dir);
                            item.Children.Add(child);
                        }
                    }

                    foreach (var file in directory.GetFiles())
                    {
                        if ((file.Attributes & FileAttributes.Hidden) == 0)
                        {
                            var child = CreateTreeItem(item, file);
                            item.Children.Add(child);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!skip_all_messagess)
                    {
                        if (MessageBox.Show(e.Message, "Внимание!", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error) == DialogResult.Ignore)
                            skip_all_messagess = true;
                    }
                }
            }

            return item;
        }
    }
}
