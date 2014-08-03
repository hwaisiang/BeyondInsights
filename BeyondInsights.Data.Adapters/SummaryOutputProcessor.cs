using BeyondInsights.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public class SummaryOutputProcessor : BaseProcessor
    {
        DataTable table;
        string charturlBase= string.Empty;
        public SummaryOutputProcessor(IDataProcessor processor)
            : base(processor)
        {

            charturlBase = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.ChartUrl].Value;

            if (string.IsNullOrEmpty(charturlBase))
            {
                charturlBase = "http://chart.finance.yahoo.com/z?s={0}&t=1y&q=c&l=on&z=l&p=e20,e50,e200,p&a=v,m26-12-9";
            }

            table = new DataTable();
            table.Columns.Add(FieldConstant.Ticker, typeof(string));
            table.Columns.Add(FieldConstant.Company, typeof(string));
            table.Columns.Add(FieldConstant.Industry , typeof(string));
            table.Columns.Add(FieldConstant.Price, typeof(float));
            table.Columns.Add(FieldConstant.PEG, typeof(float));
            table.Columns.Add(FieldConstant.ROA, typeof(string));
            table.Columns.Add(FieldConstant.ROE, typeof(string));
            table.Columns.Add(FieldConstant.InsiderTran, typeof(string));
            table.Columns.Add(FieldConstant.InstOwn, typeof(string));
            table.Columns.Add(FieldConstant.AnalystRecom, typeof(string));
            table.Columns.Add(FieldConstant.EarningDates, typeof(string));
            table.Columns.Add(FieldConstant.DividendPayDate, typeof(string));
            table.Columns.Add(FieldConstant.ExDividendDate, typeof(string));
            table.Columns.Add(FieldConstant.DividendYield, typeof(string));
            table.Columns.Add(FieldConstant.TargetPrice1y, typeof(float));
            table.Columns.Add(FieldConstant.CurrentTargetPriceDiff, typeof(float));
            table.Columns.Add(FieldConstant._50Day, typeof(float));
            table.Columns.Add(FieldConstant._20Day, typeof(float));
            table.Columns.Add(FieldConstant._200Day, typeof(float));
            table.Columns.Add(FieldConstant.StockChartUrl, typeof(string));
            table.Columns.Add(HistoricalDataAdapter.CurrentPE, typeof(float));
        }
        protected override void OnInnerProcessorCompleted(EventArg<System.Data.DataTable> e)
        {
            RaiseProcessingUpdate(new EventArg<string>("Begin summary valuation..."));

            DataColumn currentTTMPE = null;
            List<DataColumn> columns = new List<DataColumn>();
            foreach(DataColumn col in e.Data.Columns)
            {
                int year = DateTime.Now.Year-1;
                if (col.ColumnName.Contains(HistoricalDataAdapter.CurrentPE)) 
                {
                    columns.Add(col);
                    if (col.ColumnName.Equals(string.Format(HistoricalDataAdapter.CurrentPE + "_{0}",DateTime.Now.Year)))
                    {
                        currentTTMPE = col;
                    }
                }
            }
            foreach (DataRow row in e.Data.Rows)
            {
                float ratio = (float)row[currentTTMPE];

                if (ratio > 1)
                {
                    //get ticker

                    string ticker = (string)row[FieldConstant.Ticker];

                    string company = (string)row[FieldConstant.Company];

                    string industry = (string)row[FieldConstant.Industry];

                    float price = float.Parse(row[FieldConstant.Price].ToString());

                    float peg = float.Parse(row[FieldConstant.PEG].ToString());

                    string roa = (string)row[FieldConstant.ROA];
                    string roe = (string)row[FieldConstant.ROE];
                    string instOWn = (string)row[FieldConstant.InstOwn];

                    string insiderTran = (String)row[FieldConstant.InsiderTran];

                    string earningDate = (string)row[FieldConstant.EarningDates];
                    string analystRec = (string)row[FieldConstant.AnalystRecom];
                    string _20Day = (string)row[FieldConstant._20Day];
                    string _50Day = (string)row[FieldConstant._50Day];
                    string _200Day = (string)row[FieldConstant._200Day];

                    _20Day = _20Day.TrimEnd('%');
                    _50Day = _50Day.TrimEnd('%');
                    _200Day = _200Day.TrimEnd('%');

                    string dividendPayDate = (string)row[FieldConstant.DividendPayDate];
                    string exDividendDate = (string)row[FieldConstant.ExDividendDate];
                    string exDividendYield = (string)row[FieldConstant.DividendYield];
                    float targetPrice = (float)row[FieldConstant.TargetPrice1y];

                    float targetPriceGap = targetPrice - price;

                    var newRow = table.Rows.Add();
                    newRow[FieldConstant.Ticker] = ticker;
                    newRow[FieldConstant.Industry] = industry;
                    newRow[FieldConstant.Company] = company;
                    newRow[FieldConstant.Price] = price;
                    newRow[FieldConstant.PEG] = peg;
                    newRow[FieldConstant.InsiderTran] = insiderTran;
                    newRow[FieldConstant.InstOwn] = instOWn;
                    newRow[FieldConstant.EarningDates] = earningDate;
                    newRow[FieldConstant._20Day] = float.Parse(_20Day);
                    newRow[FieldConstant._50Day] = float.Parse(_50Day);
                    newRow[FieldConstant._200Day] = float.Parse(_200Day);
                    newRow[FieldConstant.AnalystRecom] = analystRec;
                    newRow[FieldConstant.ROA] = roa;
                    newRow[FieldConstant.ROE] = roe;
                    newRow[HistoricalDataAdapter.CurrentPE] = ratio;
                    newRow[FieldConstant.DividendPayDate] = dividendPayDate;
                    newRow[FieldConstant.ExDividendDate] = exDividendDate;
                    newRow[FieldConstant.DividendYield] = exDividendYield;
                    newRow[FieldConstant.TargetPrice1y] = targetPrice;
                    newRow[FieldConstant.CurrentTargetPriceDiff] = targetPriceGap;
                    newRow[FieldConstant.StockChartUrl] = string.Format(charturlBase, ticker);
                }
            
            }
            RaiseProcessingUpdate(new EventArg<string>("Summary valuation...done"));
            this.RaiseProcessingCompleted(new EventArg<DataTable>(table));
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
            get { return table; }
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
