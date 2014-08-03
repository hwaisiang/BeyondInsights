using BeyondInsights.Configuration;
using BeyondInsights.Data.Adapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdapterTest
{
    class Program
    {
        static ManualResetEvent exitFlag;
        static CsvDataAdapter adapter;

        static void Main(string[] args)
        {


            FinVizOutputProcessor processor = new FinVizOutputProcessor();
            FundamentalFilterProcessor fundamentalProcessor = new FundamentalFilterProcessor(processor);
            ValuationProcessor valuationprocessor = new ValuationProcessor(fundamentalProcessor);
            SummaryOutputProcessor summaryProcessor = new SummaryOutputProcessor(valuationprocessor);
            summaryProcessor.DataProcessingCompleted += valuationProcessor_DataProcessingCompleted;
            summaryProcessor.DataRetrieveError += valuationProcessor_DataRetrieveError;
            summaryProcessor.UpdateStatus += valuationprocessor_UpdateStatus;
            summaryProcessor.Prefilter();
            summaryProcessor.ProcessData();

            exitFlag = new ManualResetEvent(false);
               WaitHandle.WaitAll(new WaitHandle[] { exitFlag });

            Console.WriteLine("Done. Press any key to close.");
            Console.ReadLine();
        }

        static void valuationprocessor_UpdateStatus(object sender, EventArg<string> e)
        {
            Console.WriteLine(e.Data);
        }

        static void valuationProcessor_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            Console.WriteLine("Error\n\r" + e.Data);
            exitFlag.Set();
        }

        static void valuationProcessor_DataProcessingCompleted(object sender, EventArg<DataTable> e)
        {
            Console.WriteLine("Success\n\r" + e.Data);
            
            string fileName = "result_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
            string folder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BeyondInsights");

            if (!Directory.Exists(folder))
                Directory .CreateDirectory(folder);

            string path = Path.Combine(folder,fileName);

            PutDataTableToCsv(path, e.Data, true);
            exitFlag.Set(); 
        }

        static void PutDataTableToCsv(string path, DataTable table, bool isFirstRowHeader)
        {
            var lines = new List<string>(); // create a list of strings to hold the file rows

            // if there are headers add them to the file first
            if (isFirstRowHeader)
            {
                string[] colnames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                var header = string.Join(",", colnames);
                lines.Add(header);
            }

            // Place commas between the elements of each row
            var valueLines = table.AsEnumerable().Cast<DataRow>().Select(row => string.Join(",", row.ItemArray.Select(o => (o.ToString()).Replace(',','_')).ToArray()));

            // Stuff the rows into a string joined by new line characters
            var allLines = string.Join(Environment.NewLine, valueLines.ToArray<string>());
            lines.Add(allLines);

            // put that file to bed
            File.WriteAllLines(path, lines.ToArray());

            Process.Start(path);
        }
    }
}
