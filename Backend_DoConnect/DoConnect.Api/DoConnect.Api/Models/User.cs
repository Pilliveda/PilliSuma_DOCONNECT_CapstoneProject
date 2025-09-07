using System.ComponentModel.DataAnnotations;

namespace DoConnect.Api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(40)]
        public string Username { get; set; } = default!;

        [Required, MaxLength(120)]
        public string Email { get; set; } = default!;

        [Required] public string PasswordHash { get; set; } = default!;
        [Required] public RoleType Role { get; set; } = RoleType.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
