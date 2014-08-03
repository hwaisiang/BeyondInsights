using BeyondInsights.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace BeyondInsights.Data.Adapters
{
    public class YahooFinanceProcessor : BaseProcessor
    {
        
        string exportUrl = string.Empty;
        object obj = new object();

        private static int querySetCount = 0;
        public YahooFinanceProcessor(IDataProcessor processor)
            : base(processor)        
        {
            exportUrl = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.YahooFinanceURL].Value;
            if (string.IsNullOrEmpty(exportUrl))
            {
                exportUrl = "http://finance.yahoo.com/d/quotes.csv?s={0}&f={1}";
            }
        }

        protected override void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e)
        {
            try
            {
                querySetCount = 0;

                RaiseProcessingUpdate(new EventArg<string>("Getting Yahoo Finance data..."));
                var col = e.Data.Columns.Add(FieldConstant.ExDividendDate, typeof(string));
                col = e.Data.Columns.Add(FieldConstant.DividendPayDate, typeof(string));
                col = e.Data.Columns.Add(FieldConstant.DividendYield, typeof(string));
                col = e.Data.Columns.Add(FieldConstant.TargetPrice1y, typeof(float));

                //get the filtered data
                //contruct the data
                //get back the table

                var result = ConstructURL(e.Data);

                foreach (string url in result)
                {
                    CsvDataAdapter adapter = new CsvDataAdapter(false);
                    adapter.DataRetrieveCompleted += adapter_DataRetrieveCompleted;
                    adapter.DataRetrieveError += adapter_DataRetrieveError;
                    adapter.CurrentURL = url;
                    adapter.RetrieveData();
                    Interlocked.Increment(ref querySetCount);
                }
            }
            catch (Exception err)
            {

                RaiseProcessingError(new EventArg<Exception>(err));
            }
        }

        private List<string> ConstructURL(DataTable table )
        {
            List<List<string>> output = new List<List<string>>();

            List<string> tempArr = new List<string>();
            output.Add(tempArr);
            for (int i = 0; i< table.Rows.Count; i++)
            {
                string tickerName = (string)table.Rows[i][FieldConstant.Ticker];
                tempArr.Add(tickerName);

                if (tempArr.Count >= 100)
                {
                    tempArr = new List<string>();
                    output.Add(tempArr);
                }
            }

            List<string> result = new List<string>();
            output.ForEach(c => 
            {
                string quote = string.Empty;
                foreach (string ticker in c)
                {
                    quote = quote + "+" + ticker;
                }
                //s=symbol
                //q=ex dividend date
                //r1=dividend pay date
                //y=dividend yield
                //t8=1 year target
                string url = string.Format(exportUrl, quote.TrimStart('+'), "sqr1yt8");
                result.Add(url);
            });

            return result ;
        }
        private void adapter_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            Interlocked.Decrement(ref querySetCount);
            RaiseProcessingError(e); 
        }

        private void adapter_DataRetrieveCompleted(object sender, EventArg<System.Data.DataTable> e)
        {
            lock (obj)
            {
                RaiseProcessingUpdate(new EventArg<string>("Yahoo finance data received. Processing data.."));

                foreach(DataRow r in e.Data.Rows)
                {
                    //get the ticker name
                    //merge with the original table
             
                    string ticker = (string)r[0];
                    RaiseProcessingUpdate(new EventArg<string>(string.Format("Yahoo finance processing data..{0}",ticker)));
                    var row = this.OutputResult.Rows.Find(ticker);
                    if (row != null)
                    {
                        row[FieldConstant.ExDividendDate] = r[1];
                        row[FieldConstant.DividendPayDate] = r[2];
                        row[FieldConstant.DividendYield] = r[3];
                        float targetPrice=0;
                        float.TryParse(r[4].ToString(), out targetPrice);
                        row[FieldConstant.TargetPrice1y] = targetPrice;
                    }
                }
                Interlocked.Decrement(ref querySetCount);
            }

            if (querySetCount ==0)
            {
                RaiseProcessingCompleted(new EventArg<DataTable>(this.OutputResult));
            }
        }

        protected override void OnInnerProcessorError(EventArg<Exception> e)
        {
            RaiseProcessingError(e);
        }

        public override void Prefilter()
        {
            innerProcessor.Prefilter();
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
                innerProcessor.FilterString = string.Empty;
            }
        }
    }
}
