using System.Collections.Generic;

namespace Rth
{
    public class Result<TInput, TOutput, TMessage>
    {
        public Result(TInput input
            , TOutput output
            , IEnumerable<TMessage> messages
            , Status status)
        {
            this.Input = input;
            this.Output = output;
            this.Messages = messages;
            this.Status = status;
        }


        public TInput Input {get;}
        public TOutput Output {get;}
        public IEnumerable<TMessage> Messages {get;}


        public Status Status {get;}
        public bool IsError => this.Status == Status.Error;
        public bool IsSuccess => this.Status == Status.Success;
        public bool IsWarning => this.Status == Status.Warning;
    }
}
