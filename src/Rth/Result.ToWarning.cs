using System;
using System.Collections.Generic;
using System.Linq;

namespace Rth
{
    public static class ResultToWarning
    {
        public static Result<TInput, TOutput, TMessage>
            ToWarning<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , TMessage message)
            {
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , new[]{message}
                    , Status.Warning);
            }

        public static Result<TInput, TOutput, TMessage>
            ToWarning<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , IEnumerable<TMessage> messages)
            {
                if(!messages.Any())
                {
                    throw new ArgumentException(
                        $"[{nameof(messages)}] cannot be empty when creating WARNING result.");
                }
                
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , messages
                    , Status.Warning);
            }

        public static Result<TInput, TOutput, TMessage>
            ToWarning<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , params TMessage[] messages)
            {
                if(!messages.Any())
                {
                    throw new ArgumentException(
                        $"[{nameof(messages)}] cannot be empty when creating WARNING result.");
                }

                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , messages
                    , Status.Warning);
            }
    }
}