using AutoFixture.Xunit2;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
