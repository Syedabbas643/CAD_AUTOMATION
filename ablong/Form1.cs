using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Exception = System.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Arc = Autodesk.AutoCAD.DatabaseServices.Arc;
using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;
using Region = System.Drawing.Region;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MaterialSkin;
using CAD_AUTOMATION;
using MaterialSkin.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Font = System.Drawing.Font;
using Color = System.Drawing.Color;
using Brushes = System.Drawing.Brushes;

namespace CAD_AUTOMATION
{
    public partial class Form1 : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        double l;
        private double width;
        private double length;
        private double thick;
        private int sections;
        private double zchanneltb;
        private double zchannelside;
        private double vchannelsize;
        private double hchannelsize;
        private Point3d ps1, ps2, ps3, ps4, ps5, ps6, ps7, ps8;
        private Point3d pz1, pz2, pz3, pz4, pz5, pz6, pz7, pz8, pz9, pz10, pz11, pz12, pz13, pz14;

        

        BlockTableRecord shellLeft;
        BlockTableRecord shellRight;
        BlockTableRecord shellTop;
        BlockTableRecord shellBottom;
        private NameValueCollection config;

        public Form1(Point3d insert)
        {
            InitializeComponent();
            l = insert.X;
            this.Region = new Region(CreateRoundedRectangle(this.ClientRectangle, 30));
            var materialSkinManager = MaterialSkinManager.Instance;
            //materialSkinManager.AddFormToManage(this); // Optional if you are not using MaterialForm
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue800, // Primary color
                Primary.BlueGrey900, // Darker primary color
                Primary.BlueGrey500, // Light primary color
                Accent.Green700,     // Accent color
                TextShade.WHITE
                
            );

