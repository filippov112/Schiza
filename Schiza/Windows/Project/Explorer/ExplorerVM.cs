using Schiza.Elements.Explorer;
using Schiza.Other;
using Schiza.Windows.Project.Explorer.Components;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Schiza.Windows.Project.Explorer
{
    public class ExplorerVM: ViewModel
    {
        public ExplorerTreeVM? Project { get; set; } = null;
        public ExplorerSearchVM Search { get; set; } = new ExplorerSearchVM(checkText: (item) => item.Text(), displayText: (item) => item.FullPath);
        public ExplorerMenuVM ExplorerContextMenu { get; set; }

        private ExplorerControl view;
        public ExplorerVM(ExplorerControl window, Dispatcher uiDispatcher)
        {
            view = window;
            Project = new ExplorerTreeVM(uiDispatcher);
            Search.Tree = Project;
            ExplorerContextMenu = new ExplorerMenuVM(Project, uiDispatcher);
        }

        /// <summary>
        /// Массив файлов и каталогов для парсинга
        /// </summary>
        public ObservableCollection<ExplorerElementVM> AllItems
        {
            get
            {
                return Project?.AllItems ?? new ObservableCollection<ExplorerElementVM>();
            }
        }

        /// <summary>
        /// Перезагрузка дерева проекта
        /// </summary>
        public void Refresh()
        {
            Project?.Refresh();
        }
    }
}
