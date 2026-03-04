using Schiza.Model;
using Schiza.Other;
using Schiza.Services;

namespace Schiza.Windows.Settings
{
    public class SettingsVM : ViewModel
    {
        private SettingsView? view;
        public SettingsVM(SettingsView view)
        {
            this.view = view;
            GlobalConfig = Program.Storage.GConfig.Copy();
            LocalConfig = Program.Storage.LConfig.Copy();
        }

        public GlobalConfig GlobalConfig { get; private set; }
        public LocalConfig LocalConfig { get; private set; }

        public string InputProjectPrompt
        {
            get => LocalConfig.InputProjectPrompt;
            set
            {
                if (LocalConfig.InputProjectPrompt != value)
                    BtnSaveIsActive = true;
                LocalConfig.InputProjectPrompt = value;
                OnPropertyChanged();
            }
        }

        public string StructurePromptLocal
        {
            get => LocalConfig.StructurePromptLocal;
            set
            {
                if (LocalConfig.StructurePromptLocal != value)
                    BtnSaveIsActive = true;
                LocalConfig.StructurePromptLocal = value;
                OnPropertyChanged();
            }
        }

        public string StructurePromptGlobal
        {
            get => GlobalConfig.StructurePromptGlobal;
            set
            {
                if (GlobalConfig.StructurePromptGlobal != value)
                    BtnSaveIsActive = true;
                GlobalConfig.StructurePromptGlobal = value;
                OnPropertyChanged();
            }
        }

        public string UserSettingsPrompt
        {
            get => GlobalConfig.UserSettingsPrompt;
            set
            {
                if (GlobalConfig.UserSettingsPrompt != value)
                    BtnSaveIsActive = true;
                GlobalConfig.UserSettingsPrompt = value;
                OnPropertyChanged();
            }
        }

        public string StyleFileBlock
        {
            get => GlobalConfig.StyleFileBlock;
            set
            {
                if (GlobalConfig.StyleFileBlock != value)
                    BtnSaveIsActive = true;
                GlobalConfig.StyleFileBlock = value;
                OnPropertyChanged();
            }
        }



        private bool _btnSaveIsActive = false;

        public bool BtnSaveIsActive
        {
            get => _btnSaveIsActive;
            set
            {
                _btnSaveIsActive = value;
                OnPropertyChanged();
            }
        }
    }
}
