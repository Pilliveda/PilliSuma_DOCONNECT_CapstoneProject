using DoConnect.Api.Data;
using DoConnect.Api.Dtos;
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoConnect.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ImageStorageService _store;

        public AdminController(AppDbContext db, ImageStorageService store)
        {
            _db = db;
            _store = store;
        }

        // --- Moderation: Questions ---
        [HttpPost("questions/{id:guid}/approve")]
        public async Task<IActionResult> ApproveQuestion(Guid id)
        {
            var q = await _db.Questions.FindAsync(id);
            if (q == null) return NotFound();
            q.Status = ApproveStatus.Approved;
            await _db.SaveChangesAsync();
            return Ok(new { q.Id, q.Status });
        }

        [HttpPost("questions/{id:guid}/reject")]
        public async Task<IActionResult> RejectQuestion(Guid id)
        {
            var q = await _db.Questions.FindAsync(id);
            if (q == null) return NotFound();
            q.Status = ApproveStatus.Rejected;
            await _db.SaveChangesAsync();
            return Ok(new { q.Id, q.Status });
        }

        // --- Moderation: Answers ---
        [HttpPost("answers/{id:guid}/approve")]
        public async Task<IActionResult> ApproveAnswer(Guid id)
        {
            var a = await _db.Answers.FindAsync(id);
            if (a == null) return NotFound();
            a.Status = ApproveStatus.Approved;
            await _db.SaveChangesAsync();
            return Ok(new { a.Id, a.Status });
        }

        [HttpPost("answers/{id:guid}/reject")]
        public async Task<IActionResult> RejectAnswer(Guid id)
        {
            var a = await _db.Answers.FindAsync(id);
            if (a == null) return NotFound();
            a.Status = ApproveStatus.Rejected;
            await _db.SaveChangesAsync();
            return Ok(new { a.Id, a.Status });
        }

        // --- Admin delete question (with images & answers cascade) ---
        [HttpDelete("questions/{id:guid}")]
        public async Task<IActionResult> DeleteQuestion(Guid id)
        {
            var q = await _db.Questions
                .Include(x => x.Images)
                .Include(x => x.Answers).ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (q == null) return NotFound();

            var answerImages = q.Answers.SelectMany(a => a.Images).ToList();
            if (answerImages.Count > 0) _db.Images.RemoveRange(answerImages);

            if (q.Images?.Count > 0) _db.Images.RemoveRange(q.Images);

            if (q.Answers?.Count > 0) _db.Answers.RemoveRange(q.Answers);

            _db.Questions.Remove(q);

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // --- Admin review queues ---
        [HttpGet("questions/pending")]
        public async Task<IActionResult> GetPendingQuestions()
        {
            var pending = await _db.Questions
                .Include(q => q.User)
                .Include(q => q.Images)
                .Where(q => q.Status == ApproveStatus.Pending)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return Ok(pending.Select(q => new
            {
                q.Id,
                q.Title,
                q.Text,
                Author = q.User.Username,
                q.Status,
                q.CreatedAt,
                Images = q.Images.Select(i => "/" + i.Path).ToList()
            }));
        }

        [HttpGet("answers/pending")]
        public async Task<IActionResult> GetPendingAnswers()
        {
            var pending = await _db.Answers
                .Include(a => a.User)
                .Include(a => a.Images)
                .Include(a => a.Question)
                .Where(a => a.Status == ApproveStatus.Pending)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(pending.Select(a => new
            {
                a.Id,
                a.Text,
                QuestionTitle = a.Question.Title,
                Author = a.User.Username,
                a.Status,
                a.CreatedAt,
                Images = a.Images.Select(i => "/" + i.Path).ToList()
            }));
        }

        // --- NEW: Admin can post an answer (auto-approved) ---
        // POST /api/admin/questions/{questionId}/answers
        // multipart/form-data: Text (required), Files (optional)
        [HttpPost("questions/{questionId:guid}/answers")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> PostAnswerAsAdmin(Guid questionId, [FromForm] AnswerCreateDto dto)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(adminIdStr)) return Unauthorized();
            var adminId = Guid.Parse(adminIdStr);

            var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId);
            if (q == null) return NotFound();

            var a = new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = questionId,
                UserId = adminId,
                Text = dto.Text,
                Status = ApproveStatus.Approved, // auto-approve admin answers
                CreatedAt = DateTime.UtcNow
            };

            _db.Answers.Add(a);

            if (dto.Files?.Any() == true)
            {
                var imgs = await _store.SaveFilesAsync(dto.Files, questionId: questionId, answerId: a.Id);
                a.Images = imgs;
                _db.Images.AddRange(imgs);
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(QuestionsController.GetById),
                "Questions",
                new { id = questionId },
                new
                {
                    a.Id,
                    a.Text,
                    a.Status,
                    a.CreatedAt,
                    Images = a.Images?.Select(i => "/" + i.Path).ToList() ?? new List<string>()
                });
        }


        // --- NEW: Admin can create a question (auto-approved) ---
        // POST /api/admin/questions
        // multipart/form-data: Title (required), Text (required), Files (optional)
        [HttpPost("questions")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> CreateQuestionAsAdmin([FromForm] QuestionCreateDto dto)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(adminIdStr)) return Unauthorized();
            var adminId = Guid.Parse(adminIdStr);

            var q = new Question
            {
                Id = Guid.NewGuid(),                 // assign before saving files
                Title = dto.Title,
                Text = dto.Text,
                UserId = adminId,
                Status = ApproveStatus.Approved,     // auto-approve admin questions
                CreatedAt = DateTime.UtcNow
            };

            _db.Questions.Add(q);

            if (dto.Files?.Any() == true)
            {
                var imgs = await _store.SaveFilesAsync(dto.Files, questionId: q.Id, answerId: null);
                q.Images = imgs;
                _db.Images.AddRange(imgs);
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(QuestionsController.GetById),
                "Questions",
                new { id = q.Id },
                new
                {
                    q.Id,
                    q.Title,
                    q.Text,
                    q.Status,
                    q.CreatedAt,
                    Images = q.Images?.Select(i => "/" + i.Path).ToList() ?? new List<string>()
                });
        }
    }
}
