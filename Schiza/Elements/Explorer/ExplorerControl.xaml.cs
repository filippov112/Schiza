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
