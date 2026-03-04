using Schiza.Elements.Explorer.Components;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

namespace Schiza.Elements.Explorer
{
    /// <summary>
    /// Логика взаимодействия для ExplorerControl.xaml
    /// </summary>
    public partial class ExplorerControl : UserControl
    {
        private Point _startPoint; // Точка начала перемещения
        private ExplorerElementVM? _draggedItem; // Элемент, который перетаскивается
        private TreeViewItem? _visualDropTarget; // Для визуального выделения

        public ExplorerControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Объявим событие выбора элемента для открытия в редакторе
        /// </summary>
        public event EventHandler<ExplorerElementVM>? ElementSelected;

        /// <summary>
        /// Транслирует выбор элемента в TreeView в открытие документа в редакторе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ExplorerElementVM item)
            {
                if (item == null || item.Type != ItemType.File)
                    return;
                ElementSelected?.Invoke(this, item);
            }
        }

        // Добавим метод для выбора элемента под курсором
        private void SelectItemUnderMouse(object sender, MouseButtonEventArgs e)
        {
            var treeView = (TreeView)sender;
            var element = e.OriginalSource as DependencyObject;
            // Ищем TreeViewItem, на котором произошло событие
            while (element != null && element != treeView)
            {
                if (element is TreeViewItem item)
                {
                    // Устанавливаем фокус и выделяем элемент
                    item.Focus();
                    if (!item.IsSelected)
                    {
                        item.IsSelected = true;
                    }
                    break; // Нашли и обработали, выходим
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        // Обработчик для PreviewMouseRightButtonDown
        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectItemUnderMouse(sender, e);
        }

        #region Перенос элементов дерева (Drag & Drop)

        /// <summary>
        /// Захват элемента дерева для перетаскивания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);

            // Найдем TreeViewItem, на котором произошло нажатие
            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (treeViewItem != null)
            {
                _draggedItem = treeViewItem.DataContext as ExplorerElementVM;
            }
        }

        /// <summary>
        /// Обработка перемещения мыши для инициации Drag & Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_draggedItem != null)
                    {
                        // Формируем данные для перетаскивания
                        DataObject data = new DataObject("ExplorerElementVM", _draggedItem);
                        // Дополнительно можно передать текстовое представление
                        data.SetData(DataFormats.Text, Path.GetRelativePath(Program.Storage.ProjectFolder, _draggedItem.FullPath));

                        // Инициируем операцию Drag & Drop
                        DragDrop.DoDragDrop(treeExplorer, data, DragDropEffects.Move);
                    }
                }
            }
        }

        /// <summary>
        /// Обработка наведения мыши на элемент во время Drag & Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("ExplorerElementVM"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Сброс предыдущего визуального выделения
            if (_visualDropTarget != null)
            {
                if (_visualDropTarget.Tag?.ToString() == "DropTargetHighlight")
                {
                    _visualDropTarget.ClearValue(TreeViewItem.BackgroundProperty);
                    _visualDropTarget.Tag = null;
                }
                _visualDropTarget = null;
            }

            Point mousePos = e.GetPosition(treeExplorer);
            DependencyObject element = (DependencyObject)treeExplorer.InputHitTest(mousePos);

            TreeViewItem? targetItem = FindAncestor<TreeViewItem>(element);
            ExplorerElementVM? targetVM = targetItem?.DataContext as ExplorerElementVM;

            // Если не нашли под курсором, возможно, над пустой областью папки
            if (targetVM == null && targetItem == null)
            {
                DependencyObject current = element;
                while (current != null && current != treeExplorer)
                {
                    if (current is TreeViewItem currentTVI)
                    {
                        targetItem = currentTVI;
                        targetVM = targetItem.DataContext as ExplorerElementVM;
                        break;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            if (targetVM != null)
            {
                ExplorerElementVM? draggedItem = e.Data.GetData("ExplorerElementVM") as ExplorerElementVM;

                if (draggedItem != null)
                {
                    // Определяем целевую папку для сброса
                    ExplorerElementVM? destinationFolder = null;
                    if (targetVM.Type == ItemType.Folder)
                    {
                        // Сброс на папку
                        destinationFolder = targetVM;
                    }
                    else if (targetVM.Type == ItemType.File)
                    {
                        // Сброс на файл - используем родительскую папку файла
                        destinationFolder = targetVM.Parent;
                    }

                    if (destinationFolder != null)
                    {
                        // Проверяем, можно ли переместить:
                        // 1. destinationFolder должна быть папкой (уже проверено выше через присвоение)
                        // 2. draggedItem не должен быть перемещён внутрь самой себя или своих потомков
                        if (destinationFolder != draggedItem && !IsDescendantOf(destinationFolder, draggedItem))
                        {
                            e.Effects = DragDropEffects.Move;
                            // Визуальное выделение целевого элемента (папки, в которую будет сброшен элемент)
                            if (targetItem != null) // targetItem - это элемент, над которым курсор, но выделяем destinationFolder
                            {
                                // Если сброс на файл, выделяем родительский элемент
                                TreeViewItem visualItemToHighlight = targetItem;
                                if (targetVM.Type == ItemType.File && targetVM.Parent != null)
                                {
                                    // Попробуем найти TreeViewItem для родителя
                                    // Это сложно сделать напрямую через DataContext.
                                    // Лучше использовать IsSelected как визуальный индикатор или Attached Property.
                                    // Пока оставим выделение на элементе под курсором, но пометим его как "drop target".
                                    // Или, для лучшего UX, выделяем саму папку, если курсор над файлом.
                                    // Попробуем найти TreeViewItem для destinationFolder
                                    visualItemToHighlight = GetTreeViewItemForDataContext(treeExplorer, destinationFolder);
                                }

                                if (visualItemToHighlight != null)
                                {
                                    visualItemToHighlight.Background = System.Windows.Media.Brushes.LightBlue; // Используем светло-голубой как в тригере
                                    visualItemToHighlight.Tag = "DropTargetHighlight";
                                    _visualDropTarget = visualItemToHighlight;
                                }
                                else
                                {
                                    // Если не удалось найти TreeViewItem для destinationFolder (маловероятно, но возможно при быстрых изменениях),
                                    // выделяем элемент под курсором
                                    targetItem.Background = System.Windows.Media.Brushes.LightBlue;
                                    targetItem.Tag = "DropTargetHighlight";
                                    _visualDropTarget = targetItem;
                                }
                            }
                        }
                        else
                        {
                            e.Effects = DragDropEffects.None;
                        }
                    }
                    else
                    {
                        // destinationFolder не определена (например, файл на самом верхнем уровне без Parent)
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                // Если не удалось определить целевой элемент данных
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Обработка сброса элемента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            // Сбрасываем визуальное выделение
            if (_visualDropTarget != null)
            {
                _visualDropTarget.ClearValue(TreeViewItem.BackgroundProperty);
                _visualDropTarget.Tag = null;
                _visualDropTarget = null;
            }

            if (!e.Data.GetDataPresent("ExplorerElementVM"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            ExplorerElementVM? draggedItem = e.Data.GetData("ExplorerElementVM") as ExplorerElementVM;
            if (draggedItem == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Найдем целевой элемент снова на момент Drop
            Point mousePos = e.GetPosition(treeExplorer);
            DependencyObject element = (DependencyObject)treeExplorer.InputHitTest(mousePos);
            TreeViewItem? targetItem = FindAncestor<TreeViewItem>(element);
            ExplorerElementVM? targetVM = targetItem?.DataContext as ExplorerElementVM;

            if (targetVM == null && targetItem == null)
            {
                DependencyObject current = element;
                while (current != null && current != treeExplorer)
                {
                    if (current is TreeViewItem currentTVI)
                    {
                        targetItem = currentTVI;
                        targetVM = targetItem.DataContext as ExplorerElementVM;
                        break;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            if (targetVM != null)
            {
                // Определяем целевую папку для сброса
                ExplorerElementVM? destinationFolder = null;
                if (targetVM.Type == ItemType.Folder)
                {
                    destinationFolder = targetVM;
                }
                else if (targetVM.Type == ItemType.File)
                {
                    destinationFolder = targetVM.Parent;
                }

                if (destinationFolder != null)
                {
                    // Получаем ViewModel для доступа к методу MoveItem
                    if (DataContext is ExplorerVM explorerVM)
                    {
                        bool success = explorerVM.Project?.MoveItem(draggedItem, destinationFolder) ?? false;
                        if (success)
                        {
                            e.Effects = DragDropEffects.Move;
                        }
                        else
                        {
                            e.Effects = DragDropEffects.None;
                        }
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    // destinationFolder не определена
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                // Цель не найдена
                e.Effects = DragDropEffects.None;
            }

            _draggedItem = null; // Сброс состояния
            e.Handled = true;
        }

        // Вспомогательный метод для проверки, является ли 'potentialParent' родителем 'child'
        // Имя изменено на IsDescendantOf для соответствия оригинальному комментарию
        private bool IsDescendantOf(ExplorerElementVM child, ExplorerElementVM potentialParent)
        {
            var current = child.Parent;
            while (current != null)
            {
                if (current == potentialParent)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Поиск нужного типа элемента в визуальном дереве
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="current"></param>
        /// <returns></returns>
        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
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
        /// Находит TreeViewItem, соответствующий заданному DataContext (ExplorerElementVM).
        /// Это вспомогательный метод для визуального выделения.
        /// </summary>
        /// <param name="treeView">Контрол TreeView</param>
        /// <param name="dataContext">Объект данных для поиска</param>
        /// <returns>TreeViewItem или null, если не найден</returns>
        private TreeViewItem? GetTreeViewItemForDataContext(ItemsControl container, object dataContext)
        {
            if (container == null) return null;

            if (container.DataContext == dataContext)
            {
                return container as TreeViewItem;
            }

            // Проходим по контейнерам элементов
            for (int i = 0; i < container.Items.Count; i++)
            {
                var item = container.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item != null)
                {
                    if (item.DataContext == dataContext)
                    {
                        return item;
                    }

                    // Рекурсивно проверяем дочерние элементы
                    var subContainer = item;
                    var result = GetTreeViewItemForDataContext(subContainer, dataContext);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        #endregion
    }
}