using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xyfy.Helper
{
    public class AggregateExceptionArgs : EventArgs
    {
        public AggregateException AggregateException { get; set; }
    }
}
