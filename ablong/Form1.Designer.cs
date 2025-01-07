namespace CAD_AUTOMATION
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.materialTabControl1 = new MaterialSkin.Controls.MaterialTabControl();
            this.shellpage = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.sectionsbox = new MaterialSkin.Controls.MaterialComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.hbbpositionbox = new MaterialSkin.Controls.MaterialComboBox();
            this.depthbox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.shellthickbox = new System.Windows.Forms.TextBox();
            this.heigthbox = new System.Windows.Forms.TextBox();
            this.widthbox = new System.Windows.Forms.TextBox();
            this.drawbutton = new MaterialSkin.Controls.MaterialButton();
            this.materialTabSelector1 = new MaterialSkin.Controls.MaterialTabSelector();
            this.nextbutton = new MaterialSkin.Controls.MaterialButton();
            this.backbutton = new MaterialSkin.Controls.MaterialButton();
            this.materialTabControl1.SuspendLayout();
            this.shellpage.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.Transparent;
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Red;
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Red;
            this.button1.Location = new System.Drawing.Point(862, -5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 36);
            this.button1.TabIndex = 0;
            this.button1.TabStop = false;
            this.button1.Text = "X";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.Transparent;
            this.button2.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LimeGreen;
            this.button2.FlatAppearance.MouseOverBackColor = System.Drawing.Color.LimeGreen;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("Microsoft New Tai Lue", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.SystemColors.Control;
            this.button2.Location = new System.Drawing.Point(811, -4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(51, 36);
            this.button2.TabIndex = 1;
            this.button2.TabStop = false;
            this.button2.Text = "-";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("BankGothic Md BT", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.Control;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(266, 19);
            this.label1.TabIndex = 2;
            this.label1.Text = "PANEL Automation By";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MV Boli", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Lime;
            this.label2.Location = new System.Drawing.Point(279, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 28);
            this.label2.TabIndex = 3;
            this.label2.Text = "GaMeR";
            // 
            // materialTabControl1
            // 
            this.materialTabControl1.Controls.Add(this.shellpage);
            this.materialTabControl1.Depth = 0;
            this.materialTabControl1.Location = new System.Drawing.Point(35, 103);
            this.materialTabControl1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialTabControl1.Multiline = true;
            this.materialTabControl1.Name = "materialTabControl1";
            this.materialTabControl1.SelectedIndex = 0;
            this.materialTabControl1.Size = new System.Drawing.Size(833, 342);
            this.materialTabControl1.TabIndex = 4;
            // 
            // shellpage
            // 
            this.shellpage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.shellpage.Controls.Add(this.label8);
            this.shellpage.Controls.Add(this.sectionsbox);
            this.shellpage.Controls.Add(this.label7);
            this.shellpage.Controls.Add(this.hbbpositionbox);
            this.shellpage.Controls.Add(this.depthbox);
            this.shellpage.Controls.Add(this.label6);
            this.shellpage.Controls.Add(this.label5);
            this.shellpage.Controls.Add(this.label4);
            this.shellpage.Controls.Add(this.label3);
            this.shellpage.Controls.Add(this.shellthickbox);
            this.shellpage.Controls.Add(this.heigthbox);
            this.shellpage.Controls.Add(this.widthbox);
            this.shellpage.Location = new System.Drawing.Point(4, 22);
            this.shellpage.Name = "shellpage";
            this.shellpage.Padding = new System.Windows.Forms.Padding(3);
            this.shellpage.Size = new System.Drawing.Size(825, 316);
            this.shellpage.TabIndex = 0;
            this.shellpage.Text = "SHELL";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.SystemColors.Control;
            this.label8.Location = new System.Drawing.Point(447, 93);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(66, 18);
            this.label8.TabIndex = 14;
            this.label8.Text = "Sections";
            // 
            // sectionsbox
            // 
            this.sectionsbox.AutoResize = false;
            this.sectionsbox.BackColor = System.Drawing.Color.Black;
            this.sectionsbox.Depth = 0;
            this.sectionsbox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.sectionsbox.DropDownHeight = 147;
            this.sectionsbox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sectionsbox.DropDownWidth = 100;
            this.sectionsbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.sectionsbox.ForeColor = System.Drawing.Color.White;
            this.sectionsbox.FormattingEnabled = true;
            this.sectionsbox.IntegralHeight = false;
            this.sectionsbox.ItemHeight = 29;
            this.sectionsbox.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8"});
            this.sectionsbox.Location = new System.Drawing.Point(581, 84);
            this.sectionsbox.MaxDropDownItems = 5;
            this.sectionsbox.MouseState = MaterialSkin.MouseState.OUT;
            this.sectionsbox.Name = "sectionsbox";
            this.sectionsbox.Size = new System.Drawing.Size(149, 35);
            this.sectionsbox.StartIndex = 0;
            this.sectionsbox.TabIndex = 13;
            this.sectionsbox.UseTallSize = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.Control;
            this.label7.Location = new System.Drawing.Point(447, 35);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(97, 18);
            this.label7.TabIndex = 12;
            this.label7.Text = "HBB Position";
            // 
            // hbbpositionbox
            // 
            this.hbbpositionbox.AutoResize = false;
            this.hbbpositionbox.BackColor = System.Drawing.Color.Gray;
            this.hbbpositionbox.Depth = 0;
            this.hbbpositionbox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.hbbpositionbox.DropDownHeight = 89;
            this.hbbpositionbox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.hbbpositionbox.DropDownWidth = 100;
            this.hbbpositionbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.hbbpositionbox.ForeColor = System.Drawing.Color.White;
            this.hbbpositionbox.FormattingEnabled = true;
            this.hbbpositionbox.IntegralHeight = false;
            this.hbbpositionbox.ItemHeight = 29;
            this.hbbpositionbox.Items.AddRange(new object[] {
            "None",
            "Top",
            "Bottom"});
            this.hbbpositionbox.Location = new System.Drawing.Point(581, 24);
            this.hbbpositionbox.MaxDropDownItems = 3;
            this.hbbpositionbox.MouseState = MaterialSkin.MouseState.OUT;
            this.hbbpositionbox.Name = "hbbpositionbox";
            this.hbbpositionbox.Size = new System.Drawing.Size(149, 35);
            this.hbbpositionbox.StartIndex = 0;
            this.hbbpositionbox.TabIndex = 11;
            this.hbbpositionbox.UseAccent = false;
            this.hbbpositionbox.UseTallSize = false;
            // 
            // depthbox
            // 
            this.depthbox.BackColor = System.Drawing.Color.DimGray;
            this.depthbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.depthbox.Cursor = System.Windows.Forms.Cursors.Default;
            this.depthbox.Font = new System.Drawing.Font("Microsoft Tai Le", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.depthbox.ForeColor = System.Drawing.Color.White;
            this.depthbox.Location = new System.Drawing.Point(183, 126);
            this.depthbox.Margin = new System.Windows.Forms.Padding(5);
            this.depthbox.MinimumSize = new System.Drawing.Size(150, 20);
            this.depthbox.Name = "depthbox";
            this.depthbox.Size = new System.Drawing.Size(150, 21);
            this.depthbox.TabIndex = 10;
            this.depthbox.Text = "450";
            this.depthbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.Control;
            this.label6.Location = new System.Drawing.Point(54, 126);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 18);
            this.label6.TabIndex = 9;
            this.label6.Text = "Depth";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.SystemColors.Control;
            this.label5.Location = new System.Drawing.Point(54, 169);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 18);
            this.label5.TabIndex = 8;
            this.label5.Text = "Shell Thick";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.Control;
            this.label4.Location = new System.Drawing.Point(54, 80);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 18);
            this.label4.TabIndex = 7;
            this.label4.Text = "Heigth";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.Control;
            this.label3.Location = new System.Drawing.Point(54, 37);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 18);
            this.label3.TabIndex = 6;
            this.label3.Text = "Width / Length";
            // 
            // shellthickbox
            // 
            this.shellthickbox.BackColor = System.Drawing.Color.DimGray;
            this.shellthickbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.shellthickbox.Cursor = System.Windows.Forms.Cursors.Default;
            this.shellthickbox.Font = new System.Drawing.Font("Microsoft Tai Le", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.shellthickbox.ForeColor = System.Drawing.Color.White;
            this.shellthickbox.Location = new System.Drawing.Point(183, 167);
            this.shellthickbox.Margin = new System.Windows.Forms.Padding(5);
            this.shellthickbox.MinimumSize = new System.Drawing.Size(150, 20);
            this.shellthickbox.Name = "shellthickbox";
            this.shellthickbox.Size = new System.Drawing.Size(150, 21);
            this.shellthickbox.TabIndex = 5;
            this.shellthickbox.Text = "2";
            this.shellthickbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // heigthbox
            // 
            this.heigthbox.BackColor = System.Drawing.Color.DimGray;
            this.heigthbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.heigthbox.Cursor = System.Windows.Forms.Cursors.Default;
            this.heigthbox.Font = new System.Drawing.Font("Microsoft Tai Le", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.heigthbox.ForeColor = System.Drawing.Color.White;
            this.heigthbox.Location = new System.Drawing.Point(183, 78);
            this.heigthbox.Margin = new System.Windows.Forms.Padding(5);
            this.heigthbox.MinimumSize = new System.Drawing.Size(150, 20);
            this.heigthbox.Name = "heigthbox";
            this.heigthbox.Size = new System.Drawing.Size(150, 21);
            this.heigthbox.TabIndex = 4;
            this.heigthbox.Text = "2500";
            this.heigthbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // widthbox
            // 
            this.widthbox.BackColor = System.Drawing.Color.DimGray;
            this.widthbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.widthbox.Cursor = System.Windows.Forms.Cursors.Default;
            this.widthbox.Font = new System.Drawing.Font("Microsoft Tai Le", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.widthbox.ForeColor = System.Drawing.Color.White;
            this.widthbox.Location = new System.Drawing.Point(183, 35);
            this.widthbox.Margin = new System.Windows.Forms.Padding(5);
            this.widthbox.MinimumSize = new System.Drawing.Size(150, 20);
            this.widthbox.Name = "widthbox";
            this.widthbox.Size = new System.Drawing.Size(150, 21);
            this.widthbox.TabIndex = 3;
            this.widthbox.Text = "1500";
            this.widthbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // drawbutton
            // 
            this.drawbutton.AutoSize = false;
            this.drawbutton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.drawbutton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.drawbutton.Depth = 0;
            this.drawbutton.Enabled = false;
            this.drawbutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.drawbutton.HighEmphasis = true;
            this.drawbutton.Icon = null;
            this.drawbutton.Location = new System.Drawing.Point(397, 464);
            this.drawbutton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.drawbutton.MouseState = MaterialSkin.MouseState.HOVER;
            this.drawbutton.Name = "drawbutton";
            this.drawbutton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.drawbutton.Size = new System.Drawing.Size(116, 43);
            this.drawbutton.TabIndex = 5;
            this.drawbutton.Text = "DRAW";
            this.drawbutton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.drawbutton.UseAccentColor = false;
            this.drawbutton.UseVisualStyleBackColor = true;
            this.drawbutton.Click += new System.EventHandler(this.materialButton1_Click);
            // 
            // materialTabSelector1
            // 
            this.materialTabSelector1.BackColor = System.Drawing.Color.White;
            this.materialTabSelector1.BaseTabControl = this.materialTabControl1;
            this.materialTabSelector1.CharacterCasing = MaterialSkin.Controls.MaterialTabSelector.CustomCharacterCasing.Normal;
            this.materialTabSelector1.Depth = 0;
            this.materialTabSelector1.Enabled = false;
            this.materialTabSelector1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialTabSelector1.Location = new System.Drawing.Point(35, 55);
            this.materialTabSelector1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialTabSelector1.Name = "materialTabSelector1";
            this.materialTabSelector1.Size = new System.Drawing.Size(829, 32);
            this.materialTabSelector1.TabIndex = 6;
            this.materialTabSelector1.TabIndicatorHeight = 1;
            this.materialTabSelector1.Text = "materialTabSelector1";
            // 
            // nextbutton
            // 
            this.nextbutton.AutoSize = false;
            this.nextbutton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.nextbutton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.nextbutton.Depth = 0;
            this.nextbutton.HighEmphasis = true;
            this.nextbutton.Icon = null;
            this.nextbutton.Location = new System.Drawing.Point(536, 467);
            this.nextbutton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.nextbutton.MouseState = MaterialSkin.MouseState.HOVER;
            this.nextbutton.Name = "nextbutton";
            this.nextbutton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.nextbutton.Size = new System.Drawing.Size(110, 36);
            this.nextbutton.TabIndex = 7;
            this.nextbutton.Text = "NEXT";
            this.nextbutton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.nextbutton.UseAccentColor = false;
            this.nextbutton.UseVisualStyleBackColor = true;
            this.nextbutton.Click += new System.EventHandler(this.materialButton2_Click);
            // 
            // backbutton
            // 
            this.backbutton.AutoSize = false;
            this.backbutton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.backbutton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.backbutton.Depth = 0;
            this.backbutton.HighEmphasis = true;
            this.backbutton.Icon = null;
            this.backbutton.Location = new System.Drawing.Point(262, 467);
            this.backbutton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.backbutton.MouseState = MaterialSkin.MouseState.HOVER;
            this.backbutton.Name = "backbutton";
            this.backbutton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.backbutton.Size = new System.Drawing.Size(110, 36);
            this.backbutton.TabIndex = 8;
            this.backbutton.Text = "BACK";
            this.backbutton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.backbutton.UseAccentColor = false;
            this.backbutton.UseVisualStyleBackColor = true;
            this.backbutton.Click += new System.EventHandler(this.backbutton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(910, 536);
            this.Controls.Add(this.backbutton);
            this.Controls.Add(this.nextbutton);
            this.Controls.Add(this.materialTabSelector1);
            this.Controls.Add(this.drawbutton);
            this.Controls.Add(this.materialTabControl1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.Padding = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.Text = "Form1";
            this.materialTabControl1.ResumeLayout(false);
            this.shellpage.ResumeLayout(false);
            this.shellpage.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private MaterialSkin.Controls.MaterialTabControl materialTabControl1;
        private System.Windows.Forms.TabPage shellpage;
        private System.Windows.Forms.TextBox widthbox;
        private MaterialSkin.Controls.MaterialButton drawbutton;
        private MaterialSkin.Controls.MaterialTabSelector materialTabSelector1;
        private System.Windows.Forms.TextBox shellthickbox;
        private System.Windows.Forms.TextBox heigthbox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox depthbox;
        private System.Windows.Forms.Label label6;
        private MaterialSkin.Controls.MaterialComboBox hbbpositionbox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private MaterialSkin.Controls.MaterialComboBox sectionsbox;
        private MaterialSkin.Controls.MaterialButton nextbutton;
        private MaterialSkin.Controls.MaterialButton backbutton;
    }
}