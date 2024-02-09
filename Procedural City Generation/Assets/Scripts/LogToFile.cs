using System.IO;

public static class LogToFile
{
    private const string LOG_FILE_PATH = "Assets/Log.txt";

    public static void LogToTextFile(string text) => File.AppendAllText(LOG_FILE_PATH, text + "\n");

    public static void CleanLog() => File.WriteAllText(LOG_FILE_PATH, string.Empty);
}