using System.Text;
using Schiza.Model;

namespace Schiza.Services;

public class StorageService
{
    #region Constants
    /// <summary>
    /// ������������� ������� �������� ���������
    /// </summary>
    public const string GlobalConfigPath = "./config/global_config.json";
    /// <summary>
    /// ���� �������� �������
    /// </summary>
    public const string LocalConfigFileName = "local_config.json";
    /// <summary>
    /// ������� ��������� ������ �������
    /// </summary>
    public const string LocalConfigFolder = ".Schiza";
    /// <summary>
    /// - ###input### - ������� �����
    /// </summary>
    public const string KEY_WORD_INPUT = "###input###";
    /// <summary>
    /// - ###project### - ��������� �������
    /// </summary>
    public const string KEY_WORD_PROJECT = "###project###";
    /// <summary>
    /// - ###settings### - ���������������� ��������� 
    /// </summary>
    public const string KEY_WORD_SETTINGS = "###settings###";
    /// <summary>
    /// - ###request### - ������ ������������
    /// </summary>
    public const string KEY_WORD_REQUEST = "###request###";
    /// <summary>
    /// - ###path### - ������������� ���� � �����
    /// </summary>
    public const string KEY_WORD_PATH = "###path###";
    /// <summary>
    /// - ###content### - ���������� �����
    /// </summary>
    public const string KEY_WORD_CONTENT = "###content###";
    #endregion

    private ConfigService cs;
    public StorageService()
    {
        cs = new(GlobalConfigPath, LocalConfigFileName, LocalConfigFolder);
    }
    public void LoadGlobalConfig()
    {
        cs.LoadGlobalConfig();
    }
    public void LoadLocalConfig(string directoryPath)
    {
        cs.LoadLocalConfig(directoryPath);
    }

    public LocalConfig LConfig => cs.LC;
    public GlobalConfig GConfig => cs.GC;

    public void SaveSettings(GlobalConfig globalConfig, LocalConfig localConfig)
    {
        cs.LC = localConfig;
        cs.GC = globalConfig;
        cs.SaveGlobalConfig();
        if (!string.IsNullOrEmpty(ProjectFolder))
            cs.SaveLocalConfig(ProjectFolder);
    }


    // ������ ��������� ������

    /// <summary>
    /// ��������� ���������� ������� � ������ ��������� �������, ���������������� �������� � ������� ������������
    /// </summary>
    /// <param name="projectStructure">���������� �������</param>
    /// <param name="userRequest">���������������� ������</param>
    /// <returns></returns>
    public string GetPrompt(string projectStructure, string userRequest)
    {
        // ���� �� ����� ��������� ������ ���������, ���������� ����������
        string result = string.IsNullOrWhiteSpace(cs.LC.StructurePromptLocal) ? cs.GC.StructurePromptGlobal : cs.LC.StructurePromptLocal;

        result = result.Replace(KEY_WORD_INPUT, cs.LC.InputProjectPrompt);
        result = result.Replace(KEY_WORD_PROJECT, projectStructure);
        result = result.Replace(KEY_WORD_SETTINGS, cs.GC.UserSettingsPrompt);
        result = result.Replace(KEY_WORD_REQUEST, userRequest);
        return result;
    }

    /// <summary>
    /// ���������� ����� ����� ��� �������� � ������
    /// </summary>
    /// <param name="relativePath">���� � �����</param>
    /// <param name="fileContent">����������</param>
    /// <returns></returns>
    public void AddFile(StringBuilder sb, string relativePath, string fileContent)
    {
        sb.AppendLine(
            cs.GC.StyleFileBlock
            .Replace(KEY_WORD_PATH, relativePath)
            .Replace(KEY_WORD_CONTENT, fileContent)
        );
    }

    private string _projectFolder = string.Empty;
    public string ProjectFolder
    {
        get => _projectFolder;
        set
        {
            _projectFolder = value;
            LoadLocalConfig(_projectFolder);
        }
    }
}