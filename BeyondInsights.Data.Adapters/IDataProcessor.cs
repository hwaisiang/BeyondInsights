using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public interface IDataProcessor
    {
        event EventHandler<EventArg<DataTable>> DataProcessingCompleted;
        event EventHandler<EventArg<Exception>> DataRetrieveError;
        event EventHandler<EventArg<string>> UpdateStatus;

        void Prefilter();
        void ProcessData();

        DataTable OutputResult { get; }
        string FilterString { get; set;}
    }
}
