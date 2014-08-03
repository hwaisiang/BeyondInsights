using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;
using BeyondInsights.Data.Adapters;
using System.Data;

namespace BeyondInsightsExcelAddIn
{
    public class Main
    {
        public event EventHandler RunCompleted;
        public event EventHandler RunError;
        public void Run()
        {            
            //run the simulation
            FinVizOutputProcessor processor = new FinVizOutputProcessor();
            FundamentalFilterProcessor fundamentalProcessor = new FundamentalFilterProcessor(processor);
            YahooFinanceProcessor financeProcessor = new YahooFinanceProcessor(fundamentalProcessor);
            ValuationProcessor valuationprocessor = new ValuationProcessor(financeProcessor);
            SummaryOutputProcessor summaryProcessor = new SummaryOutputProcessor(valuationprocessor);
            summaryProcessor.DataProcessingCompleted += valuationProcessor_DataProcessingCompleted;
            summaryProcessor.DataRetrieveError += valuationProcessor_DataRetrieveError;
            summaryProcessor.UpdateStatus += valuationprocessor_UpdateStatus;
            summaryProcessor.Prefilter();
            summaryProcessor.ProcessData();
        }

        private void valuationprocessor_UpdateStatus(object sender, EventArg<string> e)
        {
            UpdateStatus(e.Data);
        }

        private void valuationProcessor_DataRetrieveError(object sender, EventArg<Exception> e)
        {
            UpdateStatus(e.Data.Message);
            if (RunError != null)
                RunError(this, new EventArgs());
        }

        static void UpdateStatus(string status)
        {
           var oldStatusBar = Globals.ThisAddIn.Application.DisplayStatusBar; 
           Globals.ThisAddIn.Application.DisplayStatusBar = true ;
           Globals.ThisAddIn.Application.StatusBar = status; 
           //Globals.ThisAddIn.Application.StatusBar = false;
           //Globals.ThisAddIn.Application.DisplayStatusBar = oldStatusBar;         
        }

        private void valuationProcessor_DataProcessingCompleted(object sender, EventArg<DataTable> e)
        {
            var app = Globals.ThisAddIn.Application;
            app.ScreenUpdating = false;

            try
            { //gets the application instance
                //create a new sheet
                Excel.Worksheet ws = (Excel.Worksheet)app.Worksheets.Add();
                //Worksheet ws = (Worksheet)app.ActiveSheet;
                string sheetName = "result_" + DateTime.Now.ToString("yyyyMMddhhmmss");

                ws.Name = sheetName;
                //generate the excel sheet

                if (ws != null && e.Data !=null)
                {
                    int colCount = e.Data.Columns.Count;
                    int rowCount = e.Data.Rows.Count;
                    int currentRow = 1;
                    for (int i = 1; i <= colCount; i++)
                    {
                        ws.Cells[currentRow, i].Value2 = e.Data.Columns[i-1].ColumnName;
                        ws.Cells[currentRow, i].Font.Bold = true;
                    }
                    currentRow++;
                    foreach (DataRow r in e.Data.Rows)
                    {
                        for (int i = 1; i <= colCount; i++)
                        {
                            ws.Cells[currentRow, i].Value2 = r.ItemArray[i-1];
                        }

                        float _20day = (float)r[FieldConstant._20Day];
                        float _50day = (float)r[FieldConstant._50Day];
                        float _200day = (float)r[FieldConstant._200Day];

                        
                               //check for how close is the price from EMA
                            if (Math.Abs(_200day) < 1)
                            {
                                ws.Cells[currentRow, 1].Interior.ColorIndex=39;
                            }

                            if (Math.Abs(_50day) < 1)
                            {
                                ws.Cells[currentRow, 1].Interior.ColorIndex = 40;
                            }


                            string chartUrl = (string)r[FieldConstant.StockChartUrl];

                            UpdateStatus("Loading chart for " + r[FieldConstant.Ticker]);
                            //add chart comment
                            ws.Cells[1, currentRow].ClearComments();
                            ws.Hyperlinks.Add(ws.Cells[currentRow, 1], string.Format(Constants.TradingViewURL, r[FieldConstant.Ticker]));
                            Microsoft.Office.Interop.Excel.Comment comment = ws.Cells[currentRow,1].AddComment("");
                            comment.Shape.Fill.UserPicture(chartUrl);
                            comment.Shape.ScaleHeight(6, Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoScaleFrom.msoScaleFromTopLeft);
                            comment.Shape.ScaleWidth(6, Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoScaleFrom.msoScaleFromTopLeft);
                        currentRow++;
                    }

                    int indexCurrentTargetPriceDiff = e.Data.Columns.IndexOf(FieldConstant.CurrentTargetPriceDiff);
                    int indexCurrentPE = e.Data.Columns.IndexOf(HistoricalDataAdapter.CurrentPE);
                    string colCurrentTargetPriceDiff = ExcelColumnIndexToName(indexCurrentTargetPriceDiff);
                    string colCurrentPE = ExcelColumnIndexToName(indexCurrentPE);

                    // Create a two-color ColorScale object for the created sample data
                    // range.
                    ColorCodeColumn(ws, string.Format("{0}2:{0}{1}", colCurrentTargetPriceDiff, currentRow));
                    ColorCodeColumn(ws, string.Format("{0}2:{0}{1}", colCurrentPE, currentRow));

                }
            }
            catch(Exception err) 
            {
                UpdateStatus(err.Message);
            }
            finally 
            {
                app.ScreenUpdating = true;
                if (RunCompleted != null)
                    RunCompleted(this, new EventArgs());
            }
        }
  
        private void ColorCodeColumn(Excel.Worksheet ws, string range)
        {
            // Create a two-color ColorScale object for the created sample data
            // range.
            Excel.Top10 cfColorScale = (Excel.Top10)(ws.get_Range(range,
                Type.Missing).FormatConditions.AddTop10());

            cfColorScale.Interior.Color = 0x0000FF00;
            // Set the minimum threshold to red (0x000000FF) and maximum threshold
            // to green (0x0000FF00).
            //cfColorScale.ColorScaleCriteria[1].FormatColor.Color = 0x000000FF;
            //cfColorScale.ColorScaleCriteria[2].FormatColor.Color = 0x0000FF00;
        }

        private string ExcelColumnIndexToName(int Index)
        {
            string range = string.Empty;
            if (Index < 0) return range;
            int a = 26;
            int x = (int)Math.Floor(Math.Log((Index) * (a - 1) / a + 1, a));
            Index -= (int)(Math.Pow(a, x) - 1) * a / (a - 1);
            for (int i = x + 1; Index + i > 0; i--)
            {
                range = ((char)(65 + Index % a)).ToString() + range;
                Index /= a;
            }
            return range;
        }
    }
}
