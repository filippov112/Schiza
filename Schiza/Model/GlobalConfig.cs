using Schiza.Services;

namespace Schiza.Model
{
    public class GlobalConfig
    {
        public string StructurePromptGlobal { get; set; } = $"{StorageService.KEY_WORD_INPUT}\n`````\n{StorageService.KEY_WORD_PROJECT}\n`````\n{StorageService.KEY_WORD_REQUEST}\n\n{StorageService.KEY_WORD_SETTINGS}";
        public string UserSettingsPrompt { get; set; } = "При ответе будь краток и лаконичен.";
        public string StyleFileBlock { get; set; } = $"Файл: {StorageService.KEY_WORD_PATH}\n````\n{StorageService.KEY_WORD_CONTENT}\n````\n";
        public GlobalConfig Copy() => new GlobalConfig
        {
            StructurePromptGlobal = StructurePromptGlobal,
            UserSettingsPrompt = UserSettingsPrompt,
            StyleFileBlock = StyleFileBlock
        };
    }
}
