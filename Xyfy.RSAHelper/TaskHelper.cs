using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xyfy.Helper
{
    public class TaskHelper
    {
        event EventHandler<AggregateExceptionArgs> AggregateExceptionCatched;

        private readonly TaskFactory factory;

        private readonly Action<Exception> logAction;

        public TaskHelper(Action<Exception> errorLogAction = null)
        {
            logAction = errorLogAction ?? Console.WriteLine;
            AggregateExceptionCatched += new EventHandler<AggregateExceptionArgs>(Program_AggregateExceptionCatched);
            factory = Task.Factory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workAction"></param>
        /// <returns></returns>
        public Task StartNew(Action workAction)
        {
            return ContinueWith(factory.StartNew(workAction));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private Task ContinueWith(Task task)
        {
            return task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    AggregateExceptionArgs errArgs = new AggregateExceptionArgs()
                    {
                        AggregateException = new AggregateException(t.Exception.InnerExceptions)
                    };
                    AggregateExceptionCatched(null, errArgs);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartNew(Action action, CancellationToken cancellationToken)
        {
            return ContinueWith(factory.StartNew(action, cancellationToken));
        }

        public Task StartNew(Action action, TaskCreationOptions creationOptions)
        {
            return ContinueWith(factory.StartNew(action, creationOptions));
        }

        public Task StartNew(Action action, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return ContinueWith(factory.StartNew(action, cancellationToken, creationOptions, scheduler));
        }
        private void Program_AggregateExceptionCatched(object sender, AggregateExceptionArgs e)
        {
            foreach (Exception item in e.AggregateException.InnerExceptions)
            {
                logAction(item);
            }
        }

    }
}
