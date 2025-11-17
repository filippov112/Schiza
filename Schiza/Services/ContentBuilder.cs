using System.IO;
using System.Text;
using Ude;

namespace Schiza.Services;

public static class ContentBuilder
{
    /// <summary>
    /// Формирует контекст проекта
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="extensions"></param>
    /// <param name="structure"></param>
    /// <returns></returns>
    public static string Build(List<string> files)
    {
        var sb = new StringBuilder();

        foreach (var file in files.Where(f => ShouldInclude(f)))
        {
            AppendFileContent(sb, file);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Функция добавления содержимого в конец текста
    /// </summary>
    /// <param name="sb">Текст содержимого всех файлов (StringBuilder)</param>
    /// <param name="fullPath">Абсолютный путь к файлу</param>
    private static void AppendFileContent(StringBuilder sb, string fullPath)
    {
        string relativePath = Path.GetRelativePath(Program.Storage.ProjectFolder, fullPath);
        Program.Storage.AddFile(sb, relativePath, ParseFile(fullPath));
    }

    /// <summary>
    /// Можно ли парсить конкретный файл
    /// </summary>
    /// <param name="fullpath">Абсолютный путь к файлу</param>
    /// <returns>Разрешение на парсинг</returns>
    public static bool ShouldInclude(string fullpath)
    {
        try
        {
            using var stream = File.OpenRead(fullpath);
            int readByte;
            int maxBytes = 512;
            int totalRead = 0;

            while ((readByte = stream.ReadByte()) != -1 && totalRead < maxBytes)
            {
                if (readByte == 0) return false; // бинарный null
                if (readByte < 7 || (readByte > 13 && readByte < 32)) return false;
                totalRead++;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Функция парсинга файла
    /// </summary>
    /// <param name="fullpath">Путь к файлу</param>
    /// <returns>Содержимое файла</returns>
    public static string ParseFile(string fullpath)
    {
        try
        {
            using var fs = File.OpenRead(fullpath);

            // Проверка BOM
            if (fs.Length >= 3)
            {
                byte[] bom = new byte[4];
                fs.Read(bom, 0, 4);
                fs.Position = 0;

                if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return new StreamReader(fs, Encoding.UTF8).ReadToEnd();
                if (bom[0] == 0xFF && bom[1] == 0xFE)
                    return new StreamReader(fs, Encoding.Unicode).ReadToEnd(); // UTF-16 LE
                if (bom[0] == 0xFE && bom[1] == 0xFF)
                    return new StreamReader(fs, Encoding.BigEndianUnicode).ReadToEnd(); // UTF-16 BE
            }

            // Определение кодировки через Ude
            var detector = new CharsetDetector();
            detector.Feed(fs);
            detector.DataEnd();

            if (detector.Charset != null)
            {
                fs.Position = 0;
                Encoding encoding = Encoding.GetEncoding(detector.Charset);
                using var reader = new StreamReader(fs, encoding);
                return reader.ReadToEnd();
            }

            // По умолчанию — UTF-8 без BOM
            fs.Position = 0;
            return new StreamReader(fs, new UTF8Encoding(false)).ReadToEnd();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка определения кодировки: {ex.Message}");
            throw;
        }
    }
}
