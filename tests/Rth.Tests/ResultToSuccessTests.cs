using AutoFixture.Xunit2;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rth.Tests
{
    public class ResultToSuccessTests
    {
        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineData(null, null)]
        public void WithoutMessage(
            string output
            , string input)
        {
            var result = output.ToSuccess<string, string, string>(input);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Should().BeEmpty();
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        [InlineData(null, null, null)]
        public void WithMessage(
            string output
            , string input
            , string message)
        {
            var result = output.ToSuccess<string, string, string>(input, message);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Should().NotBeEmpty();
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        [InlineData(null, null, new string[0])]
        public void WithMessages(
            string output
            , string input
            , IEnumerable<string> messages)
        {
            var result = output.ToSuccess<string, string, string>(input, messages);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Count().Should().Be(messages.Count());
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        public void WithNullMessages(
            string output
            , string input)
        {
            var result = output.ToSuccess<string, string, string>(input, messages: default(IEnumerable<string>));

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Should().BeEmpty();
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        public void WithParamsMessage(
            string output
            , string input
            , params string[] messages)
        {
            var result = output.ToSuccess<string, string, string>(input, messages);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Count().Should().Be(messages.Count());
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        public void WithNullParamsMessage(
            string output
            , string input)
        {
            var result = output.ToSuccess<string, string, string>(input, messages: new string[0]);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Success);
            result.IsSuccess.Should().BeTrue();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeFalse();
            result.Messages.Should().BeEmpty();
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }
    }
}
