using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Schiza.Windows.Settings
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        private SettingsVM vm;
        public SettingsView()
        {
            InitializeComponent();
            vm = new SettingsVM(this);
            DataContext = vm;
            btnProjectSettings.IsEnabled = !string.IsNullOrEmpty(Program.Storage.ProjectFolder);
        }

        private void DeactivateAllPanels()
        {
            generalSettings.Visibility = Visibility.Hidden;
            projectSettings.Visibility = Visibility.Hidden;
        }

        private void btnProjectSettings_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllPanels();
            projectSettings.Visibility = Visibility.Visible;
        }

        private void btnGeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllPanels();
            generalSettings.Visibility = Visibility.Visible;
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            Program.Storage.SaveSettings(vm.GlobalConfig, vm.LocalConfig);
            vm.BtnSaveIsActive = false;
        }
    }
}
