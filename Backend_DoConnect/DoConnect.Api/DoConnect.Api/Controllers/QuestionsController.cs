using DoConnect.Api.Data;
using DoConnect.Api.Dtos;
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
namespace DoConnect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ImageStorageService _store;

        public QuestionsController(AppDbContext db, ImageStorageService store)
        {
            _db = db;
            _store = store;
        }

        // POST /api/questions  (multipart/form-data: Title, Text, Files)
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Create([FromForm] QuestionCreateDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);
            // India Standard Time : added as ist
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var q = new Question
            {
                Id = Guid.NewGuid(),           // assign before saving files
                Title = dto.Title,
                Text = dto.Text,
                UserId = userId,
                Status = ApproveStatus.Pending,
                // CreatedAt = DateTime.UtcNow   // UTC time
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone)
            };

            _db.Questions.Add(q);

            if (dto.Files?.Any() == true)
            {
                var imgs = await _store.SaveFilesAsync(dto.Files, questionId: q.Id, answerId: null);
                q.Images = imgs;
                _db.Images.AddRange(imgs);
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = q.Id }, ToOutDto(q, includeAuthor: true));
        }

        // GET /api/questions
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q = null,
                                              [FromQuery] int page = 1,
                                              [FromQuery] int pageSize = 10,
                                              [FromQuery] bool includePending = false)
        {
            var isAdmin = User.IsInRole(RoleType.Admin.ToString());

            var query = _db.Questions
                .Include(x => x.User)
                .Include(x => x.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.Contains(q) || x.Text.Contains(q));

            if (!(includePending && isAdmin))
                query = query.Where(x => x.Status == ApproveStatus.Approved);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                items = items.Select(x => ToOutDto(x, includeAuthor: true))
            });
        }

        // GET /api/questions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var q = await _db.Questions
                .Include(x => x.User)
                .Include(x => x.Images)
                .Include(x => x.Answers).ThenInclude(a => a.User)
                .Include(x => x.Answers).ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (q == null) return NotFound();

            var isAdmin = User.IsInRole(RoleType.Admin.ToString());
            if (!isAdmin && q.Status != ApproveStatus.Approved) return NotFound();

            return Ok(new
            {
                question = ToOutDto(q, includeAuthor: true),
                answers = q.Answers
                    .Where(a => isAdmin || a.Status == ApproveStatus.Approved)
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new AnswerOutDto
                    {
                        Id = a.Id,
                        Text = a.Text,
                        Author = a.User.Username,
                        Status = a.Status.ToString(),
                        CreatedAt = a.CreatedAt,
                        Images = a.Images.Select(i => "/" + i.Path).ToList()
                    })
            });
        }

        private static QuestionOutDto ToOutDto(Question q, bool includeAuthor)
            => new QuestionOutDto
            {
                Id = q.Id,
                Title = q.Title,
                Text = q.Text,
                Author = includeAuthor ? q.User?.Username ?? "" : "",
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt,
                Images = q.Images.Select(i => "/" + i.Path).ToList()
            };
    }
}
