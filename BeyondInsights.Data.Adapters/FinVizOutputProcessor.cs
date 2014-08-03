using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using BeyondInsights.Configuration;


namespace BeyondInsights.Data.Adapters
{
    public class FinVizOutputProcessor : BaseProcessor
    {
  
        string exportUrl = string.Empty;
        string columnsMapping = string.Empty;
        string signalSelection = string.Empty;
        string filterString = string.Empty;
        List<string> exhcanges;
        Dictionary<string, int> columnIndexMapping = new Dictionary<string, int>();
        object lockObj = new object();
        DataTable result = new DataTable();

        public FinVizOutputProcessor():base(null)
        {
            exportUrl = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.ExportUrl].Value;
            columnsMapping = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.ColumnMapping].Value;
            string exchange = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.ExhcangeFilter].Value;
            exhcanges = new List<string>(exchange.Split(';'));
        }

        void SetupColumnIndex()
        {
            var columns = columnsMapping.Split(';');

            foreach (string c in columns)
            {
                string[] tempArr = c.Split('=');

                if (tempArr.Length ==2)
                    columnIndexMapping.Add(tempArr[0], Int32.Parse(tempArr[1])); 
            }
           
        }


        void adapter_DataRetrieveCompleted(object sender, EventArg<DataTable> e)
        {
            RaiseProcessingUpdate(new EventArg<string>("Retrieving FinViz data...done"));

            lock (lockObj)
            {
                //combine data together based on exchanges
                CsvDataAdapter adapter = sender as CsvDataAdapter;

                if (adapter == null)
                {
                    return;
                }

                string selectedExchange = string.Empty;
                foreach (var s in exhcanges)
                {
                    string[] values = s.Split('=');
                    if (adapter.CurrentURL.Contains(values[1]))
                    {
                        selectedExchange = s;
                        break;
                    }
                }

                e.Data.Columns.Add(FieldConstant.Exchange , typeof(string));

                foreach (DataRow r in e.Data.Rows)
                {
                    r[FieldConstant.Exchange] = selectedExchange;
                }

                this.result.Merge(e.Data, true, MissingSchemaAction.Add);

                DataColumn col = this.result.Columns[FieldConstant.Ticker];

                if (col != null)
                {
                    this.result.PrimaryKey = new DataColumn[]{col};
                }
                exhcanges.Remove(selectedExchange);

                if (exhcanges.Count == 0)
                    RaiseProcessingCompleted(new EventArg<DataTable>(this.result));
            }
            
        }

        public override void Prefilter()
        {
            SetupColumnIndex();
            string filterString = "&c=";
            //construct the URL
            foreach (var pair in this.columnIndexMapping)
            {
                filterString = filterString + pair.Value + ",";   
            }
            FilterString = filterString.TrimEnd(',');
        }

        public override void ProcessData()
        {
            RaiseProcessingUpdate(new EventArg<string>("Retrieving FinViz data..."));
            foreach (var exchange in exhcanges)
            {
                CsvDataAdapter adapter = new CsvDataAdapter(true);
                adapter.DataRetrieveCompleted += adapter_DataRetrieveCompleted;
                adapter.DataRetrieveError += adapter_DataRetrieveError;
                string[] values = exchange.Split('=');
                if (values.Length == 2)
                {
                    if (FilterString.Contains("&f="))
                        adapter.CurrentURL = exportUrl + FilterString + "," + values[1];
                    else
                        adapter.CurrentURL = exportUrl + FilterString + "&f=" + values[1];
                }
                else
                {
                    adapter.CurrentURL = exportUrl;
                }

                adapter.RetrieveData();
            }
        }

        void adapter_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            RaiseProcessingError(e);
        }

        public override DataTable OutputResult
        {
            get
            {
                return this.result;
            }
        }

        public override string FilterString
        {
            get
            {
                return filterString;
            }
            set
            {
                filterString = value;
            }
        }


        protected override void OnInnerProcessorCompleted(EventArg<DataTable> e)
        {
            
        }

        protected override void OnInnerProcessorError(EventArg<Exception> e)
        {
            
        }
    }
}
