using System.Collections.ObjectModel;
using Schiza.Other;
using Schiza.Services;

namespace Schiza.Elements.Explorer.Components
{
    // Базовый класс для элементов дерева
    public class ExplorerElementVM : ViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private bool _isFocused;
        private bool _isEnabled = true;

        public ItemType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Text() => Type == ItemType.Folder ? string.Empty : ContentBuilder.ShouldInclude(FullPath) ? ContentBuilder.ParseFile(FullPath) : string.Empty;
        public ExplorerElementVM? Parent { get; set; } = null;


        public ObservableCollection<ExplorerElementVM> Children { get; set; } = [];

        public bool IsExpanded // Каталог раскрыт
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
        public bool IsSelected // Элемент выбран для парсинга
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                foreach (var item in Children.Where(item => item.IsEnabled))
                    item.IsSelected = value;
                OnPropertyChanged();
            }
        }
        public bool IsFocused // Элемент отображается в редакторе
        {
            get => _isFocused;
            set
            {
                _isFocused = value;
                OnPropertyChanged();
            }
        }
        public bool IsEnabled  // Элемент соответствует фильтру поиска
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public enum ItemType
    {
        Folder,
        File
    }
}
