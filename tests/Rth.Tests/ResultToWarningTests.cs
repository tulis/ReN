using AutoFixture.Xunit2;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rth.Tests
{
    public class ResultToWarningTests
    {
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
            var result = output.ToWarning<string, string, string>(input, message);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Warning);
            result.IsSuccess.Should().BeFalse();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeTrue();
            result.Messages.Should().NotBeEmpty();
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [AutoData]
        [InlineAutoData(null)]
        [InlineAutoData(null, null)]
        public void WithMessages(
            string output
            , string input
            , IEnumerable<string> messages)
        {
            var result = output.ToWarning<string, string, string>(input, messages);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Warning);
            result.IsSuccess.Should().BeFalse();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeTrue();
            result.Messages.Count().Should().Be(messages.Count());
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [InlineAutoData(null)]
        [InlineAutoData(null, null, null)]
        [InlineAutoData(new string[0], null)]
        [InlineAutoData(new string[0], null, null)]
        public void WithNullMessages(
            IEnumerable<string> messages
            , string output
            , string input)
        {
            Action toWarning = () => output.ToWarning<string, string, string>(input, messages);

            toWarning.Should()
                .ThrowExactly<ArgumentException>()
                .WithMessage($"[{nameof(messages)}] cannot be empty when creating WARNING result.");
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
            var result = output.ToWarning<string, string, string>(input, messages);

            result.Should().NotBeNull();
            result.Status.Should().BeEquivalentTo(Status.Warning);
            result.IsSuccess.Should().BeFalse();
            result.IsError.Should().BeFalse();
            result.IsWarning.Should().BeTrue();
            result.Messages.Count().Should().Be(messages.Count());
            result.Input.Should().BeEquivalentTo(input);
            result.Output.Should().BeEquivalentTo(output);
        }

        [Theory]
        [InlineAutoData(null)]
        [InlineAutoData(null, null, null)]
        [InlineAutoData(new string[0], null)]
        [InlineAutoData(new string[0], null, null)]
        public void WithNullParamsMessage(
            string[] messages
            , string output
            , string input)
        {
            Action toWarning = () => output.ToWarning<string, string, string>(input, messages);

            toWarning.Should()
                .ThrowExactly<ArgumentException>()
                .WithMessage($"[{nameof(messages)}] cannot be empty when creating WARNING result.");
        }
    }
}
