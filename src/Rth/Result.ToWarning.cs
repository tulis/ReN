using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rth
{
    public static class ResultToWarning
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
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