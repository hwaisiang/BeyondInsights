using BeyondInsights.Configuration;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace BeyondInsights.Data.Adapters
{
    public class HistoricalDataAdapter
    {
        public const string TickerName = "TickerName";
        public const string Low10Year_PE = "Low10Y_PE";
        public const string High10Year_PE = "High10Y_PE";
        public const string Low5Year_PE = "Low5Y_PE";
        public const string High5Year_PE = "High5Y_PE";
        public const string CurrentPE = "BuyRating";


        public event EventHandler<EventArg<DataTable>> DataRetrieveCompleted;
        public event EventHandler<EventArg<Exception>> DataRetrieveError;

        string url = string.Empty;

        public HistoricalDataAdapter()
        {
            url = ConfigurationUtil.CurrentConfiguration.AppSettings.Settings[Constants.historicalPEUrl].Value;

            if (string.IsNullOrEmpty(url))
            {
                url = "http://financials.morningstar.com/valuation/valuation-history.action?&t=X{1}:{0}&region=usa&culture=en-US&cur=USD&type=price-earnings";
            }

        }
        
        public void RetrieveData(string ticker, Exchange exchange)
        {

            switch (exchange)
            {
                case Exchange.NASDAQ:
                 url = string.Format(url, ticker,"NAS");
                    break;
                case Exchange.NYSE:
                 url = string.Format(url, ticker,"NYS");
                    break;
                case Exchange.AMEX:
                 url = string.Format(url, ticker,"ASE");
                    break;
            }

        // Create a new HttpWebRequest object.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            
            // Set the Method property to 'POST' to post data to the URI.
            request.Method = "GET";

            DoWithResponse(request, (response) =>
            {

                try
                {
                    DataTable table = new DataTable();
                    
                    var col = table.Columns.Add();
                    col.ColumnName = TickerName;
                    col.DataType = typeof(string);
                    col = table.Columns.Add();
                    col.ColumnName = Low10Year_PE;
                    col.DataType = typeof(float);
                    col = table.Columns.Add();
                    col.ColumnName = High10Year_PE;
                    col.DataType = typeof(float);
                    col = table.Columns.Add();
                    col.ColumnName = Low5Year_PE;
                    col.DataType = typeof(float);
                    col = table.Columns.Add();
                    col.ColumnName = High5Year_PE;
                    col.DataType = typeof(float);
                    col = table.Columns.Add();
                    col.ColumnName = CurrentPE;
                    col.DataType = typeof(float);

                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        HtmlDocument doc = new HtmlDocument();
                        doc.Load(sr);

                        float[] tempArr = GetTickerPFHistory(doc, ticker);

                        IEnumerable<float> data =  tempArr.Reverse();

                        var first5Year = data.Take(5);

                        var low5PE = first5Year.Min();
                        var high5PE = first5Year.Max();

                        var lowPE = data.Min();
                        var highPE = data.Max();

                        var first = data.First();
                        try { table.Rows.Add(ticker, lowPE, highPE, low5PE, high5PE, first); }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        

                    }

                    if (DataRetrieveCompleted != null)
                        DataRetrieveCompleted(this, new EventArg<DataTable>(table));

                }
                catch (WebException e)
                {
                    RaiseError(e);
                }
                catch (XmlException e)
                {
                    RaiseError(e);
                }
                catch (Exception e)
                {
                    RaiseError(e);
                }
            });

        }

        private void RaiseError(Exception e)
        {
            if (this.DataRetrieveError != null)
                DataRetrieveError(this, new EventArg<Exception>(e));
        }

        private float GetCurrentPE(HtmlDocument doc)
        {
           return GetTickerFinancialValue(doc, "P/E Ratio (TTM)");
        }

        private float Get5YearLowPE(HtmlDocument doc)
        {
            return GetTickerFinancialValue(doc, "P/E Low - Last 5 Yrs.");
        }

        private float Get5YearHighPE(HtmlDocument doc)
        {
            return GetTickerFinancialValue(doc, "P/E High - Last 5 Yrs.");
        }

        private float GetTickerFinancialValue(HtmlDocument doc, string value)
        {
            var node = XPathUtil.GetParentNode(doc, "td", value);

            var nodes = node.SelectNodes("td[@class=\"data\"]");

            string currentPE = nodes.First().InnerText;
            float result = 0;
            if (float.TryParse(currentPE, out result))
            {
                return result;
            }
            return float.NaN;
        }

        private float[] GetTickerPFHistory(HtmlDocument doc, string ticker)
        {
            var node = XPathUtil.GetParentNodeByAttribute(doc, "th", "abbr", "Price/Earnings for " + ticker);

            var nodes = node.SelectNodes("td[@class=\"row_data\"]");

            var ttmNode = node.SelectSingleNode("td[@class=\"row_data_0\"]"); //current PE

            float[] values = new float[nodes.Count + 1];

            float ttmPE = 0;
            if (float.TryParse(ttmNode.InnerText, out ttmPE))
            {
                values[values.Length - 1] = ttmPE;
            }

            for (int i = 0; i < nodes.Count; i++)
            { 
                float result = 0;
                
                if (!float.TryParse(nodes[i].InnerText, out result))
                {
                    result = ttmPE; //use ttm value
                }
                values[i] = result;
            }

            return values;
        }

        void DoWithResponse(HttpWebRequest request, Action<HttpWebResponse> responseAction)
        {
            Action wrapperAction = () =>
            {
                try
                {
                    request.BeginGetResponse(new AsyncCallback((iar) =>
                    {
                        var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                        responseAction(response);
                    }), request);
                }
                catch (WebException e)
                {
                    RaiseError(e);
                }
                catch (Exception e)
                {
                    RaiseError(e);
                }
            };
            wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }), wrapperAction);
        }

    }

}
