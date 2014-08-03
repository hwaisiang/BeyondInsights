using BeyondInsights.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public class FundamentalFilterProcessor : BaseProcessor
    {
        
        public FundamentalFilterProcessor(IDataProcessor processor):base(processor)
        {
        }

        public override void Prefilter()
        {
            innerProcessor.Prefilter();
            FilterString = FilterString + ConstructFundamentalFilter();
        }

        public override void ProcessData()
        {
            innerProcessor.ProcessData(); 
        }

        public override System.Data.DataTable OutputResult
        {
            get { return innerProcessor.OutputResult; }
        }

        public override string FilterString
        {
            get
            {
                return innerProcessor.FilterString;
            }
            set
            {
                innerProcessor.FilterString = value;
            }
        }

        protected override void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e)
        {
            RaiseProcessingCompleted(e);
        }

        protected override void OnInnerProcessorError(EventArg<Exception> e)
        {
            RaiseProcessingError(e);
        }

        private string ConstructFundamentalFilter()
        {
            RaiseProcessingUpdate(new EventArg<string>("Constructing filters..."));

            string filter = string.Empty;
            foreach (FundamentalRuleConfig r in ConfigurationUtil.FundamentalRuleConfiguration.FundamentalRuleSettings)
            {
                var ruleValue = r.GetSelectedRule();
                if(ruleValue !=null)
                    filter = filter + ruleValue.Value + ",";
            }

            if (filter.Length != 0)
                filter = "&f=" + filter.TrimEnd(',');

            return filter;
        }

        
    }
}
