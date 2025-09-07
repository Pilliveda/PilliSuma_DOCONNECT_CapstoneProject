using System.Linq;
using DoConnect.Api.Dtos;
using DoConnect.Api.Models;
using FluentAssertions;
using Xunit;

namespace DoConnect.Tests.Unit
{
    public class DataAnnotationTests
    {
        [Fact]
        public void RegisterDto_EmailFormat_Invalid_ShouldFail()
        {
            var dto = new RegisterDto
            {
                Username = "john",
                Email = "not-an-email",
                Password = "secret!"
            };

            var results = ValidationHelpers.ValidateObject(dto);
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void LoginDto_Required_Fields_Missing_ShouldFail()
        {
            var dto = new LoginDto { UsernameOrEmail = "", Password = "" };
            var results = ValidationHelpers.ValidateObject(dto);
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void QuestionCreateDto_MaxLengths_ShouldFailWhenExceeded()
        {
            var dto = new QuestionCreateDto
            {
                Title = new string('x', 141),     // [MaxLength(140)]
                Text  = new string('y', 4001)     // [MaxLength(4000)]
            };
            var results = ValidationHelpers.ValidateObject(dto);
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void AnswerCreateDto_Text_Required_ShouldFail()
        {
            var dto = new AnswerCreateDto { Text = "" }; // Required + MaxLength(4000)
            var results = ValidationHelpers.ValidateObject(dto);
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void User_Valid_Instance_ShouldPass()
        {
            var user = new User
            {
                Username = "alice",
                Email = "alice@example.com",
                PasswordHash = "hash",
                Role = RoleType.User
            };
            var results = ValidationHelpers.ValidateObject(user);
            results.Should().BeEmpty();
        }

        [Fact]
        public void Question_Valid_Instance_ShouldPass()
        {
            var q = new Question
            {
                Title = "How to test?",
                Text = "Explain xUnit with Moq.",
                UserId = System.Guid.NewGuid()
            };
            var results = ValidationHelpers.ValidateObject(q);
            results.Should().BeEmpty();
        }

        [Fact]
        public void Answer_Valid_Instance_ShouldPass()
        {
            var a = new Answer
            {
                Text = "Use xUnit + Moq + FluentAssertions.",
                QuestionId = System.Guid.NewGuid(),
                UserId = System.Guid.NewGuid()
            };
            var results = ValidationHelpers.ValidateObject(a);
            results.Should().BeEmpty();
        }
    }
}
