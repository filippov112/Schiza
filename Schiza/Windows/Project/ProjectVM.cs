using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using Schiza.Elements.Tree;
using Schiza.Other;

namespace Schiza.Windows.Project
{
    public class ProjectVM : ViewModel
    {
        private ProjectView view;
        public ProjectVM(ProjectView window, Dispatcher uiDispatcher)
        {
            view = window;
            Project = new TreeModel(uiDispatcher);
            Search.Tree = Project;
        }

        public TreeModel? Project { get; set; } = null;
        public TreeSearch Search { get; set; } = new TreeSearch(checkText: (item) => item.Text(), displayText: (item) => item.FullPath);

        public void OpenDocument(TreeElement item)
        {
            try
            {
                // Загрузить содержимое файла
                string content = File.ReadAllText(item.FullPath);
                view.UpdateEditorContent(content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}");
            }
        }
    }
}
