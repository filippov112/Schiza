using ICSharpCode.AvalonEdit.Highlighting;
using SharpToken;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Schiza.Elements.Tree;
using Schiza.Services;
using Schiza.Windows.Project;
using Schiza.Windows.Settings;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;


namespace Schiza.Windows.Project
{
    public partial class ProjectView : Window
    {
        private ProjectVM vm;
        public ProjectView()
        {
            InitializeComponent();
            vm = new ProjectVM(this, Dispatcher);
            DataContext = vm;
        }


        /// <summary>
        /// Открытие проекта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenProject(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Выберите проект";
                folderDialog.UseDescriptionForTitle = true;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    editorAvalon.Clear();
                    txtMessage.Text = string.Empty;
                    Program.Storage.ProjectFolder = folderDialog.SelectedPath;
                    vm.Project?.Refresh();
                }
            }
        }

        /// <summary>
        /// Открытие окна настроек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsView();
            settings.ShowDialog();
        }


        /// <summary>
        /// Запуск процесса сборки промпта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildPrompt(object sender, RoutedEventArgs e)
        {
            var content = ContentBuilder.Build(vm.Project.AllItems.Where(x => x.IsSelected && x.Type == ItemType.File).Select(x => x.FullPath).ToList());
            string prompt = Program.Storage.GetPrompt(content, txtMessage.Text);

            // Для GPT-3.5 и GPT-4
            var encoding = GptEncoding.GetEncoding("cl100k_base");
            var tokens = encoding.Encode(prompt);

            lWordCounter.Content = tokens.Count().ToString();
            editorAvalon.Text = prompt;
            Clipboard.SetText(prompt);
        }

        /// <summary>
        /// Эндпоинт для обновления содержимого редактора из ViewModel
        /// </summary>
        /// <param name="content"></param>
        public void UpdateEditorContent(string content)
        {
            editorAvalon.Text = content;
        }

        /// <summary>
        /// Транслирует выбор элемента в TreeView в открытие документа в редакторе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeElement item)
            {
                if (item == null || item.Type != ItemType.File)
                    return;
                vm?.OpenDocument(item);
                SetSyntaxHighlightingByExtension(item.FullPath);
            }
        }
        /// <summary>
        /// Определение подсветки синтаксиса по расширению файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        private void SetSyntaxHighlightingByExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".cs":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                    break;
                case ".xml":
                case ".config":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    break;
                case ".js":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                    break;
                case ".html":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML");
                    break;
                case ".css":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CSS");
                    break;
                case ".py":
                    editorAvalon.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Python");
                    break;
                default:
                    editorAvalon.SyntaxHighlighting = null; // Без подсветки
                    break;
            }
        }

        #region Drag-drop tree elements
        private Point _startPoint; // Точка начала перемещения
        private bool _isDragging = false; // Процесс перемещения
        /// <summary>
        /// Захват элемента дерева для перетаскивания в промпт
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        /// <summary>
        /// Перетаскивание элемента дерева в промпт
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (treeViewItem != null)
                    {
                        TreeElement item = (TreeElement)treeViewItem.DataContext;

                        // Формируем текстовые данные для перемещения
                        DataObject data = new DataObject(DataFormats.Text, Path.GetRelativePath(Program.Storage.ProjectFolder, item.FullPath));

                        if (!_isDragging)
                        {
                            _isDragging = true;
                            DragDrop.DoDragDrop(treeViewItem, data, DragDropEffects.Copy); // Перемещение выполняется до момента отпуска кнопки мыши
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Поиск нужного типа элемента в дереве события
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="current"></param>
        /// <returns></returns>
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        /// <summary>
        /// Обработчик сброса элемента дерева в текстовое поле промпта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FilePath"))
            {
                string filePath = e.Data.GetData("FilePath") as string;
                txtMessage.Text = filePath;
                e.Handled = true;
            }
        }
        #endregion
    }
}