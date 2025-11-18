using Schiza.Elements.Explorer.Components;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
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
                    // Устанавливаем IsSelected в true, что вызовет обновление связанного свойства IsFocused
                    if (item.DataContext is ExplorerElementVM vm)
                    {
                        // Снимаем выделение (IsFocused) со всех элементов, кроме текущего
                        // Это может быть не нужно, если стандартное поведение WPF при установке IsSelected
                        // само управляет выделением. Проверим сначала, нужно ли это.
                        // Достаточно установить IsSelected, и стиль в XAML сработает.
                        // Но чтобы убедиться, что IsFocused установлен, можно явно снять с других.
                        // Однако, проще полагаться на стандартное поведение TreeView и IsSelected.
                        // В ViewModel IsSelected при установке в true, устанавливает IsFocused.
                        // Поэтому установка IsSelected должна быть достаточной.
                        if (!item.IsSelected)
                        {
                            item.IsSelected = true;
                        }
                        // e.Handled = true; // Опционально: может помешать другим обработчикам
                        break; // Нашли и обработали, выходим
                    }
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        // Обработчик для PreviewMouseRightButtonDown
        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectItemUnderMouse(sender, e);
            // Важно: Не вызываем e.Handled = true;, если хотим, чтобы контекстное меню открылось нормально.
            // SelectItemUnderMouse устанавливает IsSelected, что приведет к установке IsFocused через привязку.
            // Это должно быть достаточным для корректного отображения выделения.
        }

        #region Перенос элементов дерева в промпт
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
                        ExplorerElementVM item = (ExplorerElementVM)treeViewItem.DataContext;

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
        #endregion
    }
}