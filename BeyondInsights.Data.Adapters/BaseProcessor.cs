using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public abstract class BaseProcessor : IDataProcessor
    {
        public event EventHandler<EventArg<System.Data.DataTable>> DataProcessingCompleted;
        public event EventHandler<EventArg<Exception>> DataRetrieveError;
        public event EventHandler<EventArg<string>> UpdateStatus;
 
        protected IDataProcessor innerProcessor;

        protected BaseProcessor(IDataProcessor processor)
        {
            if (processor != null)
            {
                innerProcessor = processor;
                innerProcessor.DataProcessingCompleted += innerProcessor_DataProcessingCompleted;
                innerProcessor.DataRetrieveError += innerProcessor_DataRetrieveError;
                innerProcessor.UpdateStatus += innerProcessor_UpdateStatus;
            }
        }

        void innerProcessor_UpdateStatus(object sender, EventArg<string> e)
        {
            RaiseProcessingUpdate(e);
        }

        private void innerProcessor_DataProcessingCompleted(object sender, EventArg<System.Data.DataTable> e)
        {
            OnInnerProcessorCompleted(e);
        }

        private void innerProcessor_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            OnInnerProcessorError(e);
        }

        protected void RaiseProcessingCompleted(EventArg<System.Data.DataTable> e)
        {
            if (DataProcessingCompleted != null)
                DataProcessingCompleted(this, e);
        }

        protected void RaiseProcessingError(EventArg<Exception> e)
        {
            if (DataRetrieveError != null)
                DataRetrieveError(this, e);
        }

        protected void RaiseProcessingUpdate(EventArg<string> e)
        {
            if (UpdateStatus != null)
                UpdateStatus(this, e);
        }

        protected abstract void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e);

        protected abstract void OnInnerProcessorError(EventArg<Exception> e);


        public abstract void Prefilter();

        public abstract void ProcessData();

        public abstract System.Data.DataTable OutputResult { get; }

        public abstract string FilterString
        {
            get;
            set;
        }
    }
}
