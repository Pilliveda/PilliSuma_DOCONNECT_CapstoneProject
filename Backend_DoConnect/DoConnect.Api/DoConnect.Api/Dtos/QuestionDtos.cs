using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DoConnect.Api.Dtos
{
    public class QuestionCreateDto
    {
        [Required, MaxLength(140)] public string Title { get; set; } = default!;
        [Required, MaxLength(4000)] public string Text { get; set; } = default!;
        public List<IFormFile>? Files { get; set; }
    }

    public class QuestionOutDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Text { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public List<string> Images { get; set; } = new();
    }
}
