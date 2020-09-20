using AutoFixture.Xunit2;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rth.Tests
{
    public class ResultThenTests
    {
        [Fact]
        public void PreviousResultsNull()
        {
            var previousResults = default(IEnumerable<IEnumerable<IResult<decimal, int, string>>>);

            Action then = () => previousResults
                .Then<decimal, int, long, string>(previousOutputs => previousOutputs
                        .Select(previousOutput => new[] { default(long).ToSuccess<int, long, string>(previousOutput) })
                    , onNotFoundMessage: input => String.Empty)
                .ToList();

            then.Should()
                .ThrowExactly<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'previousResults')");
        }

        [Theory]
        [InlineAutoData]
        public void ThenTests()
        {
            var originalInputs = new[]{Enumerable.Range(0, count: 16)
                .Select(index => index.ToSuccess<int, int, string>(index)).ToArray()};

            var results = originalInputs
                .Then(ModToLong
                    , onNotFoundMessage: (input) => $"Could not find long output for int input [{input}] due to side effect, such as exception maybe.")
                .Then(LongToDouble
                    , onNotFoundMessage: (input) => $"Could not find double output for long input [{input}] due to side effect, such as exception maybe.")
                .ToList();

            results.Should().HaveCount(133);
            results.SelectMany(_ => _.Where(result => result.IsSuccess)).Should().HaveCount(16 * 4);
            results.SelectMany(_ => _.Where(result => result.IsWarning)).Should().HaveCount(16 * 4);
            results.SelectMany(_ => _.Where(result => result.IsError)).Should().HaveCount(16 * 4 + 4);

            IEnumerable<IEnumerable<IResult<int, long, string>>>
                ModToLong(IEnumerable<int> inputs)
            {
                var results = inputs
                    .Select(input =>
                    {
                        if (input % 4 == 0)
                        {
                            return Enumerable
                                .Range(input * 100, count: 16)
                                .Select(index => ((long)index).ToSuccess<int, long, string>(input
                                    , message: $"int input {input} % 4 == 0 is successful"));
                        }
                        else if (input % 4 == 1)
                        {
                            return Enumerable
                                .Range(input * 100, count: 16)
                                .Select(index => ((long)index + Int32.MaxValue)
                                    .ToWarning<int, long, string>(input
                                    , message: $"int input {input} % 4 == 1 causing output exceeds Int32.MaxValue"));
                        }
                        else if (input % 4 == 2)
                        {
                            return Enumerable
                                .Range(input * 100, count: 16)
                                .Select(index => ((long) -index - Int32.MaxValue)
                                    .ToError<int, long, string>(input
                                        , message: $"int input {input} % 4 == 2 causing output becomes negative value"));
                        }
                        else
                        {
                            // For onNotFoundMessage scenario
                            return new[] { (default(long)).ToSuccess<int, long, string>(-1) };
                        }
                    });

                return results.ToList();
            }

            IEnumerable<IEnumerable<IResult<long, double, string>>>
                LongToDouble(IEnumerable<long> inputs)
            {
                var results = inputs
                    .Select(input =>
                    {
                        return Enumerable
                            .Range(0, count: 1)
                            .Select(index => ((double)index).ToSuccess<long, double, string>(input
                                , message: $"long input {input} to double is successful."));
                    });

                return results.ToList();
            }
        }
    }
}
