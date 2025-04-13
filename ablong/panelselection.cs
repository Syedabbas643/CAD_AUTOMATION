using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CAD_AUTOMATION
{
    public partial class panelselection : Form
    {
        public string BaseSize { get; private set; }
        public string paneltype { get; private set; }
        public string ViewPosition { get; private set; }
        public string cablealley { get; private set; }
        //public string Option4 { get; private set; } 

        public panelselection()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            BaseSize = basebox.SelectedItem?.ToString();
            ViewPosition = viewbox.SelectedItem?.ToString();
            cablealley = cablebox.SelectedItem?.ToString();
            paneltype = tibox.SelectedItem?.ToString();
            

            if (BaseSize == null || ViewPosition == null || cablealley == null)
            {
                errorlabel.Visible = true;
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            
        }
    }
}
