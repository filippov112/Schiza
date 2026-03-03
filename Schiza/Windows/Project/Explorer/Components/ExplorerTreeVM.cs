using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Schiza.Other;
using Schiza.Services;

namespace Schiza.Windows.Project.Explorer.Components
{
    public class ExplorerTreeVM : ViewModel
    {
        public ExplorerTreeVM(Dispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            Items = new ObservableCollection<ExplorerElementVM>();
        }
        public ObservableCollection<ExplorerElementVM> AllItems { get; set; } = [];
        private ObservableCollection<ExplorerElementVM> _items = [];

        private bool skip_all_messagess = false;
        private FileSystemWatcher? _watcher = null;
        private string _watcher_path = string.Empty;
        private Dispatcher? _uiDispatcher = null;

        private List<string> _expandedPaths = new List<string>();
        private List<string> _selectedPaths = new List<string>();

        // Флаг для предотвращения перекрытия Refresh
        private bool _isRefreshing = false;

        private void SaveTreeState()
        {
            // Проверяем, инициализирован ли _uiDispatcher и вызываем из UI-потока, если нужно
            // Но в текущем контексте LoadProject вызывается из UI-потока, так что это безопасно
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
            if (_isRefreshing)
            {
                // Если Refresh уже выполняется, откладываем новый вызов
                // Это предотвращает ошибки при быстрых изменениях
                _uiDispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    // Повторная проверка внутри вызова диспетчера
                    if (!_isRefreshing)
                    {
                        Refresh();
                    }
                }));
                return;
            }

            _isRefreshing = true; // Устанавливаем флаг
            try
            {
                StartWatching(Program.Storage.ProjectFolder);
                LoadProject(Program.Storage.ProjectFolder);
            }
            finally
            {
                _isRefreshing = false; // Сбрасываем флаг в любом случае
            }
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
                    _watcher.Renamed -= OnFileChanged;
                    _watcher.Dispose();
                }
                _watcher = new FileSystemWatcher(path);
                _watcher.IncludeSubdirectories = true;
                _watcher.Created += OnFileChanged;
                _watcher.Deleted += OnFileChanged;
                _watcher.Renamed += OnFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
        }

        // Обработчик событий файловой системы
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Всегда вызываем Refresh через Dispatcher, чтобы он выполнялся в UI-потоке
            _uiDispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(Refresh));
        }

        public ObservableCollection<ExplorerElementVM> Items
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
            // Убедимся, что метод вызывается из UI-потока
            if (_uiDispatcher != null && !_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LoadProject(rootPath)));
                return;
            }

            if (rootPath == string.Empty || !Directory.Exists(rootPath))
                return;

            SaveTreeState();
            skip_all_messagess = false;
            AllItems.Clear(); // Очищаем коллекцию в UI-потоке
            Items.Clear();    // Очищаем коллекцию в UI-потоке

            var rootItem = CreateTreeItem(null, new DirectoryInfo(rootPath));
            Items.Add(rootItem);
            RestoreTreeState();
            OnPropertyChanged(); // Уведомляем, что Items изменились
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
        private ExplorerElementVM CreateTreeItem(ExplorerElementVM? parent, FileSystemInfo info)
        {
            var item = new ExplorerElementVM
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

        // Метод для перемещения элемента в файловой системе
        public bool MoveItem(ExplorerElementVM element, ExplorerElementVM address)
        {
            if (element == null || address == null || address.Type != ItemType.Folder)
                return false;


            string old_path = element.FullPath;
            string new_path = Path.Combine(address.FullPath, element.Name);

     

            


            

            
        }

        

    }
}