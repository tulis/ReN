using System.Collections.Generic;

namespace Rth
{
    public interface IResult<out TInput, out TOutput, out TMessage>
    {
        public TInput Input {get;}
        public TOutput Output {get;}
        public IEnumerable<TMessage> Messages {get;}


        public Status Status {get;}
        public bool IsError => this.Status == Status.Error;
        public bool IsSuccess => this.Status == Status.Success;
        public bool IsWarning => this.Status == Status.Warning;
    }
}