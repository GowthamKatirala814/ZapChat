using System.Reflection;

namespace Shared.Moderation;

public class ModerationDictionaryLoader
{
    private readonly string _dictionariesPath;

    public ModerationDictionaryLoader(string dictionariesPath = "")
    {
        if (string.IsNullOrEmpty(dictionariesPath))
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            var dir = Path.GetDirectoryName(codeBase) ?? string.Empty;
            _dictionariesPath = Path.Combine(dir, "Dictionaries");
        }
        else
        {
            _dictionariesPath = dictionariesPath;
        }
    }

    public HashSet<string> LoadDictionary(string fileName)
    {
        var filePath = Path.Combine(_dictionariesPath, fileName);
        if (!File.Exists(filePath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var lines = File.ReadAllLines(filePath)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));

        return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
    }
}
