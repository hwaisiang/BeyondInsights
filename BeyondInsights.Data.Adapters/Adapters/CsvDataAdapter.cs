using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace BeyondInsights.Data.Adapters
{
    public class EventArg<T> : EventArgs
    {
        // Property variable
        private readonly T p_EventData;

        // Constructor
        public EventArg(T data)
        {
            p_EventData = data;
        }

        // Property for EventArgs argument
        public T Data
        {
            get { return p_EventData; }
        }
    }

    public class CsvDataAdapter
    {
        public event EventHandler<EventArg<DataTable>> DataRetrieveCompleted;
        public event EventHandler<EventArg<Exception>> DataRetrieveError;
        string currentURL = string.Empty;
        private bool hasHeader = true;
        public CsvDataAdapter(bool hasHeader)
        {
            this.hasHeader = hasHeader;
        }


        public string CurrentURL
        {
            get { return currentURL; }
            set { currentURL = value; }
        }

        public void RetrieveData()
        {
            string url = currentURL;

            if (url.Length == 0) return;

            // Create a new HttpWebRequest object.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            
            // Set the Method property to 'POST' to post data to the URI.
            request.Method = "GET";

            DoWithResponse(request, (response) =>
            {

                try 
                {
                    using (CsvReader csv = new CsvReader(new StreamReader(response.GetResponseStream()), hasHeader))
                    {
                        var body = new StreamReader(response.GetResponseStream());
                        DataTable table = new DataTable();
                        table.Load(csv);
                        if (DataRetrieveCompleted != null)
                            DataRetrieveCompleted(this, new EventArg<DataTable>(table));
                    }
                  
                }
                catch (WebException e)
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
