using Schiza.Elements.Explorer;
using Schiza.Elements.Explorer.Components;
using Schiza.Other;
using Schiza.Services;
using Schiza.Windows.Settings;
using SharpToken;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace Schiza.Windows.Project
{
    public class ProjectVM : ViewModel
    {
        private ProjectView view;
        private readonly Dispatcher uiDispatcher;

        public ProjectVM(ProjectView window, Dispatcher uiDispatcher)
        {
            view = window;
            this.uiDispatcher = uiDispatcher;
            Explorer = new ExplorerVM(view.explorerControl, uiDispatcher);

            OpenProjectCommand = new RelayCommand(OpenProject);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            BuildPromptCommand = new RelayCommand(BuildPrompt);
        }

        public ExplorerVM Explorer { get; set; }

        private string editorText = string.Empty;
        public string EditorText
        {
            get => editorText;
            set
            {
                editorText = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenProjectCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand BuildPromptCommand { get; }

        private void OpenProject(object? e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            try
            {
                folderDialog.Description = "Выберите проект";
                folderDialog.UseDescriptionForTitle = true;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    view.editorAvalon.Clear();
                    view.txtMessage.Text = string.Empty;
                    Program.Storage.ProjectFolder = folderDialog.SelectedPath;
                    Explorer.Refresh();
                }
            }
            finally
            {
                folderDialog.Dispose();
            }
        }

        private void OpenSettings(object? e)
        {
            var settings = new SettingsView();
            settings.ShowDialog();
        }

        private void BuildPrompt(object? e)
        {
            var selectedFiles = Explorer.AllItems
                .Where(x => x.IsSelected && x.Type == ItemType.File)
                .Select(x => x.FullPath)
                .ToList();

            var content = ContentBuilder.Build(selectedFiles);
            string prompt = Program.Storage.GetPrompt(content, view.txtMessage.Text);

            var encoding = GptEncoding.GetEncoding("cl100k_base");
            var tokens = encoding.Encode(prompt);

            view.lWordCounter.Content = tokens.Count().ToString();
            view.UpdateEditorContent(prompt);
            Clipboard.SetText(prompt);
        }

        public void OpenDocument(ExplorerElementVM item)
        {
            try
            {
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