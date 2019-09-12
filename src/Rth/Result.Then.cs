using System;
using System.Collections.Generic;
using System.Linq;

namespace Rth
{
    public static class ResultThen
    {
        public static IEnumerable<IEnumerable<IResult<TInput, TNextOutput, TMessage>>>
            Then<TInput, TPreviousOutput, TNextOutput, TMessage>(
                this IEnumerable<IEnumerable<IResult<TInput,TPreviousOutput, TMessage>>> previousResults
                , Func<IEnumerable<TPreviousOutput>
                    , IEnumerable<IEnumerable<IResult<TPreviousOutput, TNextOutput, TMessage>>>> 
                    @do
                , Func<TInput, TMessage> onNotFoundMessage)
            {
                if(previousResults == null)
                {
                    throw new ArgumentNullException(paramName: nameof(previousResults));
                }

                foreach(var batchPreviousResult in previousResults)
                {
                    var inputs = batchPreviousResult
                        .Where(previousResult => !previousResult.IsError)
                        .Select(previousResult => previousResult.Output)
                        .Distinct()
                        .ToArray();

                    var failedNextOutputs = batchPreviousResult
                        .Where(previousResult => previousResult.IsError)
                        .Select(previousResult => default(TNextOutput)
                            .ToError(previousResult.Input, previousResult.Messages))
                        .ToList();

                    if(!inputs.Any())
                    {
                        if(failedNextOutputs.Any())
                        {
                            yield return failedNextOutputs;
                        }
                        continue;
                    }

                    var unMatchedInputs = inputs.Select(_ => _).ToList();
                    foreach (var batchNextResult in @do(inputs))
                    {
                        var matchedInputs = batchPreviousResult.Join(batchNextResult
                            , outerKeySelector: previousOutput => previousOutput.Output
                            , innerKeySelector: nextOutput => nextOutput.Input
                            , resultSelector: (previousOutput, joinNextOutput) =>
                            {
                                unMatchedInputs.Remove(previousOutput.Output);

                                if (joinNextOutput.IsError)
                                {
                                    return joinNextOutput.Output.ToError(previousOutput.Input, joinNextOutput.Messages);
                                }
                                else if (joinNextOutput.IsWarning)
                                {
                                    return joinNextOutput.Output.ToWarning(previousOutput.Input, joinNextOutput.Messages);
                                }
                                else
                                {
                                    return joinNextOutput.Output.ToSuccess<TInput, TNextOutput, TMessage>(previousOutput.Input);
                                }
                            })
                            .ToList();

                        if (matchedInputs.Any())
                        {
                            yield return matchedInputs;
                        }
                    }

                    var unMatchedResults = unMatchedInputs.Join(batchPreviousResult
                        , outerKeySelector: unMatchedInput => unMatchedInput
                        , innerKeySelector: previousOutput => previousOutput.Output
                        , resultSelector: (unMatchedInput, joinPreviousOutput)
                            => default(TNextOutput)
                                .ToError(message: onNotFoundMessage(joinPreviousOutput.Input)
                                    , input: joinPreviousOutput.Input));

                    yield return failedNextOutputs.Concat(unMatchedResults).ToList();
                }
            }
    }
}