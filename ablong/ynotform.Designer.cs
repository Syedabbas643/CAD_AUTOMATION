namespace CAD_AUTOMATION
{
    partial class ynotform
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.errorlabel = new MetroFramework.Controls.MetroLabel();
            this.metroLabel4 = new MetroFramework.Controls.MetroLabel();
            this.lineweightbox = new MetroFramework.Controls.MetroComboBox();
            this.panelbox = new MetroFramework.Controls.MetroComboBox();
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.metroLabel3 = new MetroFramework.Controls.MetroLabel();
            this.metroLabel2 = new MetroFramework.Controls.MetroLabel();
            this.metroLabel1 = new MetroFramework.Controls.MetroLabel();
            this.a4scalebox = new MetroFramework.Controls.MetroTextBox();
            this.ltscalebox = new MetroFramework.Controls.MetroTextBox();
            this.metroLabel5 = new MetroFramework.Controls.MetroLabel();
            this.mergebox = new MetroFramework.Controls.MetroComboBox();
            this.metroLabel6 = new MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // errorlabel
            // 
            this.errorlabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.errorlabel.CustomBackground = true;
            this.errorlabel.CustomForeColor = true;
            this.errorlabel.ForeColor = System.Drawing.Color.Red;
            this.errorlabel.Location = new System.Drawing.Point(115, 416);
            this.errorlabel.Name = "errorlabel";
            this.errorlabel.Size = new System.Drawing.Size(183, 19);
            this.errorlabel.TabIndex = 17;
            this.errorlabel.Text = "please select all fields";
            this.errorlabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.errorlabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.errorlabel.Visible = false;
            // 
            // metroLabel4
            // 
            this.metroLabel4.AutoSize = true;
            this.metroLabel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel4.CustomBackground = true;
            this.metroLabel4.CustomForeColor = true;
            this.metroLabel4.FontSize = MetroFramework.MetroLabelSize.Tall;
            this.metroLabel4.FontWeight = MetroFramework.MetroLabelWeight.Regular;
            this.metroLabel4.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel4.Location = new System.Drawing.Point(111, 38);
            this.metroLabel4.Name = "metroLabel4";
            this.metroLabel4.Size = new System.Drawing.Size(210, 25);
            this.metroLabel4.TabIndex = 16;
            this.metroLabel4.Text = "SELECT OPTIONS BELOW";
            this.metroLabel4.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // lineweightbox
            // 
            this.lineweightbox.FormattingEnabled = true;
            this.lineweightbox.ItemHeight = 23;
            this.lineweightbox.Items.AddRange(new object[] {
            "YES",
            "NO"});
            this.lineweightbox.Location = new System.Drawing.Point(233, 148);
            this.lineweightbox.Name = "lineweightbox";
            this.lineweightbox.Size = new System.Drawing.Size(121, 29);
            this.lineweightbox.TabIndex = 14;
            this.lineweightbox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // panelbox
            // 
            this.panelbox.FormattingEnabled = true;
            this.panelbox.ItemHeight = 23;
            this.panelbox.Items.AddRange(new object[] {
            "SINGLE_PANEL",
            "MULTIPLE_PANEL"});
            this.panelbox.Location = new System.Drawing.Point(233, 92);
            this.panelbox.Name = "panelbox";
            this.panelbox.Size = new System.Drawing.Size(121, 29);
            this.panelbox.Style = MetroFramework.MetroColorStyle.Green;
            this.panelbox.TabIndex = 13;
            this.panelbox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // metroButton1
            // 
            this.metroButton1.Location = new System.Drawing.Point(151, 390);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(115, 23);
            this.metroButton1.TabIndex = 12;
            this.metroButton1.Text = "GENERATE PDF";
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // metroLabel3
            // 
            this.metroLabel3.AutoSize = true;
            this.metroLabel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel3.CustomBackground = true;
            this.metroLabel3.CustomForeColor = true;
            this.metroLabel3.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel3.Location = new System.Drawing.Point(79, 212);
            this.metroLabel3.Name = "metroLabel3";
            this.metroLabel3.Size = new System.Drawing.Size(80, 19);
            this.metroLabel3.TabIndex = 11;
            this.metroLabel3.Text = "A4 MARGIN";
            this.metroLabel3.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // metroLabel2
            // 
            this.metroLabel2.AutoSize = true;
            this.metroLabel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel2.CustomBackground = true;
            this.metroLabel2.CustomForeColor = true;
            this.metroLabel2.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel2.Location = new System.Drawing.Point(79, 154);
            this.metroLabel2.Name = "metroLabel2";
            this.metroLabel2.Size = new System.Drawing.Size(117, 19);
            this.metroLabel2.TabIndex = 10;
            this.metroLabel2.Text = "PLOT LINEWEIGHT";
            this.metroLabel2.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel1.CustomBackground = true;
            this.metroLabel1.CustomForeColor = true;
            this.metroLabel1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel1.Location = new System.Drawing.Point(79, 98);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(118, 19);
            this.metroLabel1.TabIndex = 9;
            this.metroLabel1.Text = "PANEL SELECTION";
            this.metroLabel1.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // a4scalebox
            // 
            this.a4scalebox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.a4scalebox.FontSize = MetroFramework.MetroTextBoxSize.Medium;
            this.a4scalebox.Location = new System.Drawing.Point(233, 208);
            this.a4scalebox.Name = "a4scalebox";
            this.a4scalebox.Size = new System.Drawing.Size(121, 23);
            this.a4scalebox.Style = MetroFramework.MetroColorStyle.Black;
            this.a4scalebox.TabIndex = 18;
            this.a4scalebox.Text = "0.8";
            this.a4scalebox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.a4scalebox.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.a4scalebox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.a4scalebox_KeyPress);
            // 
            // ltscalebox
            // 
            this.ltscalebox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ltscalebox.FontSize = MetroFramework.MetroTextBoxSize.Medium;
            this.ltscalebox.Location = new System.Drawing.Point(233, 253);
            this.ltscalebox.Name = "ltscalebox";
            this.ltscalebox.Size = new System.Drawing.Size(121, 23);
            this.ltscalebox.Style = MetroFramework.MetroColorStyle.Black;
            this.ltscalebox.TabIndex = 20;
            this.ltscalebox.Text = "0.03";
            this.ltscalebox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ltscalebox.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.ltscalebox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ltscalebox_KeyPress);
            // 
            // metroLabel5
            // 
            this.metroLabel5.AutoSize = true;
            this.metroLabel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel5.CustomBackground = true;
            this.metroLabel5.CustomForeColor = true;
            this.metroLabel5.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel5.Location = new System.Drawing.Point(79, 257);
            this.metroLabel5.Name = "metroLabel5";
            this.metroLabel5.Size = new System.Drawing.Size(63, 19);
            this.metroLabel5.TabIndex = 19;
            this.metroLabel5.Text = "LT SCALE";
            this.metroLabel5.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // mergebox
            // 
            this.mergebox.FormattingEnabled = true;
            this.mergebox.ItemHeight = 23;
            this.mergebox.Items.AddRange(new object[] {
            "YES",
            "NO"});
            this.mergebox.Location = new System.Drawing.Point(233, 305);
            this.mergebox.Name = "mergebox";
            this.mergebox.Size = new System.Drawing.Size(121, 29);
            this.mergebox.TabIndex = 22;
            this.mergebox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // metroLabel6
            // 
            this.metroLabel6.AutoSize = true;
            this.metroLabel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel6.CustomBackground = true;
            this.metroLabel6.CustomForeColor = true;
            this.metroLabel6.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel6.Location = new System.Drawing.Point(79, 311);
            this.metroLabel6.Name = "metroLabel6";
            this.metroLabel6.Size = new System.Drawing.Size(87, 19);
            this.metroLabel6.TabIndex = 21;
            this.metroLabel6.Text = "MERGE BOM";
            this.metroLabel6.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // ynotform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(424, 450);
            this.Controls.Add(this.mergebox);
            this.Controls.Add(this.metroLabel6);
            this.Controls.Add(this.ltscalebox);
            this.Controls.Add(this.metroLabel5);
            this.Controls.Add(this.a4scalebox);
            this.Controls.Add(this.errorlabel);
            this.Controls.Add(this.metroLabel4);
            this.Controls.Add(this.lineweightbox);
            this.Controls.Add(this.panelbox);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.metroLabel3);
            this.Controls.Add(this.metroLabel2);
            this.Controls.Add(this.metroLabel1);
            this.Name = "ynotform";
            this.Text = "ynotform";
            this.Load += new System.EventHandler(this.ynotform_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MetroFramework.Controls.MetroLabel errorlabel;
        private MetroFramework.Controls.MetroLabel metroLabel4;
        private MetroFramework.Controls.MetroComboBox lineweightbox;
        private MetroFramework.Controls.MetroComboBox panelbox;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroLabel metroLabel3;
        private MetroFramework.Controls.MetroLabel metroLabel2;
        private MetroFramework.Controls.MetroLabel metroLabel1;
        private MetroFramework.Controls.MetroTextBox a4scalebox;
        private MetroFramework.Controls.MetroTextBox ltscalebox;
        private MetroFramework.Controls.MetroLabel metroLabel5;
        private MetroFramework.Controls.MetroComboBox mergebox;
        private MetroFramework.Controls.MetroLabel metroLabel6;
    }
}