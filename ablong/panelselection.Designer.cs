namespace CAD_AUTOMATION
{
    partial class panelselection
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
            this.metroLabel1 = new MetroFramework.Controls.MetroLabel();
            this.metroLabel2 = new MetroFramework.Controls.MetroLabel();
            this.metroLabel3 = new MetroFramework.Controls.MetroLabel();
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.basebox = new MetroFramework.Controls.MetroComboBox();
            this.viewbox = new MetroFramework.Controls.MetroComboBox();
            this.cablebox = new MetroFramework.Controls.MetroComboBox();
            this.metroLabel4 = new MetroFramework.Controls.MetroLabel();
            this.errorlabel = new MetroFramework.Controls.MetroLabel();
            this.tibox = new MetroFramework.Controls.MetroComboBox();
            this.metroLabel5 = new MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel1.CustomBackground = true;
            this.metroLabel1.CustomForeColor = true;
            this.metroLabel1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel1.Location = new System.Drawing.Point(85, 147);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(88, 19);
            this.metroLabel1.TabIndex = 0;
            this.metroLabel1.Text = "BASE HEIGHT";
            this.metroLabel1.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // metroLabel2
            // 
            this.metroLabel2.AutoSize = true;
            this.metroLabel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel2.CustomBackground = true;
            this.metroLabel2.CustomForeColor = true;
            this.metroLabel2.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel2.Location = new System.Drawing.Point(85, 203);
            this.metroLabel2.Name = "metroLabel2";
            this.metroLabel2.Size = new System.Drawing.Size(93, 19);
            this.metroLabel2.TabIndex = 1;
            this.metroLabel2.Text = "VIEW NEEDED";
            this.metroLabel2.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // metroLabel3
            // 
            this.metroLabel3.AutoSize = true;
            this.metroLabel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel3.CustomBackground = true;
            this.metroLabel3.CustomForeColor = true;
            this.metroLabel3.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel3.Location = new System.Drawing.Point(85, 261);
            this.metroLabel3.Name = "metroLabel3";
            this.metroLabel3.Size = new System.Drawing.Size(88, 19);
            this.metroLabel3.TabIndex = 2;
            this.metroLabel3.Text = "CABLE ALLEY";
            this.metroLabel3.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // metroButton1
            // 
            this.metroButton1.Location = new System.Drawing.Point(170, 325);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(115, 23);
            this.metroButton1.TabIndex = 3;
            this.metroButton1.Text = "RUN";
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // basebox
            // 
            this.basebox.FormattingEnabled = true;
            this.basebox.ItemHeight = 23;
            this.basebox.Items.AddRange(new object[] {
            "NOBASE",
            "CRCA50",
            "ISMC75",
            "ISMC100"});
            this.basebox.Location = new System.Drawing.Point(239, 141);
            this.basebox.Name = "basebox";
            this.basebox.Size = new System.Drawing.Size(121, 29);
            this.basebox.Style = MetroFramework.MetroColorStyle.Green;
            this.basebox.TabIndex = 4;
            this.basebox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // viewbox
            // 
            this.viewbox.FormattingEnabled = true;
            this.viewbox.ItemHeight = 23;
            this.viewbox.Items.AddRange(new object[] {
            "BOTTOMVIEW",
            "TOPVIEW",
            "NOVIEW"});
            this.viewbox.Location = new System.Drawing.Point(239, 197);
            this.viewbox.Name = "viewbox";
            this.viewbox.Size = new System.Drawing.Size(121, 29);
            this.viewbox.TabIndex = 5;
            this.viewbox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // cablebox
            // 
            this.cablebox.FormattingEnabled = true;
            this.cablebox.ItemHeight = 23;
            this.cablebox.Items.AddRange(new object[] {
            "FRONT CABLING",
            "REAR CABLING"});
            this.cablebox.Location = new System.Drawing.Point(239, 254);
            this.cablebox.Name = "cablebox";
            this.cablebox.Size = new System.Drawing.Size(121, 29);
            this.cablebox.TabIndex = 6;
            this.cablebox.Theme = MetroFramework.MetroThemeStyle.Light;
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
            this.metroLabel4.Location = new System.Drawing.Point(118, 24);
            this.metroLabel4.Name = "metroLabel4";
            this.metroLabel4.Size = new System.Drawing.Size(210, 25);
            this.metroLabel4.TabIndex = 7;
            this.metroLabel4.Text = "SELECT OPTIONS BELOW";
            this.metroLabel4.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // errorlabel
            // 
            this.errorlabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.errorlabel.CustomBackground = true;
            this.errorlabel.CustomForeColor = true;
            this.errorlabel.ForeColor = System.Drawing.Color.Red;
            this.errorlabel.Location = new System.Drawing.Point(134, 351);
            this.errorlabel.Name = "errorlabel";
            this.errorlabel.Size = new System.Drawing.Size(183, 19);
            this.errorlabel.TabIndex = 8;
            this.errorlabel.Text = "please select all fields";
            this.errorlabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.errorlabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.errorlabel.Visible = false;
            // 
            // tibox
            // 
            this.tibox.FormattingEnabled = true;
            this.tibox.ItemHeight = 23;
            this.tibox.Items.AddRange(new object[] {
            "TI",
            "NON-TI"});
            this.tibox.Location = new System.Drawing.Point(239, 89);
            this.tibox.Name = "tibox";
            this.tibox.Size = new System.Drawing.Size(121, 29);
            this.tibox.Style = MetroFramework.MetroColorStyle.Green;
            this.tibox.TabIndex = 10;
            this.tibox.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // metroLabel5
            // 
            this.metroLabel5.AutoSize = true;
            this.metroLabel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.metroLabel5.CustomBackground = true;
            this.metroLabel5.CustomForeColor = true;
            this.metroLabel5.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.metroLabel5.Location = new System.Drawing.Point(85, 95);
            this.metroLabel5.Name = "metroLabel5";
            this.metroLabel5.Size = new System.Drawing.Size(111, 19);
            this.metroLabel5.TabIndex = 9;
            this.metroLabel5.Text = "TI - OR - NON TI";
            this.metroLabel5.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // panelselection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(454, 394);
            this.Controls.Add(this.tibox);
            this.Controls.Add(this.metroLabel5);
            this.Controls.Add(this.errorlabel);
            this.Controls.Add(this.metroLabel4);
            this.Controls.Add(this.cablebox);
            this.Controls.Add(this.viewbox);
            this.Controls.Add(this.basebox);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.metroLabel3);
            this.Controls.Add(this.metroLabel2);
            this.Controls.Add(this.metroLabel1);
            this.Name = "panelselection";
            this.Text = "panelselection";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MetroFramework.Controls.MetroLabel metroLabel1;
        private MetroFramework.Controls.MetroLabel metroLabel2;
        private MetroFramework.Controls.MetroLabel metroLabel3;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroComboBox basebox;
        private MetroFramework.Controls.MetroComboBox viewbox;
        private MetroFramework.Controls.MetroComboBox cablebox;
        private MetroFramework.Controls.MetroLabel metroLabel4;
        private MetroFramework.Controls.MetroLabel errorlabel;
        private MetroFramework.Controls.MetroComboBox tibox;
        private MetroFramework.Controls.MetroLabel metroLabel5;
    }
}