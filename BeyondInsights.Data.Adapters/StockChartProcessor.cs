using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public class StockChartProcessor : BaseProcessor
    {
        public StockChartProcessor(IDataProcessor processor)
            : base(processor)
        {
 
        }

        protected override void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e)
        {
            throw new NotImplementedException();
        }

        protected override void OnInnerProcessorError(EventArg<Exception> e)
        {
            throw new NotImplementedException();
        }

        public override void Prefilter()
        {
            throw new NotImplementedException();
        }

        public override void ProcessData()
        {
            throw new NotImplementedException();
        }

        public override System.Data.DataTable OutputResult
        {
            get { throw new NotImplementedException(); }
        }

        public override string FilterString
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
