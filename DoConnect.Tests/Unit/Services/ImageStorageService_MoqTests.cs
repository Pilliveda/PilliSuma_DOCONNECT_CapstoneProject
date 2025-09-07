// DoConnect.Tests/Unit/Services/ImageStorageService_MoqTests.cs
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace DoConnect.Tests.Unit.Services
{
    public class ImageStorageService_MoqTests
    {
        [Fact]
        public async Task SaveFilesAsync_saves_to_webroot_uploads_and_sets_ids()
        {
            // Arrange: a temp webroot
            var root = Path.Combine(Path.GetTempPath(), "DoConnect_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(root);

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(root);

            var svc = new ImageStorageService(env.Object);

            // two in-memory files
            IFormFile FileFrom(string name, byte[] data)
            {
                var ms = new MemoryStream(data);
                return new FormFile(ms, 0, data.Length, "files", name) { Headers = new HeaderDictionary(), ContentType = "image/png" };
            }
            var files = new List<IFormFile>
            {
                FileFrom("one.png",  new byte[]{1,2,3,4}),
                FileFrom("two.jpg",  new byte[]{5,6,7,8,9}),
            };

            var qid = Guid.NewGuid();

            // Act
            var saved = await svc.SaveFilesAsync(files, questionId: qid, answerId: null);

            // Assert
            saved.Should().HaveCount(2);
            saved.Should().OnlyContain(i => i.QuestionId == qid && i.AnswerId == null);
            saved.Should().OnlyContain(i => i.Path.StartsWith("uploads/") || i.Path.StartsWith("uploads\\"));

            // physical files exist under <webroot>/uploads/<generated>
            foreach (var img in saved)
            {
                var full = Path.Combine(root, img.Path.Replace("/", Path.DirectorySeparatorChar.ToString()));
                File.Exists(full).Should().BeTrue($"expected {full} to be written");
            }

            // Cleanup
            Directory.Delete(root, recursive: true);
        }
    }
}
