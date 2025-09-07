using System.ComponentModel.DataAnnotations;

namespace DoConnect.Api.Models
{
    public class Answer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(4000)]
        public string Text { get; set; } = default!;

        public ApproveStatus Status { get; set; } = ApproveStatus.Pending;

        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = default!;

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ImageFile> Images { get; set; } = new List<ImageFile>();
    }
}
