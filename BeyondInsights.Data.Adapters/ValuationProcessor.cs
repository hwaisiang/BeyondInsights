using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public class ValuationProcessor : BaseProcessor
    {
        const int YearCount = 5;
        ConcurrentDictionary<string, HistoricalDataAdapter> adapters;
        object obj = new object();
        public ValuationProcessor(IDataProcessor processor)
            : base(processor)
        {
            adapters = new ConcurrentDictionary<string, HistoricalDataAdapter>();
        }

        protected override void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e)
        {

                RaiseProcessingUpdate(new EventArg<string>("Begin valuation calculation..."));

                //construct more PE derived columns
                var col = e.Data.Columns.Add(HistoricalDataAdapter.Low5Year_PE, typeof(float));
                col = e.Data.Columns.Add(HistoricalDataAdapter.High5Year_PE, typeof(float));
                col = e.Data.Columns.Add(HistoricalDataAdapter.Low10Year_PE, typeof(float));
                col = e.Data.Columns.Add(HistoricalDataAdapter.High10Year_PE, typeof(float));

                List<string> columnLabels = new List<string>();
                columnLabels.Add(HistoricalDataAdapter.CurrentPE + "_{0}");
                columnLabels.Add(HistoricalDataAdapter.Low5Year_PE + "_{0}");
                columnLabels.Add(HistoricalDataAdapter.High5Year_PE + "_{0}");
                columnLabels.Add(HistoricalDataAdapter.Low10Year_PE + "_{0}");
                columnLabels.Add(HistoricalDataAdapter.High10Year_PE + "_{0}");

                //construct 5 years growth potential columns
               
                foreach (string label in columnLabels)
                {
                    int year = DateTime.Now.Year-1;
                    for (int i = 0; i < YearCount; i++)
                    {
                        e.Data.Columns.Add(string.Format(label, year), typeof(float));
                        year = year+1;
                    }
                }

                foreach (DataRow row in e.Data.Rows)
                {
                    //get the ticker name
                    var value = (string)row[FieldConstant.Ticker];

                    //get the exchange
                    var exchange = (string)row[FieldConstant.Exchange];

                    HistoricalDataAdapter adapter = new HistoricalDataAdapter();
                    adapter.DataRetrieveCompleted += adapter_DataRetrieveCompleted;
                    adapter.DataRetrieveError += adapter_DataRetrieveError;
                    adapters.TryAdd(value, adapter);
                    string[] values = exchange.Split('=');
                    if (values.Length == 2)
                    {
                        Exchange ex = Exchange.NYSE;

                        Enum.TryParse(values[0], true, out ex);
                        RaiseProcessingUpdate(new EventArg<string>("Retrieving PE for ..." + value));
                        adapter.RetrieveData(value, ex);
                    }

                }
           
        }

        void adapter_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            RaiseProcessingError(e);
        }

        private void adapter_DataRetrieveCompleted(object sender, EventArg<System.Data.DataTable> e)
        {
            lock (obj)
            {
                //get the ticker name
                if (e.Data.Rows.Count != 0)
                {

                    string ticker = (string)e.Data.Rows[0][HistoricalDataAdapter.TickerName];

                    RaiseProcessingUpdate(new EventArg<string>("Begin valuation calculation for " + ticker));


                    //construct the growth potential using current PE, 5 year high, 5 year low, 10 year low, 10 year high
                    HistoricalDataAdapter adapter;
                    adapters.TryRemove(ticker, out adapter);

                    //locate the ticker row
                    var rows = innerProcessor.OutputResult.Select(string.Format("Ticker='{0}'", ticker));

                    if (rows.Count() == 1) //there should only be 1 unique ticker
                    {

                        //get the row
                        DataRow row = rows[0];

                        //get the current price
                        var currentPrice = float.Parse((string)row[FieldConstant.Price]);

                        //get current EPS
                        double EPS = double.Parse((string)row[FieldConstant.EPS]);

                        //get  EPS next 5 years
                        var EPSGrowthNext5Y = double.Parse(((string)row[FieldConstant.EPS5Y]).TrimEnd(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol.ToCharArray())) / 100d;

                        Dictionary<string, float> PEColumnPair = new Dictionary<string, float>();
                        PEColumnPair.Add(HistoricalDataAdapter.Low10Year_PE, (float)e.Data.Rows[0][HistoricalDataAdapter.Low10Year_PE]);
                        PEColumnPair.Add(HistoricalDataAdapter.High10Year_PE, (float)e.Data.Rows[0][HistoricalDataAdapter.High10Year_PE]);
                        PEColumnPair.Add(HistoricalDataAdapter.Low5Year_PE, (float)e.Data.Rows[0][HistoricalDataAdapter.Low5Year_PE]);
                        PEColumnPair.Add(HistoricalDataAdapter.High5Year_PE, (float)e.Data.Rows[0][HistoricalDataAdapter.High5Year_PE]);
                        PEColumnPair.Add(HistoricalDataAdapter.CurrentPE, (float)e.Data.Rows[0][HistoricalDataAdapter.CurrentPE]);

                        row[HistoricalDataAdapter.Low5Year_PE] = (float)e.Data.Rows[0][HistoricalDataAdapter.Low5Year_PE];
                        row[HistoricalDataAdapter.High5Year_PE] = (float)e.Data.Rows[0][HistoricalDataAdapter.High5Year_PE];
                        row[HistoricalDataAdapter.Low10Year_PE] = (float)e.Data.Rows[0][HistoricalDataAdapter.Low10Year_PE];
                        row[HistoricalDataAdapter.High10Year_PE] = (float)e.Data.Rows[0][HistoricalDataAdapter.High10Year_PE];

                        var currentEPS = EPS;
                        int year = DateTime.Now.Year-1;
                        for (int i = 0; i < YearCount; i++)
                        {
                            foreach (var PEPair in PEColumnPair)
                            {
                                string label = PEPair.Key;
                                float currentPE = PEPair.Value;

                                string columnName = string.Format(label + "_{0}", year);

                                //calculate the price
                                double price = currentEPS * currentPE;

                                row[columnName] = (price / currentPrice);
                            }
                            year = year + 1;
                            currentEPS = currentEPS * (1 + EPSGrowthNext5Y);
                        }
                    }

                    RaiseProcessingUpdate(new EventArg<string>("Completed valuation calculation for " + ticker));

                }
            }

            if (adapters.Count == 0)
                RaiseProcessingCompleted(new EventArg<DataTable>(innerProcessor.OutputResult));
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
                innerProcessor.FilterString = value;
            }
        }
    }
}
