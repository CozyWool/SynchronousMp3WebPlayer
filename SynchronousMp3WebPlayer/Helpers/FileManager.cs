namespace SynchronousMp3WebPlayer.Helpers;

public static class FileManager
{
    public static void DeleteFilesInDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            var filePaths = Directory.GetFiles(directoryPath);

            foreach (var filePath in filePaths)
            {
                File.Delete(filePath);
                Console.WriteLine($"Удалено: {filePath}");
            }
            Console.WriteLine($"Все файлы в '{directoryPath}' удалены.");
        }
        else
        {
            Console.WriteLine($"Папки '{directoryPath}' не существует.");
        }
    }
}