            RoundCorners(materialTabControl1, 20);
            RoundCorners(materialTabSelector1, 10);
            RoundCorners(widthbox, 10);
            RoundCorners(heigthbox, 10);
            RoundCorners(shellthickbox, 10);

            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
        }
        private void materialButton2_Click(object sender, EventArgs e)
        {
            if (materialTabControl1.SelectedTab.Name == "shellpage")
            {
                int baseX = 400, baseY = 20;
                int labelWidth = 150, labelHeight = 20;
                int textBoxWidth = 150, textBoxHeight = 21;
                int buttonWidth = 155, buttonHeight = 37;
                int spacingY = 40;

                // Get the selected count from the combo box
                if (int.TryParse(sectionsbox.SelectedItem.ToString(), out int tabCount))
                {
                    
                    int currentTabCount = materialTabControl1.TabPages.Count -1;
                    if (currentTabCount < tabCount)
                    {
                        for (int i2 = currentTabCount +1 ; i2 <= tabCount; i2++)
                        {
                            var tabPage = new TabPage
                            {
                                Text = $"Section - {i2}", // Set tab text
                                Name = $"sec{i2}page", // Set unique name
                            };
                            //MessageBox.Show($"sec{i2}page");
                            for (int i = 1; i <= 8; i++)
                            {
                                // Create Label
                                Label partitionLabel = new Label
                                {
                                    Name = $"labelSecPart{i}",
                                    Text = $"Partition {i}",
                                    Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular),
                                    ForeColor = SystemColors.Control,
                                    Location = new Point(baseX, baseY + (i - 1) * spacingY),
                                    Size = new Size(labelWidth, labelHeight),
                                    AutoSize = true
                                };
                                tabPage.Controls.Add(partitionLabel);

                                // Create TextBox
                                TextBox partitionTextBox = new TextBox
                                {
                                    Name = $"sec{i2}part{i}",
                                    Text = "500",
                                    Font = new Font("Microsoft Tai Le", 12F, FontStyle.Regular),
                                    ForeColor = Color.White,
                                    BackColor = Color.FromArgb(64, 64, 64),
                                    BorderStyle = BorderStyle.None,
                                    TextAlign = HorizontalAlignment.Center,
                                    MinimumSize = new Size(textBoxWidth, textBoxHeight),
                                    Location = new Point(baseX + 90, baseY + (i - 1) * spacingY),
                                    Size = new Size(textBoxWidth, textBoxHeight),
                                };
                                tabPage.Controls.Add(partitionTextBox);
                                RoundCorners(partitionTextBox, 10);

                                MaterialSkin.Controls.MaterialCheckbox mPlateCheckbox = new MaterialSkin.Controls.MaterialCheckbox
                                {
                                    Name = $"mp{i2}part{i}",
                                    Text = "Mounting plate",
                                    Depth = 0,
                                    ForeColor = Color.White,
                                    Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
                                    TextAlign = ContentAlignment.TopLeft,
                                    CheckAlign = ContentAlignment.TopCenter,
                                    Location = new Point(baseX + 90 + 165, baseY + (i - 1) * spacingY - 5),
                                    Margin = new Padding(0),
                                    MouseLocation = new Point(-1, -1),
                                    MouseState = MaterialSkin.MouseState.HOVER,
                                    Ripple = true,
                                    Size = new Size(buttonWidth, buttonHeight),
                                    UseVisualStyleBackColor = true
                                };
                                tabPage.Controls.Add(mPlateCheckbox);



                            }

                            // Add Section Size Label and TextBox at the top
                            Label sectionSizeLabel = new Label
                            {
                                Name = $"labelSectionSize{i2}",
                                Text = "Section size",
                                Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular),
                                ForeColor = SystemColors.Control,
                                Location = new Point(40, 20),
                                Size = new Size(labelWidth, labelHeight),
                                AutoSize = true
                            };
                            tabPage.Controls.Add(sectionSizeLabel);

                            TextBox sectionSizeTextBox = new TextBox
                            {
                                Name = $"sec{i2}size",
                                Text = "500",
                                Font = new Font("Microsoft Tai Le", 12F, FontStyle.Regular),
                                ForeColor = Color.White,
                                BackColor = Color.FromArgb(64, 64, 64),
                                BorderStyle = BorderStyle.None,
                                TextAlign = HorizontalAlignment.Center,
                                MinimumSize = new Size(textBoxWidth, textBoxHeight),
                                Location = new Point(170, 20),
                                Size = new Size(textBoxWidth, textBoxHeight),
                            };
                            tabPage.Controls.Add(sectionSizeTextBox);
                            RoundCorners(sectionSizeTextBox, 10);

                            // Add Section Size Label and TextBox at the top
                            Label partLabel = new Label
                            {
                                Name = $"labelpart{i2}",
                                Text = "Partitions",
                                Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular),
                                ForeColor = SystemColors.Control,
                                Location = new Point(40, 70),
                                Size = new Size(labelWidth, labelHeight),
                                AutoSize = true
                            };
                            tabPage.Controls.Add(partLabel);

                            // Create Material ComboBox
                            MaterialSkin.Controls.MaterialComboBox partitionComboBox = new MaterialSkin.Controls.MaterialComboBox
                            {
                                Name = $"sec{i2}partbox",
                                AutoResize = false,
                                BackColor = Color.White,
                                Depth = 0,
                                DrawMode = DrawMode.OwnerDrawVariable,
                                DropDownHeight = 147,
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                DropDownWidth = 100,
                                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold, GraphicsUnit.Pixel),
                                ForeColor = Color.White,
                                FormattingEnabled = true,
                                IntegralHeight = false,
                                ItemHeight = 29,
                                Items = { "1", "2", "3", "4", "5", "6", "7", "8" },
                                Location = new Point(170, 60),
                                MaxDropDownItems = 5,
                                MouseState = MaterialSkin.MouseState.OUT,
                                Size = new Size(149, 35),
                                StartIndex = 0,
                                TabIndex = 15,
                                UseAccent = true,
                                UseTallSize = false
                            };
                            tabPage.Controls.Add(partitionComboBox);

                            tabPage.BackColor = Color.FromArgb(45, 45, 45);
                            materialTabControl1.TabPages.Add(tabPage);
                        }
                    }
                    else if (currentTabCount > tabCount)
                    {
                        // Remove the excess tabs if the new selected count is smaller
                        for (int i = currentTabCount; i >= tabCount; i--)
                        {
                            materialTabControl1.TabPages.RemoveAt(i); // Remove excess tabs
                        }
                    }

                    int currentTabIndex = materialTabControl1.SelectedIndex;
                    if (currentTabIndex < materialTabControl1.TabPages.Count - 1)
                    {
                        materialTabControl1.SelectedIndex = currentTabIndex + 1;
                    }
                    else
                    {
                        drawbutton.Enabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Please select a valid number from the dropdown.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // If not in maintab, navigate to the next tab
                int currentTabIndex = materialTabControl1.SelectedIndex;
                if (currentTabIndex < materialTabControl1.TabPages.Count - 1)
                {
                    materialTabControl1.SelectedIndex = currentTabIndex + 1;
                }
                else
                {
                    drawbutton.Enabled = true;
                }
            }

        }
        private void backbutton_Click(object sender, EventArgs e)
        {
            // If not in maintab, navigate to the next tab
            int currentTabIndex = materialTabControl1.SelectedIndex;
            if(currentTabIndex == 0) 
            { 

            }
            else if (currentTabIndex < materialTabControl1.TabPages.Count - 1)
            {
                materialTabControl1.SelectedIndex = currentTabIndex - 1;
                
            }
            else
            {
                materialTabControl1.SelectedIndex = currentTabIndex - 1;
                drawbutton.Enabled = false;
            }
        }
        private void materialButton1_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor editor = doc.Editor;

                config = new System.Collections.Specialized.NameValueCollection();
                string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // Define the path to the http.exe
                string iniFilePath = Path.Combine(pluginDirectory, "gi_config_in.ini");

                if (File.Exists(iniFilePath))
                {
                    var lines = File.ReadAllLines(iniFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            config[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("\nConfiguration file not found: " + iniFilePath);
                    return;
                }

                width = Convert.ToDouble(heigthbox.Text);
                length = Convert.ToDouble(widthbox.Text);
                thick = Convert.ToDouble(shellthickbox.Text);
                sections = Convert.ToInt32(sectionsbox.Text);
                zchanneltb = Convert.ToDouble(config["top_bottom_shell_size"]);
                zchannelside = Convert.ToDouble(config["side_shell_size"]);

                ps1 = new Point3d(l, 0, 0);
                ps2 = new Point3d(l + length, 0, 0);
                ps3 = new Point3d(l + length, width, 0);
                ps4 = new Point3d(l, width, 0);
                ps5 = new Point3d(ps1.X + zchannelside, ps1.Y + zchanneltb, 0);
                ps6 = new Point3d(ps2.X - zchannelside, ps5.Y, 0);
                ps7 = new Point3d(ps6.X, ps3.Y - zchanneltb, 0);
                ps8 = new Point3d(ps5.X, ps7.Y, 0);

                // Z Channel Points
                pz1 = new Point3d(l + thick, zchanneltb, 0);
                pz2 = new Point3d(pz1.X, width - zchanneltb, 0);
                pz3 = new Point3d(pz1.X + zchannelside, pz2.Y, 0);
                pz4 = new Point3d(pz3.X, pz1.Y, 0);
                pz5 = new Point3d(l + length - thick, pz1.Y, 0);
                pz6 = new Point3d(pz5.X, width - zchanneltb, 0);
                pz7 = new Point3d(pz5.X - zchannelside, pz6.Y, 0);
                pz8 = new Point3d(pz7.X, pz1.Y, 0);
                pz9 = new Point3d(pz1.X + thick, pz1.Y, 0);
                pz10 = new Point3d(pz9.X, pz2.Y, 0);
                pz11 = new Point3d(pz3.X - thick, pz2.Y, 0);
                pz12 = new Point3d(pz11.X, pz1.Y, 0);
                pz13 = new Point3d(pz1.X, pz2.Y + zchanneltb - thick, 0);
                pz14 = new Point3d(l - thick + length, pz1.Y, 0);


                using (Transaction trans = db.TransactionManager.StartTransaction())
               {
                    // Access the BlockTable and ModelSpace
                    BlockTable blockTable = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                    BlockTableRecord modelSpace = (BlockTableRecord)db.CurrentSpaceId.GetObject(OpenMode.ForWrite);

                    if(blockTable.Has("shellLeft") || blockTable.Has("shellRight") || blockTable.Has("shellTop") || blockTable.Has("shellBottom"))
                    {
                        MessageBox.Show("Block already exists. Try in a new drawing file");
                        return;
                    }

                    blockTable.UpgradeOpen();

                    shellLeft = new BlockTableRecord { Name = "shellLeft" };
                    blockTable.Add(shellLeft);
                    trans.AddNewlyCreatedDBObject(shellLeft, true);

                    shellRight = new BlockTableRecord { Name = "shellRight" };
                    blockTable.Add(shellRight);
                    trans.AddNewlyCreatedDBObject(shellRight, true);

                    shellTop = new BlockTableRecord { Name = "shellTop" };
                    blockTable.Add(shellTop);
                    trans.AddNewlyCreatedDBObject(shellTop, true);

                    shellBottom = new BlockTableRecord { Name = "shellBottom" };
                    blockTable.Add(shellBottom);
                    trans.AddNewlyCreatedDBObject(shellBottom, true);

                    Line linez1 = new Line(pz11, pz3) { ColorIndex = 6 };
                    shellLeft.AppendEntity(linez1);
                    trans.AddNewlyCreatedDBObject(linez1, true);
                    Line linez2 = new Line(pz11, pz3) { ColorIndex = 6 };
                    shellRight.AppendEntity(linez2);
                    trans.AddNewlyCreatedDBObject(linez2, true);
                    Vector3d linez2move = pz12.GetVectorTo(pz8);
                    linez2.TransformBy(Matrix3d.Displacement(linez2move));
                    Line linez3 = new Line(pz12, pz4) { ColorIndex = 6 };
                    shellLeft.AppendEntity(linez3);
                    trans.AddNewlyCreatedDBObject(linez3, true);
                    Line linez4 = new Line(pz11, pz12) { ColorIndex = 6 };
                    shellLeft.AppendEntity(linez4);
                    trans.AddNewlyCreatedDBObject(linez4, true);
                    Line linez5 = new Line(pz3, pz7) { ColorIndex = 6 };
                    shellTop.AppendEntity(linez5);
                    trans.AddNewlyCreatedDBObject(linez5, true);
                    Line linez6 = new Line(pz9, pz10) { ColorIndex = 6 };
                    shellRight.AppendEntity(linez6);
                    trans.AddNewlyCreatedDBObject(linez6, true);
                    Vector3d linez6move = pz1.GetVectorTo(pz8);
                    linez6.TransformBy(Matrix3d.Displacement(linez6move));

                    // Add lines to shellLeft block
                    Line lineP4 = new Line(ps4, ps1) { ColorIndex = 6 };
                    Line lineP13 = new Line(ps1, ps5) { ColorIndex = 6 };
                    Line lineP14 = new Line(ps4, ps8) { ColorIndex = 6 };
                    shellLeft.AppendEntity(lineP4);
                    shellLeft.AppendEntity(lineP13);
                    shellLeft.AppendEntity(lineP14);
                    trans.AddNewlyCreatedDBObject(lineP4, true);
                    trans.AddNewlyCreatedDBObject(lineP13, true);
                    trans.AddNewlyCreatedDBObject(lineP14, true);

                    // Add lines to shellRight block
                    Line lineP2 = new Line(ps2, ps3) { ColorIndex = 6 };
                    Line lineP15 = new Line(ps7, ps3) { ColorIndex = 6 };
                    Line lineP16 = new Line(ps2, ps6) { ColorIndex = 6 };
                    shellRight.AppendEntity(lineP2);
                    shellRight.AppendEntity(lineP15);
                    shellRight.AppendEntity(lineP16);
                    trans.AddNewlyCreatedDBObject(lineP2, true);
                    trans.AddNewlyCreatedDBObject(lineP15, true);
                    trans.AddNewlyCreatedDBObject(lineP16, true);

                    // Add lines to shellTop block
                    Line lineP3 = new Line(ps3, ps4) { ColorIndex = 6 };
                    Line lineP11 = new Line(ps7, ps3) { ColorIndex = 6 };
                    Line lineP10 = new Line(ps4, ps8) { ColorIndex = 6 };
                    Line lineP18 = new Line(new Point3d(ps4.X + thick, ps4.Y - thick,0), new Point3d(ps3.X - thick, ps3.Y - thick,0)) { ColorIndex = 6 };
                    shellTop.AppendEntity(lineP10);
                    shellTop.AppendEntity(lineP3);
                    shellTop.AppendEntity(lineP11);
                    shellTop.AppendEntity(lineP18);
                    trans.AddNewlyCreatedDBObject(lineP3, true);
                    trans.AddNewlyCreatedDBObject(lineP10, true);
                    trans.AddNewlyCreatedDBObject(lineP11, true);
                    trans.AddNewlyCreatedDBObject(lineP18, true);

                    // Add lines to shellBottom block
                    Line lineP1 = new Line(ps1, ps2) { ColorIndex = 6 };
                    Line lineP5 = new Line(ps5, ps6) { ColorIndex = 6 };
                    Line lineP12 = new Line(ps2, ps6) { ColorIndex = 6 };
                    Line lineP9 = new Line(ps1, ps5) { ColorIndex = 6 };
                    Line lineP17 = new Line(new Point3d(ps1.X + thick,ps1.Y + thick,0), new Point3d(ps2.X - thick,ps2.Y + thick,0)) { ColorIndex = 6 };
                    shellBottom.AppendEntity(lineP1);
                    shellBottom.AppendEntity(lineP5);
                    shellBottom.AppendEntity(lineP12);
                    shellBottom.AppendEntity(lineP9);
                    shellBottom.AppendEntity(lineP17);
                    trans.AddNewlyCreatedDBObject(lineP1, true);
                    trans.AddNewlyCreatedDBObject(lineP5, true);
                    trans.AddNewlyCreatedDBObject(lineP12, true);
                    trans.AddNewlyCreatedDBObject(lineP9, true);
                    trans.AddNewlyCreatedDBObject(lineP17, true);

                    DBObjectCollection offsetCurvesP2 = lineP2.GetOffsetCurves(thick);
                    foreach (DBObject obj in offsetCurvesP2)
                    {
                        Line offsetLine = obj as Line;
                        if (offsetLine != null)
                        {
                            // Add the offset line to the same block or space
                            shellRight.AppendEntity(offsetLine);
                            trans.AddNewlyCreatedDBObject(offsetLine, true);
                        }
                    }

                    DBObjectCollection offsetCurvesP3 = lineP4.GetOffsetCurves(thick);
                    foreach (DBObject obj in offsetCurvesP3)
                    {
                        Line offsetLine = obj as Line;
                        if (offsetLine != null)
                        {
                            // Add the offset line to the same block or space
                            shellLeft.AppendEntity(offsetLine);
                            trans.AddNewlyCreatedDBObject(offsetLine, true);
                        }
                    }

                    // Insert blocks into model space as block references
                    BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), shellLeft.ObjectId);
                    modelSpace.AppendEntity(shellLeftRef);
                    trans.AddNewlyCreatedDBObject(shellLeftRef, true);

                    BlockReference shellRightRef = new BlockReference(new Point3d(0, 0, 0), shellRight.ObjectId);
                    modelSpace.AppendEntity(shellRightRef);
                    trans.AddNewlyCreatedDBObject(shellRightRef, true);

                    BlockReference shellTopRef = new BlockReference(new Point3d(0, 0, 0), shellTop.ObjectId);
                    modelSpace.AppendEntity(shellTopRef);
                    trans.AddNewlyCreatedDBObject(shellTopRef, true);

                    BlockReference shellBottomRef = new BlockReference(new Point3d(0, 0, 0), shellBottom.ObjectId);
                    modelSpace.AppendEntity(shellBottomRef);
                    trans.AddNewlyCreatedDBObject(shellBottomRef, true);

                    for (int i = 1; i <= sections; i++)
                    {
                        string secPosition;

                        if (i == 1 && sections == 1)
                        {
                            secPosition = "full";
                        }
                        else if (i == 1)
                        {
                            secPosition = "first";
                        }
                        else if (i == sections)
                        {
                            secPosition = "last";
                        }
                        else
                        {
                            secPosition = "mid";
                        }

                        string controlName = $"sec{i}size";
                        TextBox sectionTextBox = null;

                        Control tabControl = this.Controls.Find("materialTabControl1", true).FirstOrDefault();
                        if (tabControl != null && tabControl is TabControl materialTabControl2)
                        {
                            // Locate the specific tab page for this section
                                TabPage targetTab = materialTabControl2.TabPages.Cast<TabPage>()
                                    .FirstOrDefault(tab => tab.Name == $"sec{i}page");

                                if (targetTab != null)
                                {
                                    
                                    // Find the control by name within the specific tab
                                    sectionTextBox = targetTab.Controls.Find(controlName, true).FirstOrDefault() as TextBox;

                                }
                                else
                                {
                                    MessageBox.Show($"Tab section-{i} not found.");
                                }
                            

                        }

                        if (sectionTextBox != null && !string.IsNullOrWhiteSpace(sectionTextBox.Text))
                        {
                            double sectionSize = Convert.ToDouble(sectionTextBox.Text);
                            drawsections(trans, blockTable, modelSpace, sectionSize, secPosition, i.ToString());
                        }
                        else
                        {
                            MessageBox.Show($"Section size TextBox '{controlName}' not found or is empty.");
                        }
                    }

                    trans.Commit();
               }

                this.Close();

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void drawsections(Transaction trans, BlockTable blockTable,BlockTableRecord modelSpace,double sectionsize, string position, string secnumber)
        {
            BlockTableRecord leftchannel = null;
            BlockTableRecord rightchannel = null;
            vchannelsize = Convert.ToDouble(config["vertical_channel_size"]);
            hchannelsize = Convert.ToDouble(config["horizontal_channel_size"]);
            double[] partSizes = new double[8]; // Array to store part sizes
            double partitioncount = 0;

            string partboxName = $"sec{secnumber}partbox";

            // Locate the MaterialTabControl
            Control tabControl = this.Controls.Find("materialTabControl1", true).FirstOrDefault();
            if (tabControl != null && tabControl is TabControl materialTabControl)
            {
                // Locate the specific tab page
                TabPage targetTab = materialTabControl.TabPages.Cast<TabPage>()
                    .FirstOrDefault(tab => tab.Name == $"sec{secnumber}page");

                if (targetTab != null)
                {
                    // Find the ComboBox inside the tab
                    Control[] foundControls = targetTab.Controls.Find(partboxName, true);
                    if (foundControls.Length > 0 && foundControls[0] is ComboBox partitionBox)
                    {
                        // Retrieve the selected text from the ComboBox
                        if (!string.IsNullOrWhiteSpace(partitionBox.Text))
                        {
                            partitioncount = Convert.ToDouble(partitionBox.Text);
                            
                        }
                        else
                        {
                            MessageBox.Show($"Partition box '{partboxName}' is empty or no selection made.");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Partition box '{partboxName}' not found or not a ComboBox.");
                    }

                    for (int i = 1; i <= 8; i++)
                    {
                        string partName = $"sec{secnumber}part{i}";
                        Control[] foundControls2 = targetTab.Controls.Find(partName, true);

                        if (foundControls2.Length > 0 && foundControls2[0] is TextBox partTextBox)
                        {
                            if (!string.IsNullOrWhiteSpace(partTextBox.Text))
                            {
                                partSizes[i - 1] = Convert.ToDouble(partTextBox.Text);
                                
                            }
                            else
                            {
                                MessageBox.Show($"Part {i} found but TextBox is empty.");
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Control '{partName}' not found or not a TextBox.");
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Tab page 'sec{secnumber}page' not found.");
                }
            }
            else
            {
                MessageBox.Show("MaterialTabControl 'materialTabControl1' not found or not a TabControl.");
            }


            if (position == "full")
            {
                leftchannel = shellLeft;
                rightchannel = shellRight;

                Point3d cz1 = new Point3d(pz3.X, pz2.Y - thick, 0);
                Point3d cz6 = new Point3d(pz7.X, pz2.Y - thick, 0);
                Point3d czb1 = new Point3d(pz3.X, pz1.Y + thick, 0);
                Point3d czb6 = new Point3d(pz7.X, pz1.Y + thick, 0);

                Line linect1 = new Line(pz3, cz1) { ColorIndex = 6 };
                shellTop.AppendEntity(linect1);
                trans.AddNewlyCreatedDBObject(linect1, true);
                Line linect2 = new Line(cz6, cz1) { ColorIndex = 6 };
                shellTop.AppendEntity(linect2);
                trans.AddNewlyCreatedDBObject(linect2, true);
                Line linect7 = new Line(cz6, pz7) { ColorIndex = 6 };
                shellTop.AppendEntity(linect7);
                trans.AddNewlyCreatedDBObject(linect7, true);

                Line linecb1 = new Line(pz4, czb1) { ColorIndex = 6 };
                shellBottom.AppendEntity(linecb1);
                trans.AddNewlyCreatedDBObject(linecb1, true);
                Line linecb2 = new Line(czb1, czb6) { ColorIndex = 6 };
                shellBottom.AppendEntity(linecb2);
                trans.AddNewlyCreatedDBObject(linecb2, true);
                Line linecb7 = new Line(czb6, pz8) { ColorIndex = 6 };
                shellBottom.AppendEntity(linecb7);
                trans.AddNewlyCreatedDBObject(linecb7, true);
            }
            else if (position == "first")
            {
                leftchannel = shellLeft;
                rightchannel = new BlockTableRecord { Name = "v1" };
                blockTable.Add(rightchannel);
                trans.AddNewlyCreatedDBObject(rightchannel, true);

                Point3d cz1 = new Point3d(pz3.X, pz2.Y - thick, 0);
                Point3d cz2 = new Point3d(l + sectionsize - (vchannelsize/2) ,pz2.Y - thick, 0);
                Point3d cz3 = new Point3d(l + sectionsize - (vchannelsize / 2), pz2.Y, 0);
                Point3d cz6 = new Point3d(l + sectionsize, pz2.Y, 0);

                Point3d czb1 = new Point3d(pz4.X, pz1.Y + thick, 0);
                Point3d czb2 = new Point3d(l + sectionsize - (vchannelsize / 2),pz1.Y + thick, 0);
                Point3d czb3 = new Point3d(l + sectionsize - (vchannelsize / 2),pz1.Y, 0);
                Point3d czb6 = new Point3d(l + sectionsize, pz1.Y, 0);

                drawline(trans, shellTop, pz3, cz1, 6);
                drawline(trans, shellTop, cz1, cz2, 6);
                drawline(trans, shellTop, cz2, cz3, 6);
                drawline(trans, shellTop, cz3, cz6, 6);

                drawline(trans, shellBottom, pz4, czb1, 6);
                drawline(trans, shellBottom, czb1, czb2, 6);
                drawline(trans, shellBottom, czb2, czb3, 6);
                drawline(trans, shellBottom, czb3, czb6, 6);

                drawline(trans, rightchannel, cz6, new Point3d(cz3.X + thick,cz6.Y,0));
                drawline(trans, rightchannel, czb6, new Point3d(czb3.X + thick, czb6.Y, 0));
                drawline(trans, rightchannel, new Point3d(cz3.X + thick, cz6.Y, 0), new Point3d(czb3.X + thick, czb6.Y, 0));

                BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), rightchannel.ObjectId);
                modelSpace.AppendEntity(shellLeftRef);
                trans.AddNewlyCreatedDBObject(shellLeftRef, true);
            }
            else if (position == "mid")
            {
                double sec = 0;
                int secnum = Convert.ToInt32(secnumber);
                string leftsec = "v"+(secnum - 1).ToString();
                string rightsec = "v" + (secnum).ToString();

                if (tabControl != null && tabControl is TabControl materialTabControl2)
                {
                    for (int i = 1; i <= secnum - 1; i++)
                    {
                        // Locate the specific tab page for this section
                        TabPage targetTab = materialTabControl2.TabPages.Cast<TabPage>()
                            .FirstOrDefault(tab => tab.Name == $"sec{i}page");

                        if (targetTab != null)
                        {
                            string controlName = $"sec{i}size";

                            // Find the control by name within the specific tab
                            TextBox partTextBox = targetTab.Controls.Find(controlName, true).FirstOrDefault() as TextBox;

                            if (partTextBox != null && !string.IsNullOrWhiteSpace(partTextBox.Text))
                            {
                                sec += Convert.ToDouble(partTextBox.Text); // Sum the sizes
                            }
                            else
                            {
                                MessageBox.Show($"Control {controlName} not found or empty in section-{i}.");
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Tab section-{i} not found.");
                        }
                    }

                }

                if (blockTable.Has(leftsec))
                {
                    leftchannel = (BlockTableRecord)blockTable[leftsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    leftchannel = new BlockTableRecord { Name = leftsec };
                    blockTable.Add(leftchannel);
                    trans.AddNewlyCreatedDBObject(leftchannel, true);
                }

                if (blockTable.Has(rightsec))
                {
                    rightchannel = (BlockTableRecord)blockTable[rightsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    rightchannel = new BlockTableRecord { Name = rightsec };
                    blockTable.Add(rightchannel);
                    trans.AddNewlyCreatedDBObject(rightchannel, true);
                }

                Point3d cz1 = new Point3d(l + sec, pz2.Y, 0);
                Point3d cz2 = new Point3d(l + sec + (vchannelsize /2), pz2.Y, 0);
                Point3d cz3 = new Point3d(l + sec +(vchannelsize / 2), pz2.Y- thick, 0);
                Point3d cz4 = new Point3d(l + sec + sectionsize - (vchannelsize / 2), pz2.Y - thick, 0);
                Point3d cz5 = new Point3d(l + sec + sectionsize - (vchannelsize / 2), pz2.Y, 0);
                Point3d cz6 = new Point3d(l + sec + sectionsize, pz2.Y, 0);

                Point3d czb1 = new Point3d(l + sec, pz1.Y, 0);
                Point3d czb2 = new Point3d(l + sec + (vchannelsize / 2), pz1.Y, 0);
                Point3d czb3 = new Point3d(l + sec + (vchannelsize / 2), pz1.Y + thick, 0);
                Point3d czb4 = new Point3d(l + sec + sectionsize - (vchannelsize / 2), pz1.Y + thick, 0);
                Point3d czb5 = new Point3d(l + sec + sectionsize - (vchannelsize / 2), pz1.Y, 0);
                Point3d czb6 = new Point3d(l + sec + sectionsize, pz1.Y, 0);

                drawline(trans, shellTop, pz3, cz1, 6);
                drawline(trans, shellTop, cz1, cz2, 6);
                drawline(trans, shellTop, cz2, cz3, 6);
                drawline(trans, shellTop, cz3, cz4, 6);
                drawline(trans, shellTop, cz4, cz5, 6);
                drawline(trans, shellTop, cz5, cz6, 6);

                drawline(trans, shellBottom, pz4, czb1, 6);
                drawline(trans, shellBottom, czb1, czb2, 6);
                drawline(trans, shellBottom, czb2, czb3, 6);
                drawline(trans, shellBottom, czb3, czb4, 6);
                drawline(trans, shellBottom, czb4, czb5, 6);
                drawline(trans, shellBottom, czb5, czb6, 6);

                drawline(trans, leftchannel, cz1, new Point3d(cz2.X - thick, cz1.Y, 0));
                drawline(trans, leftchannel, czb1, new Point3d(czb2.X - thick, czb1.Y, 0));
                drawline(trans, leftchannel, new Point3d(cz2.X - thick, cz1.Y, 0), new Point3d(czb2.X - thick, czb1.Y, 0));

                drawline(trans, rightchannel, cz6, new Point3d(cz5.X + thick, cz6.Y, 0));
                drawline(trans, rightchannel, czb6, new Point3d(czb5.X + thick, czb6.Y, 0));
                drawline(trans, rightchannel, new Point3d(cz5.X + thick, cz6.Y, 0), new Point3d(czb5.X + thick, czb6.Y, 0));

                //BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), leftchannel.ObjectId);
                //modelSpace.AppendEntity(shellLeftRef);
                //trans.AddNewlyCreatedDBObject(shellLeftRef, true);
                BlockReference shellrightRef = new BlockReference(new Point3d(0, 0, 0), rightchannel.ObjectId);
                modelSpace.AppendEntity(shellrightRef);
                trans.AddNewlyCreatedDBObject(shellrightRef, true);

            }
            else if (position == "last")
            {
                
                int secnum = Convert.ToInt32(secnumber);
                string leftsec = "v" + (secnum - 1).ToString();
                if (blockTable.Has(leftsec))
                {
                    leftchannel = (BlockTableRecord)blockTable[leftsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    MessageBox.Show("Cant find Block, please try in a new file");
                    return;
                }
                rightchannel = shellRight;

                Point3d cz1 = new Point3d(pz7.X, pz2.Y - thick, 0);
                Point3d cz2 = new Point3d(l + length - sectionsize + (vchannelsize/2), pz2.Y - thick, 0);
                Point3d cz3 = new Point3d(l + length - sectionsize + (vchannelsize/2), pz2.Y, 0);
                Point3d cz6 = new Point3d(l + length - sectionsize, pz2.Y, 0);

                Point3d czb1 = new Point3d(pz8.X, pz1.Y + thick, 0);
                Point3d czb2 = new Point3d(l + length - sectionsize + (vchannelsize / 2), pz1.Y + thick, 0);
                Point3d czb3 = new Point3d(l + length - sectionsize + (vchannelsize / 2), pz1.Y, 0);
                Point3d czb6 = new Point3d(l + length - sectionsize, pz1.Y, 0);

                drawline(trans, shellTop, pz7, cz1, 6);
                drawline(trans, shellTop, cz1, cz2, 6);
                drawline(trans, shellTop, cz2, cz3, 6);
                drawline(trans, shellTop, cz3, cz6, 6);

                drawline(trans, shellBottom, pz8, czb1, 6);
                drawline(trans, shellBottom, czb1, czb2, 6);
                drawline(trans, shellBottom, czb2, czb3, 6);
                drawline(trans, shellBottom, czb3, czb6, 6);

                drawline(trans, leftchannel, cz6, new Point3d(cz3.X - thick, cz6.Y, 0));
                drawline(trans, leftchannel, czb6, new Point3d(czb3.X - thick, czb6.Y, 0));
                drawline(trans, leftchannel, new Point3d(cz3.X - thick, cz6.Y, 0), new Point3d(czb3.X - thick, czb6.Y, 0));

                //BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), leftchannel.ObjectId);
                //modelSpace.AppendEntity(shellLeftRef);
                //trans.AddNewlyCreatedDBObject(shellLeftRef, true);

            }


            for (int i = 0; i < partitioncount; i++)
            {
                string partitionPosition = partitioncount == 1 ? "full" :
                                           i == 0 ? "first" :
                                           i == partitioncount - 1 ? "last" : "mid";

                string partitionIndex = (i + 1).ToString(); // Partition index starts from 1
                drawpartitions(trans, blockTable, modelSpace, leftchannel, rightchannel, partSizes[i], partitionPosition, partitionIndex, secnumber, position,sectionsize);
            }
        }
        private void drawpartitions(Transaction trans, BlockTable blockTable, BlockTableRecord modelSpace, BlockTableRecord leftchannel, BlockTableRecord rightchannel, double partsize, string partposition, string partnumber,string secnumber, string secposition,double secsize)
        {
            double leftpoint = 0;
            double rightpoint = 0;
            BlockTableRecord topchannel = null;
            BlockTableRecord bottomchannel = null;

            // Calculate cumulative section size dynamically
            double cumulativeSize = 0;
            int currentSec = Convert.ToInt32(secnumber);

            Control tabControl = this.Controls.Find("materialTabControl1", true).FirstOrDefault();
            if (tabControl != null && tabControl is TabControl materialTabControl2)
            {
                for (int i = 1; i <= currentSec - 1; i++)
                {
                    // Locate the specific tab page for this section
                    TabPage targetTab = materialTabControl2.TabPages.Cast<TabPage>()
                        .FirstOrDefault(tab => tab.Name == $"sec{i}page");

                    if (targetTab != null)
                    {
                        string controlName = $"sec{i}size";

                        // Find the control by name within the specific tab
                        TextBox partTextBox = targetTab.Controls.Find(controlName, true).FirstOrDefault() as TextBox;

                        if (partTextBox != null && !string.IsNullOrWhiteSpace(partTextBox.Text))
                        {
                            cumulativeSize += Convert.ToDouble(partTextBox.Text); // Sum the sizes
                        }
                        else
                        {
                            MessageBox.Show($"Control {controlName} not found or empty in section-{i}.");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Tab section-{i} not found.");
                    }
                }

            }

            // Calculate leftpoint and rightpoint based on secposition
            switch (secposition)
            {
                case "full":
                    leftpoint = l + zchannelside + thick;
                    rightpoint = l + secsize - zchannelside - thick;
                    break;

                case "first":
                    leftpoint = l + zchannelside + thick;
                    rightpoint = l + secsize - (vchannelsize / 2);
                    break;

                case "mid":
                    leftpoint = l + cumulativeSize + (vchannelsize / 2);
                    rightpoint = leftpoint + secsize - vchannelsize;
                    break;

                case "last":
                    leftpoint = l + cumulativeSize + (vchannelsize / 2);
                    rightpoint = l + cumulativeSize + secsize - zchannelside - thick;
                    break;

                default:
                    MessageBox.Show($"Invalid secposition: {secposition}");
                    return;
            }


            if (partposition == "full")
            {
                Point3d l1 = new Point3d(leftpoint, pz1.Y, 0);
                Point3d l6 = new Point3d(leftpoint, pz2.Y, 0);
                Point3d r1 = new Point3d(rightpoint, pz1.Y, 0);
                Point3d r6 = new Point3d(rightpoint, pz2.Y, 0);
                drawline(trans, leftchannel, l1, l6);
                drawline(trans, rightchannel, r1, r6);
            }
            else if(partposition == "first")
            {
                Point3d l1 = new Point3d(leftpoint, pz1.Y, 0);
                Point3d l2 = new Point3d(leftpoint, ps1.Y + partsize - (hchannelsize/2), 0);
                Point3d l3 = new Point3d(leftpoint - thick, ps1.Y + partsize - (hchannelsize / 2), 0);
                Point3d l4 = new Point3d(leftpoint - thick, ps1.Y + partsize, 0);

                Point3d r1 = new Point3d(rightpoint, pz1.Y, 0);
                Point3d r2 = new Point3d(rightpoint, ps1.Y + partsize - (hchannelsize / 2), 0);
                Point3d r3 = new Point3d(rightpoint + thick, ps1.Y + partsize - (hchannelsize / 2), 0);
                Point3d r4 = new Point3d(rightpoint + thick, ps1.Y + partsize, 0);

                drawline(trans, leftchannel, l1, l2);
                drawline(trans, leftchannel, l2, l3);
                drawline(trans, leftchannel, l3, l4);

                drawline(trans, rightchannel, r1, r2);
                drawline(trans, rightchannel, r2, r3);
                drawline(trans, rightchannel, r3, r4);

                topchannel = new BlockTableRecord { Name = $"h{secnumber}_1" };
                blockTable.Add(topchannel);
                trans.AddNewlyCreatedDBObject(topchannel, true);

                drawline(trans, topchannel, l3, l4);
                drawline(trans, topchannel, r3, r4);
                drawline(trans, topchannel, r3, l3);
                drawline(trans, topchannel, new Point3d(l3.X,l3.Y + thick,0),new Point3d(r3.X,r3.Y + thick,0));

                BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), topchannel.ObjectId);
                modelSpace.AppendEntity(shellLeftRef);
                trans.AddNewlyCreatedDBObject(shellLeftRef, true);
            }
            else if(partposition == "mid")
            {

                double sec = 0;
                int partNum = Convert.ToInt32(partnumber);
                string leftsec = $"h{secnumber}_"  + (partNum - 1).ToString();
                string rightsec = $"h{secnumber}_" + (partNum).ToString();

                // Locate the MaterialTabControl
                //Control tabControl = this.Controls.Find("materialTabControl1", true).FirstOrDefault();
                if (tabControl != null && tabControl is TabControl materialTabControl)
                {
                    // Locate the specific tab for the given section number
                    TabPage targetTab = materialTabControl.TabPages.Cast<TabPage>()
                        .FirstOrDefault(tab => tab.Name == $"sec{secnumber}page");

                    if (targetTab != null)
                    {
                        // Iterate through all parts up to partNum - 1
                        for (int i = 1; i <= partNum - 1; i++)
                        {
                            string controlName = $"sec{secnumber}part{i}";

                            // Find the control by name within the specific tab
                            TextBox partTextBox = targetTab.Controls.Find(controlName, true).FirstOrDefault() as TextBox;

                            if (partTextBox != null && !string.IsNullOrWhiteSpace(partTextBox.Text))
                            {
                                sec += Convert.ToDouble(partTextBox.Text); // Sum the sizes
                            }
                            else
                            {
                                MessageBox.Show($"Control {controlName} not found or is empty.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Tab section-{secnumber} not found.");
                    }
                }
                else
                {
                    MessageBox.Show("MaterialTabControl not found.");
                }

                if (blockTable.Has(leftsec))
                {
                    bottomchannel = (BlockTableRecord)blockTable[leftsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    bottomchannel = new BlockTableRecord { Name = leftsec };
                    blockTable.Add(bottomchannel);
                    trans.AddNewlyCreatedDBObject(bottomchannel, true);
                }

                if (blockTable.Has(rightsec))
                {
                    topchannel = (BlockTableRecord)blockTable[rightsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    topchannel = new BlockTableRecord { Name = rightsec };
                    blockTable.Add(topchannel);
                    trans.AddNewlyCreatedDBObject(topchannel, true);
                }


                Point3d l1 = new Point3d(leftpoint - thick, ps1.Y + sec, 0);
                Point3d l2 = new Point3d(leftpoint - thick, ps1.Y + sec + (hchannelsize/2), 0);
                Point3d l3 = new Point3d(leftpoint, ps1.Y + sec + (hchannelsize / 2), 0);
                Point3d l4 = new Point3d(leftpoint, ps1.Y + sec+ partsize - (hchannelsize / 2), 0);
                Point3d l5 = new Point3d(leftpoint - thick, ps1.Y + sec + partsize - (hchannelsize / 2), 0);
                Point3d l6 = new Point3d(leftpoint - thick, ps1.Y + sec + partsize, 0);

                Point3d r1 = new Point3d(rightpoint + thick, ps1.Y + sec, 0);
                Point3d r2 = new Point3d(rightpoint + thick, ps1.Y + sec + (hchannelsize / 2), 0);
                Point3d r3 = new Point3d(rightpoint, ps1.Y + sec + (hchannelsize / 2), 0);
                Point3d r4 = new Point3d(rightpoint, ps1.Y + sec + partsize - (hchannelsize / 2), 0);
                Point3d r5 = new Point3d(rightpoint + thick, ps1.Y + sec + partsize - (hchannelsize / 2), 0);
                Point3d r6 = new Point3d(rightpoint + thick, ps1.Y + sec + partsize, 0);

                drawline(trans, leftchannel, l1, l2);
                drawline(trans, leftchannel, l2, l3);
                drawline(trans, leftchannel, l3, l4);
                drawline(trans, leftchannel, l4, l5);
                drawline(trans, leftchannel, l5, l6);

                drawline(trans, rightchannel, r1, r2);
                drawline(trans, rightchannel, r2, r3);
                drawline(trans, rightchannel, r3, r4);
                drawline(trans, rightchannel, r4, r5);
                drawline(trans, rightchannel, r5, r6);

                drawline(trans, bottomchannel, l1, l2);
                drawline(trans, bottomchannel, l2, r2);
                drawline(trans, bottomchannel, r2, r1);
                drawline(trans, bottomchannel, new Point3d(l2.X, l2.Y - thick, 0), new Point3d(r2.X, r2.Y - thick, 0));

                drawline(trans, topchannel, l6, l5);
                drawline(trans, topchannel, l5, r5);
                drawline(trans, topchannel, r5, r6);
                drawline(trans, topchannel, new Point3d(l5.X, l5.Y + thick, 0), new Point3d(r5.X, r5.Y + thick, 0));

                BlockReference shellLeftRef = new BlockReference(new Point3d(0, 0, 0), topchannel.ObjectId);
                modelSpace.AppendEntity(shellLeftRef);
                trans.AddNewlyCreatedDBObject(shellLeftRef, true);

            }
            else if (partposition == "last")
            {
                Point3d l1 = new Point3d(leftpoint, pz2.Y, 0);
                Point3d l2 = new Point3d(leftpoint, ps4.Y - partsize + (hchannelsize / 2), 0);
                Point3d l3 = new Point3d(leftpoint - thick, ps4.Y - partsize + (hchannelsize / 2), 0);
                Point3d l4 = new Point3d(leftpoint - thick, ps4.Y - partsize, 0);

                Point3d r1 = new Point3d(rightpoint, pz2.Y, 0);
                Point3d r2 = new Point3d(rightpoint, ps4.Y - partsize + (hchannelsize / 2), 0);
                Point3d r3 = new Point3d(rightpoint + thick, ps4.Y - partsize + (hchannelsize / 2), 0);
                Point3d r4 = new Point3d(rightpoint + thick, ps4.Y - partsize, 0);

                drawline(trans, leftchannel, l1, l2);
                drawline(trans, leftchannel, l2, l3);
                drawline(trans, leftchannel, l3, l4);

                drawline(trans, rightchannel, r1, r2);
                drawline(trans, rightchannel, r2, r3);
                drawline(trans, rightchannel, r3, r4);

                int partnum = Convert.ToInt32(partnumber);
                string leftsec = $"h{secnumber}_" + (partnum - 1).ToString();
                if (blockTable.Has(leftsec))
                {
                    bottomchannel = (BlockTableRecord)blockTable[leftsec].GetObject(OpenMode.ForWrite);
                }
                else
                {
                    MessageBox.Show("Cant find Block, please try in a new file");
                    return;
                }

                drawline(trans, bottomchannel, l3, l4);
                drawline(trans, bottomchannel, r3, r4);
                drawline(trans, bottomchannel, r3, l3);
                drawline(trans, bottomchannel, new Point3d(l3.X, l3.Y - thick, 0), new Point3d(r3.X, r3.Y - thick, 0));
            }

        }

        private Line drawline(Transaction trans, BlockTableRecord block, Point3d p1, Point3d p2, int? color = null)
        {
            
            Line line = new Line(p1, p2);

            if (color.HasValue)
            {
                line.ColorIndex = color.Value; 
            }
            
            block.AppendEntity(line);
            trans.AddNewlyCreatedDBObject(line, true);

            // Return the created line
            return line;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // Start dragging if the left mouse button is pressed
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            // If dragging, move the form
            if (dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            // Stop dragging
            dragging = false;
        }
        private void RoundCorners(Control control, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = control.ClientRectangle;

            path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90); // Top left
            path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90); // Top right
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90); // Bottom right
            path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90); // Bottom left
            path.CloseAllFigures();

            control.Region = new Region(path);
        }    
        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90); // Top left
            path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90); // Top right
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90); // Bottom right
            path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90); // Bottom left
            path.CloseAllFigures();
            return path;
        }


    }
}
