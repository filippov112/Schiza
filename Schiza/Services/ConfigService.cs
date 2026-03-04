using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Schiza.Model;

namespace Schiza.Services
{
    public class ConfigService
    {
        public ConfigService(string globalConfigPath, string localConfigFileName, string localConfigFolder)
        {
            GlobalConfigPath = globalConfigPath;
            LocalConfigFileName = localConfigFileName;
            LocalConfigFolder = localConfigFolder;
        }

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Относительный маршрут настроек программы
        /// </summary>
        private string GlobalConfigPath = "./config/global_config.json";
        /// <summary>
        /// Файл настроек проекта
        /// </summary>
        private string LocalConfigFileName = "local_config.json";
        /// <summary>
        /// Каталог служебных файлов проекта
        /// </summary>
        private string LocalConfigFolder = ".Schiza";

        public GlobalConfig GC { get; set; } = new(); // Общие настройки приложения
        public LocalConfig LC { get; set; } = new(); // Настройки проекта


        /// <summary>
        /// Загрузка глобальных настроек
        /// </summary>
        /// <returns></returns>
        public void LoadGlobalConfig()
        {
            try
            {
                if (File.Exists(GlobalConfigPath))
                {
                    var json = File.ReadAllText(GlobalConfigPath);
                    GC = JsonSerializer.Deserialize<GlobalConfig>(json, _jsonOptions) ?? new();
                }
                else
                {

                    // Создаем директорию если не существует
                    Directory.CreateDirectory(Path.GetDirectoryName(GlobalConfigPath) ?? ".");
                    CreateDefaultGlobalConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки глобальной конфигурации: {ex.Message}");
                CreateDefaultGlobalConfig();
            }
        }

        /// <summary>
        /// Загрузка локальных настроек
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public void LoadLocalConfig(string directoryPath)
        {
            var localConfigPath = Path.Combine(directoryPath, LocalConfigFolder, LocalConfigFileName);

            try
            {
                if (File.Exists(localConfigPath))
                {
                    var json = File.ReadAllText(localConfigPath);
                    LC = JsonSerializer.Deserialize<LocalConfig>(json, _jsonOptions) ?? new();
                }
                else
                {
                    CreateDefaultLocalConfig(directoryPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки локальной конфигурации: {ex.Message}");
                CreateDefaultLocalConfig(directoryPath);
            }
        }

        /// <summary>
        /// Глобальные настройки по умолчанию
        /// </summary>
        /// <returns></returns>
        public void CreateDefaultGlobalConfig()
        {
            GC = new();
            SaveGlobalConfig();
        }

        /// <summary>
        /// Локальные настройки по умолчанию
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public void CreateDefaultLocalConfig(string directoryPath)
        {
            LC = new();
            SaveLocalConfig(directoryPath);
        }

        /// <summary>
        /// Сохранение глобальных настроек
        /// </summary>
        public void SaveGlobalConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(GlobalConfigPath) ?? ".");
                var json = JsonSerializer.Serialize(GC, _jsonOptions);
                File.WriteAllText(GlobalConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения глобальной конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохранение локальных настроек
        /// </summary>
        /// <param name="directoryPath"></param>
        public void SaveLocalConfig(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.WriteLine("Невозможно сохранить локальную конфигурацию: не указан путь");
                return;
            }

            var localConfigPath = Path.Combine(directoryPath, LocalConfigFolder, LocalConfigFileName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localConfigPath) ?? ".");
                var json = JsonSerializer.Serialize(LC, _jsonOptions);
                File.WriteAllText(localConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения локальной конфигурации: {ex.Message}");
            }
        }
    }
}
