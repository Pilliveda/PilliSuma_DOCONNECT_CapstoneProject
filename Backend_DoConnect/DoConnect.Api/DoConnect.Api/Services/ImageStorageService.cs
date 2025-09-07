using DoConnect.Api.Models;

namespace DoConnect.Api.Services
{
    public class ImageStorageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<List<ImageFile>> SaveFilesAsync(IEnumerable<IFormFile> files, Guid? questionId, Guid? answerId)
        {
            var saved = new List<ImageFile>();
            if (files == null) return saved;

            var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(root);

            foreach (var f in files)
            {
                if (f.Length == 0) continue;
                var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(f.FileName)}";
                var full = Path.Combine(root, safeName);
                using var fs = File.Create(full);
                await f.CopyToAsync(fs);

                saved.Add(new ImageFile
                {
                    Path = Path.Combine("uploads", safeName).Replace("\\", "/"),
                    QuestionId = questionId,
                    AnswerId = answerId
                });
            }
            return saved;
        }
        //Step 1: Check if the File was sent
        //Step 2: Creating a folder to save file if it doesn't exist
        //Step 3: Create the full file path
        //Step 4: Save the file to the folder
        //Step 5: return success message
    }
}
