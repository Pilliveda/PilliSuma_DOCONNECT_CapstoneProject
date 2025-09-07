using System.ComponentModel.DataAnnotations;

namespace DoConnect.Api.Models
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(140)]
        public string Title { get; set; } = default!;

        [Required, MaxLength(4000)]
        public string Text { get; set; } = default!;

        public ApproveStatus Status { get; set; } = ApproveStatus.Pending;

        // FK
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<ImageFile> Images { get; set; } = new List<ImageFile>();
    }
}
