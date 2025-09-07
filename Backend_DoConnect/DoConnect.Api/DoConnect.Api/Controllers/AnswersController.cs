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
    [Route("api/questions/{questionId:guid}/[controller]")]
    public class AnswersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ImageStorageService _store;

        public AnswersController(AppDbContext db, ImageStorageService store)
        {
            _db = db;
            _store = store;
        }

        // POST /api/questions/{questionId}/answers
        // multipart/form-data (Text, Files)
        // For normal users => Pending by default
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Create(Guid questionId, [FromForm] AnswerCreateDto dto)
        {
            var question = await _db.Questions.FindAsync(questionId);
            if (question == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var ans = new Answer
            {
                Id = Guid.NewGuid(),                 // assign before saving files
                QuestionId = questionId,
                UserId = userId,
                Text = dto.Text,
                Status = ApproveStatus.Pending,      // user answers are pending
                                                     // CreatedAt = DateTime.UtcNow
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone)
            };

            _db.Answers.Add(ans);

            if (dto.Files?.Any() == true)
            {
                var imgs = await _store.SaveFilesAsync(dto.Files, questionId: questionId, answerId: ans.Id);
                ans.Images = imgs;
                _db.Images.AddRange(imgs);
            }

            await _db.SaveChangesAsync();
            return Created("", new { ans.Id, ans.Text, ans.Status, ans.CreatedAt });
        }

        // GET /api/questions/{questionId}/answers
        [HttpGet]
        public async Task<IActionResult> List(Guid questionId)
        {
            var isAdmin = User.IsInRole(RoleType.Admin.ToString());

            var query = _db.Answers
                .Include(a => a.User)
                .Include(a => a.Images)
                .Where(a => a.QuestionId == questionId);

            if (!isAdmin)
                query = query.Where(a => a.Status == ApproveStatus.Approved);

            var res = await query.OrderBy(a => a.CreatedAt).ToListAsync();

            return Ok(res.Select(a => new AnswerOutDto
            {
                Id = a.Id,
                Text = a.Text,
                Author = a.User.Username,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt,
                Images = a.Images.Select(i => "/" + i.Path).ToList()
            }));
        }
    }
}
