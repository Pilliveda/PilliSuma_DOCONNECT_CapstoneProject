using System;
using System.Linq;
using System.Threading.Tasks;
using DoConnect.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DoConnect.Tests.Integration
{
    /// <summary>
    /// Verifies DB-level rules (CHECK constraints, cascade) on SQL Server.
    /// </summary>
    [Collection("SqlServer collection")]
    public class EfModelSqlServerTests
    {
        private readonly SqlServerDbFixture _fx;
        public EfModelSqlServerTests(SqlServerDbFixture fx) => _fx = fx;

        [Fact]
        public async Task ImageFile_Without_QuestionOrAnswer_Should_Fail_CheckConstraint()
        {
            await using var db = _fx.CreateContext();

            db.Images.Add(new ImageFile { Path = "uploads/solo.png" });
            Func<Task> act = async () => await db.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>(); // enforced by CK_Image_Target
        }

        [Fact]
        public async Task Deleting_Answer_Should_Cascade_Delete_Its_Images()
        {
            await using var db = _fx.CreateContext();

            var user = new User { Username = "u", Email = "u@e.com", PasswordHash = "hash" };
            var q = new Question { Title = "T", Text = "X", User = user };
            var a = new Answer { Text = "Ans", Question = q, User = user };
            a.Images.Add(new ImageFile { Path = "uploads/ans1.png" });
            db.AddRange(user, q, a);

            await db.SaveChangesAsync();

            db.Answers.Remove(a);
            await db.SaveChangesAsync();

            (await db.Images.CountAsync()).Should().Be(0); // cascaded
            (await db.Questions.CountAsync()).Should().Be(1);
        }
    }
}
