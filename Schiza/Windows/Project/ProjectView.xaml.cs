using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using System.Windows;
using DragEventArgs = System.Windows.DragEventArgs;
using Schiza.Elements.Explorer.Components;

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
        /// Эндпоинт для обновления содержимого редактора из ViewModel
        /// </summary>
        /// <param name="content"></param>
        public void UpdateEditorContent(string content)
        {
            editorAvalon.Text = content;
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

        #region Перенос элементов дерева в промпт

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

        /// <summary>
        /// Выделение элемента в дереве проекта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void explorerControl_ElementSelected(object sender, ExplorerElementVM e)
        {
            vm?.OpenDocument(e);
            SetSyntaxHighlightingByExtension(e.FullPath);
        }
    }
}