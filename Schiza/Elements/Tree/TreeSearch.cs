using System.Windows;
using Schiza.Other;

namespace Schiza.Elements.Tree
{
    public class TreeSearch : ViewModel
    {
        private TreeModel? _tree;
        public TreeModel? Tree
        {
            get => _tree;
            set
            {
                _tree = value;
                OnPropertyChanged();
            }
        }
        private string _searchText = string.Empty;

        public TreeSearch()
        {
            CheckText = (item) => item?.ToString() ?? string.Empty;
            DisplayText = (item) => item?.ToString() ?? string.Empty;
        }
        public TreeSearch(Func<TreeElement, string> checkText, Func<TreeElement, string> displayText)
        {
            CheckText = checkText;
            DisplayText = displayText;
        }

        /// <summary>
        /// Текст в поисковой строке
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }

        /// <summary>
        /// Поиск
        /// </summary>
        protected virtual void FilterItems()
        {
            if (_tree == null)
                return;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var item in _tree.AllItems)
                {
                    if (CheckText(item).IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        item.IsEnabled = true;
                    else
                        item.IsEnabled = false;
                }

                foreach (var item in _tree.AllItems.Where(i => i.IsEnabled))
                {
                    if (item.Parent != null)
                        SwitchOnVisibility(item.Parent);
                }
            }
            else
            {
                foreach (var item in _tree.AllItems)
                {
                    item.IsEnabled = true;
                }
            }
        }

        private void SwitchOnVisibility(TreeElement item)
        {
            item.IsEnabled = true;
            if (item.Parent != null)
                SwitchOnVisibility(item.Parent);
        }


        /// <summary>
        /// Текст для сравнения с поисковой строкой
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Func<TreeElement, string> CheckText;


        /// <summary>
        /// Отображаемый текст результатов выборки
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Func<TreeElement, string> DisplayText;

    }
}
