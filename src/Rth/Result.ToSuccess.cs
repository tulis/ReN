using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rth
{
    public static class ResultToSuccess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
            ToSuccess<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input)
            {
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , Enumerable.Empty<TMessage>()
                    , Status.Success);
            }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
            ToSuccess<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , TMessage message)
            {
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , new[]{message}
                    , Status.Success);
            }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
            ToSuccess<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , IEnumerable<TMessage> messages)
            {
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , messages ?? Enumerable.Empty<TMessage>()
                    , Status.Success);
            }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult<TInput, TOutput, TMessage>
            ToSuccess<TInput, TOutput, TMessage>(
                this TOutput output
                , TInput input
                , params TMessage[] messages)
            {
                return new Result<TInput, TOutput, TMessage>(
                    input
                    , output
                    , messages ?? Enumerable.Empty<TMessage>()
                    , Status.Success);
            }
    }
}