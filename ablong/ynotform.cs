using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp;

namespace CAD_AUTOMATION
{
    public partial class ynotform : Form
    {
        public string panelselection { get; private set; }
        public bool lineweight { get; private set; }
        public bool mergebom { get; private set; }
        public string a4scale { get; private set; }
        public string ltscale { get; private set; }
        public ynotform()
        {
            InitializeComponent();
        }

        private void ynotform_Load(object sender, EventArgs e)
        {
            panelbox.SelectedIndex = 0;
            lineweightbox.SelectedIndex = 0;
            mergebox.SelectedIndex = 0;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            panelselection = panelbox.SelectedItem?.ToString();
            lineweight = lineweightbox.SelectedItem?.ToString() == "YES";
            mergebom = mergebox.SelectedItem?.ToString() == "YES";
            a4scale = a4scalebox.Text;  // FIXED for TextBox
            ltscale = ltscalebox.Text;  // FIXED for TextBox


            if (panelselection == null || a4scale == null || ltscale == null)
            {
                errorlabel.Visible = true;
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void a4scalebox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits (0-9), backspace, and dot (.) but only one dot
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // Prevent multiple dots
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.Contains(".")))
            {
                e.Handled = true;
            }
        }

        private void ltscalebox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Same logic for ltscale
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.Contains(".")))
            {
                e.Handled = true;
            }
        }
    }
}
