namespace BunkerGame.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Сохраняет файл и возвращает путь к нему (относительно корня сайта)
    /// </summary>
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    
    /// <summary>
    /// Удаляет файл (если пользователь меняет аватарку, старую лучше удалить)
    /// </summary>
    Task DeleteFileAsync(string filePath);
}