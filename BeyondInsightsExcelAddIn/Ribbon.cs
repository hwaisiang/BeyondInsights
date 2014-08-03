using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Tools.Ribbon;
using System.Windows.Forms;

namespace BeyondInsightsExcelAddIn
{
    public partial class Ribbon
    {
        private void Ribbon_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void btnRun_Click(object sender, RibbonControlEventArgs e)
        {
            Main m = new Main();
            m.RunCompleted += m_RunCompleted;
            m.RunError += m_RunError;
            btnRun.Enabled = false;
            m.Run();
        }

        void m_RunError(object sender, EventArgs e)
        {
            btnRun.Enabled = true;
            MessageBox.Show("Run completed with error. Please check network connection.");            
        }

        void m_RunCompleted(object sender, EventArgs e)
        {
            btnRun.Enabled = true;
            MessageBox.Show("Run completed successfully.");
        }
    }
}
