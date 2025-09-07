namespace DoConnect.Api.Models
{
    public class ImageFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Path { get; set; } = default!;   // relative like "uploads/..."
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Guid? QuestionId { get; set; }
        public Question? Question { get; set; }

        public Guid? AnswerId { get; set; }
        public Answer? Answer { get; set; }
    }
}
