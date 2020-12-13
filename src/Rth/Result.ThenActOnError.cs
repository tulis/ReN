using System;
using System.Collections.Generic;
using System.Linq;

namespace Rth
{
    public static class ResultThenActOnError
    {
        public static IEnumerable<IEnumerable<IResult<TInput, TOutput, TMessage>>>
            ThenActOnError<TInput, TOutput, TMessage>(
                this IEnumerable<IEnumerable<IResult<TInput, TOutput, TMessage>>> previousResults
                , Action<IEnumerable<TOutput>> act)
        {
            if (previousResults == null)
            {
                throw new ArgumentNullException(paramName: nameof(previousResults));
            }

            foreach (var batch in previousResults)
            {
                var batchPreviousResult = batch.ToList();
                var inputs = batchPreviousResult
                    .Where(previousResult => previousResult.IsError)
                    .Select(previousResult => previousResult.Output)
                    .Distinct()
                    .ToArray();

                if (inputs.Any())
                {
                    act(inputs);
                }

                yield return batchPreviousResult;
            }
        }
    }
}