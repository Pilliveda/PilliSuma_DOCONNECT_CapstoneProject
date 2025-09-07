using System.IO;
using System.Text;
using System.Threading.Tasks;
using DoConnect.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace DoConnect.Tests.Unit
{
    public class ImageStorageServiceTests
    {
        private static IFormFile MakeFormFile(string filename, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", filename);
        }

        [Fact]
        public async Task SaveFilesAsync_ForQuestion_ShouldReturn_ImagesWithUploadsPrefix()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "doconnect-tests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(tmp);

            var svc = new ImageStorageService(env.Object);
            var qid = System.Guid.NewGuid();
            var files = new[] { MakeFormFile("a.png", "A"), MakeFormFile("b.jpg", "B") };

            var result = await svc.SaveFilesAsync(files, questionId: qid, answerId: null);

            result.Should().HaveCount(2);
            foreach (var img in result)
            {
                img.Path.Should().StartWith("uploads/");
                img.QuestionId.Should().Be(qid);
                img.AnswerId.Should().BeNull();
            }

            // Optional: if your service physically writes files, you can enable this:
            // foreach (var img in result)
            //     File.Exists(Path.Combine(tmp, img.Path.Replace("/", Path.DirectorySeparatorChar.ToString()))).Should().BeTrue();
        }

        [Fact]
        public async Task SaveFilesAsync_ForAnswer_ShouldLinkTo_AnswerId()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "doconnect-tests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(tmp);

            var svc = new ImageStorageService(env.Object);
            var aid = System.Guid.NewGuid();
            var files = new[] { MakeFormFile("c.gif", "C") };

            var result = await svc.SaveFilesAsync(files, questionId: null, answerId: aid);

            result.Should().HaveCount(1);
            result[0].AnswerId.Should().Be(aid);
            result[0].QuestionId.Should().BeNull();
            result[0].Path.Should().StartWith("uploads/");
        }
    }
}
