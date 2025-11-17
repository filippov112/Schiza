namespace Schiza.Model
{
    public class LocalConfig
    {
        public string InputProjectPrompt { get; set; } = "Помоги мне пожалуйста со следующим проектом. Вот его структура:";
        public string StructurePromptLocal { get; set; } = string.Empty;
        public LocalConfig Copy() => new LocalConfig
        {
            InputProjectPrompt = InputProjectPrompt,
            StructurePromptLocal = StructurePromptLocal
        };
    }
}
