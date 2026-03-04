using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Schiza.Other;
using Schiza.Services;

namespace Schiza.Elements.Explorer.Components
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
        public bool MoveItem(ExplorerElementVM itemToMove, ExplorerElementVM newParent)
        {
            if (itemToMove == null || newParent == null || newParent.Type != ItemType.Folder)
                return false;

            if (itemToMove.Parent == newParent)
                return true; // Уже в нужной папке

            string sourcePath = itemToMove.FullPath;
            string destinationPath = Path.Combine(newParent.FullPath, itemToMove.Name);

            // нельзя перемещать элемент в самого себя
            if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"нельзя перемещать элемент в самого себя: source = `{sourcePath}`, dest = `{destinationPath}`");
                return false;
            }

            // нельзя перемещать родителя в его потомка
            if (IsChildPath(sourcePath, destinationPath))
            {
                Debug.WriteLine($"нельзя перемещать родителя в его потомка: source = `{sourcePath}`, dest = `{destinationPath}`");
                return false;
            }


            // Проверка на существование файла/папки с таким именем в новом месте
            if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
            {
                MessageBox.Show($"Файл или папка с именем '{itemToMove.Name}' уже существует в папке '{newParent.Name}'.", "Ошибка перемещения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                if (itemToMove.Type == ItemType.Folder)
                {
                    Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    File.Move(sourcePath, destinationPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Вспомогательный метод для проверки, является ли 'parent' родительским для 'child'
        private bool IsChildPath(string parent, string child)
        {
            var parentInfo = new DirectoryInfo(parent);
            var childInfo = new DirectoryInfo(child);

            while (childInfo.Parent != null)
            {
                if (childInfo.Parent.FullName.Equals(parentInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    return true;

                childInfo = childInfo.Parent;
            }

            return false;
        }

    }
}