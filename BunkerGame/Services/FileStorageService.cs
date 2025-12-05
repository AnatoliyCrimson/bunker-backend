using BunkerGame.Services;

namespace BunkerGame.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        // 1. Проверяем расширение (разрешаем только картинки)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Invalid file extension. Allowed: {string.Join(", ", allowedExtensions)}");
        }

        // 2. Генерируем уникальное имя файла
        var fileName = $"{Guid.NewGuid()}{extension}";

        // 3. Формируем путь к папке (например: wwwroot/uploads/avatars)
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folderName);

        // Если папки нет — создаем
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 4. Полный путь к файлу на диске
        var filePath = Path.Combine(uploadsFolder, fileName);

        // 5. Сохраняем
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 6. Возвращаем URL путь (для веба)
        // Важно использовать '/' для URL, даже если на Windows пути через '\'
        return $"/uploads/{folderName}/{fileName}";
    }

    public Task DeleteFileAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return Task.CompletedTask;

        // Превращаем URL (/uploads/...) обратно в путь на диске
        // TrimStart('/') убирает первый слэш, чтобы Path.Combine сработал корректно
        var relativePath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}