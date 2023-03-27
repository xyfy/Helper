using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xyfy.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public class AggregateExceptionArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public AggregateException? AggregateException { get; set; }
    }
}
