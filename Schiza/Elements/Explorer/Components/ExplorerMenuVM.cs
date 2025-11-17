using Schiza.Other;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;


namespace Schiza.Elements.Explorer.Components
{
    public class ExplorerMenuVM : ViewModel
    {
        private readonly ExplorerTreeVM _project;
        private readonly Dispatcher _uiDispatcher;

        public ExplorerMenuVM(ExplorerTreeVM project, Dispatcher uiDispatcher)
        {
            _project = project;
            _uiDispatcher = uiDispatcher;

            CreateFileCommand = new RelayCommand<ExplorerElementVM>(CreateFile);
            CreateFolderCommand = new RelayCommand<ExplorerElementVM>(CreateFolder);
            DeleteCommand = new RelayCommand<ExplorerElementVM>(DeleteItem);
            ExcludeCommand = new RelayCommand<ExplorerElementVM>(ExcludeItem);
            RenameCommand = new RelayCommand<ExplorerElementVM>(RenameItem);
        }

        // Команды
        public ICommand CreateFileCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExcludeCommand { get; }
        public ICommand RenameCommand { get; }

        private void CreateFile(ExplorerElementVM parentItem)
        {
            if (parentItem == null || parentItem.Type != ItemType.Folder)
            {
                // Пытаемся создать в корне проекта, если родитель не папка
                parentItem = _project?.Items?.FirstOrDefault();
                if (parentItem?.Type != ItemType.Folder) return;
            }

            var fileName = Microsoft.VisualBasic.Interaction.InputBox("Введите имя файла:", "Создать файл", "newfile.txt");
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var fullPath = Path.Combine(parentItem.FullPath, fileName);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                MessageBox.Show("Файл или каталог с таким именем уже существует.");
                return;
            }

            try
            {
                File.WriteAllText(fullPath, string.Empty);
                _project?.Refresh(); // Перезагрузка дерева
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании файла: {ex.Message}");
            }
        }

        private void CreateFolder(ExplorerElementVM parentItem)
        {
            if (parentItem == null || parentItem.Type != ItemType.Folder)
            {
                parentItem = _project?.Items?.FirstOrDefault();
                if (parentItem?.Type != ItemType.Folder) return;
            }

            var folderName = Microsoft.VisualBasic.Interaction.InputBox("Введите имя каталога:", "Создать каталог", "NewFolder");
            if (string.IsNullOrWhiteSpace(folderName)) return;

            var fullPath = Path.Combine(parentItem.FullPath, folderName);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                MessageBox.Show("Файл или каталог с таким именем уже существует.");
                return;
            }

            try
            {
                Directory.CreateDirectory(fullPath);
                _project?.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании каталога: {ex.Message}");
            }
        }

        private void DeleteItem(ExplorerElementVM item)
        {
            if (item == null) return;

            var confirmMessage = item.Type == ItemType.Folder
                ? $"Удалить каталог '{item.Name}' и всё его содержимое?"
                : $"Удалить файл '{item.Name}'?";
            var result = MessageBox.Show(confirmMessage, "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                if (item.Type == ItemType.Folder)
                {
                    Directory.Delete(item.FullPath, true);
                }
                else
                {
                    File.Delete(item.FullPath);
                }
                _project?.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void ExcludeItem(ExplorerElementVM item)
        {
            if (item == null) return;

            // Просто удаляем из дерева и из AllItems, не из файловой системы
            // Нужно найти родителя и удалить из его Children
            if (item.Parent != null)
            {
                item.Parent.Children.Remove(item);
                _project?.AllItems.Remove(item);
            }
            else if (_project?.Items != null)
            {
                // Если это корневой элемент (должен быть один)
                _project.Items.Remove(item);
                _project.AllItems.Remove(item);
            }
        }

        private void RenameItem(ExplorerElementVM item)
        {
            if (item == null) return;

            var currentName = item.Name;
            var newName = Microsoft.VisualBasic.Interaction.InputBox("Введите новое имя:", "Переименовать", currentName);
            if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;

            var newFullPath = Path.Combine(Path.GetDirectoryName(item.FullPath) ?? ".", newName);
            if (File.Exists(newFullPath) || Directory.Exists(newFullPath))
            {
                MessageBox.Show("Файл или каталог с таким именем уже существует.");
                return;
            }

            try
            {
                if (item.Type == ItemType.Folder)
                {
                    Directory.Move(item.FullPath, newFullPath);
                }
                else
                {
                    File.Move(item.FullPath, newFullPath);
                }
                // Обновляем свойства элемента
                item.Name = newName;
                item.FullPath = newFullPath;
                // Обновляем FullPath для всех детей
                UpdateChildrenPaths(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переименовании: {ex.Message}");
            }
        }

        private void UpdateChildrenPaths(ExplorerElementVM parent)
        {
            foreach (var child in parent.Children)
            {
                child.FullPath = Path.Combine(parent.FullPath, child.Name);
                if (child.Type == ItemType.Folder)
                {
                    UpdateChildrenPaths(child); // Рекурсивно для вложенных
                }
            }
        }
    }
}
