using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using ExcelApplication = Microsoft.Office.Interop.Excel.Application;
using Excel = Microsoft.Office.Interop.Excel;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Runtime.InteropServices;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Arc = Autodesk.AutoCAD.DatabaseServices.Arc;
using Autodesk.AutoCAD.GraphicsInterface;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using System.IO;
using Autodesk.AutoCAD.Colors;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Windows.Documents;
using System.Windows.Media;
using TextBox = System.Windows.Forms.TextBox;

namespace CAD_AUTOMATION
{
    public partial class TIPARTS : Form
    {
        double lx;
        double ly;
        double c;
        private NameValueCollection config;
        public TIPARTS(Point3d descpoint)
        {
            InitializeComponent();
            lx = descpoint.X;
            ly = descpoint.Y;
            c = descpoint.Y;
        }

        private void runbutton_Click(object sender, EventArgs e)
        {
            
            double width = 0;
            double height = 0;
            double panelheight = 0;
            double hbbsize = 0;
            string layerName = "BENDING LINE";
            string readside = "";
            string locktype = "";
            double camlockBvalue = 0;
            double camlockCvalue = 0;
            double hingeDvalue = 0;
            double hingeEvalue = 0;

            if (string.IsNullOrEmpty(readsidecombobox.SelectedItem?.ToString()))
            {
                errorlabel.Visible = true;
                return;
            }
            else
            {
                readside = readsidecombobox.SelectedItem?.ToString();
            }

            if (readsidecombobox.SelectedItem.ToString() == "DOOR" && string.IsNullOrEmpty(locktypecombobox.SelectedItem?.ToString()))
            {
                errorlabel.Visible = true;
                return;
            }
            else
            {
                locktype = locktypecombobox.SelectedItem?.ToString();
            }

            if (!string.IsNullOrEmpty(partcountbox.SelectedItem?.ToString()))
            {
                // Get selected count
                int selectedCount = int.Parse(partcountbox.SelectedItem.ToString());

                // Now check each relevant textbox
                for (int i = 1; i <= selectedCount; i++)
                {
                    TextBox tb = this.Controls["part" + i] as TextBox;
                    if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                    {
                        errorlabel.Visible = true;
                        return;
                    }
                }
            }


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            if (int.TryParse(heightbox.Text,out int test))
            {
                panelheight = test;
            }
            else { return; }



            if (int.TryParse(hbbbox.Text, out int test2))
            {
                hbbsize = test2;
            }
            else { return; }

            if (int.TryParse(widthbox.Text, out int test3))
            {
                width = test3;
            }
            else { return; }


            Excel.Application excelApp = null;
            try
            {
                excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");
            }
            catch (COMException)
            {
                // Excel is not running, show a message and return
                MessageBox.Show("TI DOOR's Excel was not opened");
                return;
            }

            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;
            int matchCount = 0;
            Excel.Worksheet matchedWorksheet = null;
            Excel.Workbook matchedWorkbook = null;

            // Check if any workbooks are open
            if (excelApp.Workbooks.Count == 0)
            {
                MessageBox.Show("NO sheet opened for TI DOOR's");
                return;
            }

            foreach (Excel.Workbook wb in excelApp.Workbooks)
            {
                foreach (Excel.Worksheet ws in wb.Sheets)
                {
                    if (ws.Name == "REAR_DOOR")
                    {
                        matchCount++;
                        matchedWorksheet = ws;
                        matchedWorkbook = wb;

                        if (matchCount > 1)
                        {
                            MessageBox.Show("Error: Multiple sheets match the filename. Please check.");
                            return;
                        }
                    }
                }
            }


            if (matchCount == 1)
            {
                workbook = matchedWorkbook;
                worksheet = matchedWorksheet;
            }
            else if (matchCount == 0)
            {
                MessageBox.Show("No matching sheet found in any open workbook for TI DOOR's.");
                return;
            }

            Excel.Range usedrange = worksheet.UsedRange;
            bool found = false;

            foreach (Excel.Range row in usedrange.Rows)
            { 
                if(row.Row == 1)
                {
                    continue;
                }
                
                string cellValue = row.Cells[1, 7].Value2?.ToString();
                string cellValue2 = row.Cells[1, 8].Value2?.ToString();

                if(cellValue == panelheight.ToString() && cellValue2 == hbbsize.ToString())
                {
                    found = true;
                    height = double.Parse(row.Cells[1, 2].Value2?.ToString());
                    camlockBvalue = double.Parse(row.Cells[1, 3].Value2?.ToString());
                    camlockCvalue = double.Parse(row.Cells[1, 4].Value2?.ToString());
                    hingeDvalue = double.Parse(row.Cells[1, 5].Value2?.ToString());
                    hingeEvalue = double.Parse(row.Cells[1, 6].Value2?.ToString());
                    break;
                }

            }

            if (!found)
            {
                MessageBox.Show($"NO matching data found");
                return;
            }

            config = new System.Collections.Specialized.NameValueCollection();
            string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string cadFilePath = Path.Combine(pluginDirectory, "blocks.dwg");
            string iniFilePath = Path.Combine(pluginDirectory, "gi_config_in.ini");

            if (!File.Exists(cadFilePath))
            {
                MessageBox.Show("\nBlocks DWG file not found.");
                return;
            }

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
            
            double thick = Convert.ToDouble(config["door_thick"]);
            double folding1 = Convert.ToDouble(config["door_firstfolding"]);
            double folding2 = Convert.ToDouble(config["door_secondfolding"]);
            double folding_hingeside = Convert.ToDouble(config["door_hingesidefolding"]);
            double releaving_holes_dia = Convert.ToDouble(config["releaving_holes_radius"]);
            double camlockXvalue = Convert.ToDouble(config["camlock_clearence_x"]);
            double nameplateholes = Convert.ToDouble(config["nameplate_holes_dia"]);
            double fold = folding1 + folding2;

            //ImportBlocksFromDWG(db, cadFilePath);

            using (Database sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(cadFilePath, FileOpenMode.OpenForReadAndReadShare, false, null);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {

                    BlockTable blockTable = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                    BlockTableRecord modelSpace = (BlockTableRecord)db.CurrentSpaceId.GetObject(OpenMode.ForWrite);

                    // Check if "GaMeR" dimension style exists
                    DimStyleTable dimStyleTable = tr.GetObject(db.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;
                    string dimStyleName = "ROHITH";
                    ObjectId dimStyleId;

                    if (!dimStyleTable.Has(dimStyleName))
                    {
                        DimStyleTableRecord newDimStyle = new DimStyleTableRecord
                        {
                            Name = dimStyleName,
                            Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 2),
                            Dimclrt = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 3),
                            Dimclre = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 2),
                            Dimasz = 35,
                            Dimtxt = 45,
                            Dimexo = 4.0,
                            Dimdec = 0,
                            Dimtad = 0,
                            Dimjust = 0,
                            Dimtoh = true,
                            Dimtih = false,
                            Dimupt = false,
                            Dimgap = 5
                        };

                        // Only add to table AFTER setting all properties
                        dimStyleTable.UpgradeOpen();
                        dimStyleId = dimStyleTable.Add(newDimStyle);
                        tr.AddNewlyCreatedDBObject(newDimStyle, true);
                        //db.SetDimstyleData(newDimStyle);
                    }
                    else
                    {
                        
                        dimStyleId = dimStyleTable[dimStyleName];
                    }

                    string dimStyleName2 = "BLANK SIZE";
                    ObjectId dimStyleId2;

                    if (!dimStyleTable.Has(dimStyleName2))
                    {
                        // Create the new dimension style record and set its name
                        DimStyleTableRecord newDimStyle = new DimStyleTableRecord
                        {
                            Name = dimStyleName2,
                            Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 4),
                            Dimclrt = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 1),
                            Dimclre = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 4),
                            Dimasz = 35,
                            Dimtxt = 45,
                            Dimexo = 4.0,
                            Dimdec = 0,
                            Dimtad = 0,
                            Dimjust = 0,
                            Dimtoh = true,
                            Dimtih = false,
                            Dimupt = false,
                            Dimgap = 5
                        };

                        // Add to the dim style table
                        dimStyleTable.UpgradeOpen(); // Upgrade BEFORE adding
                        dimStyleId2 = dimStyleTable.Add(newDimStyle);
                        tr.AddNewlyCreatedDBObject(newDimStyle, true);
                        //db.SetDimstyleData(newDimStyle);
                    }
                    else
                    {
                        dimStyleId2 = dimStyleTable[dimStyleName2];
                    }

                    // Set the new dimension style as the current one
                    //db.Dimstyle = dimStyleId;

                    if (readside == "COVER")
                    {
                        double fold1 = fold - thick;
                        double off = thick * 2;
                        //double wid = width + fold1 + lx;
                        //double hei = height + fold1;

                        Point3d p1 = new Point3d(lx + fold1, ly, 0);
                        Point3d p2 = new Point3d(p1.X + width - off, ly, 0);
                        Point3d p3 = new Point3d(p2.X, fold1 + ly, 0);
                        Point3d p4 = new Point3d(p2.X + fold1, p3.Y, 0);
                        Point3d p5 = new Point3d(p4.X, height + fold1 - off + ly, 0);
                        Point3d p6 = new Point3d(p2.X, p5.Y, 0);
                        Point3d p7 = new Point3d(p2.X, p5.Y + fold1, 0);
                        Point3d p8 = new Point3d(p1.X, p7.Y, 0);
                        Point3d p9 = new Point3d(p1.X, p5.Y, 0);
                        Point3d p10 = new Point3d(lx, p5.Y, 0);
                        Point3d p11 = new Point3d(lx, p3.Y, 0);
                        Point3d p12 = new Point3d(p1.X, p3.Y, 0);

                        Line line1 = new Line(p1, p2);
                        Line line2 = new Line(p2, p3);
                        Line line3 = new Line(p3, p4);
                        Line line4 = new Line(p4, p5);
                        Line line5 = new Line(p5, p6);
                        Line line6 = new Line(p6, p7);
                        Line line7 = new Line(p7, p8);
                        Line line8 = new Line(p8, p9);
                        Line line9 = new Line(p9, p10);
                        Line line10 = new Line(p10, p11);
                        Line line11 = new Line(p11, p12);
                        Line line12 = new Line(p12, p1);

                        ApplyChamfer(tr, modelSpace, line1, line2, p1, p2, p3, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line3, line4, p3, p4, p5, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line4, line5, p4, p5, p6, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line6, line7, p6, p7, p8, folding2 - thick);

                        ApplyChamfer(tr, modelSpace, line7, line8, p7, p8, p9, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line9, line10, p9, p10, p11, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line10, line11, p10, p11, p12, folding2 - thick);
                        ApplyChamfer(tr, modelSpace, line12, line1, p12, p1, p2, folding2 - thick);

                        Applyholes(tr, modelSpace, line2, line3, p2, p3, p4, releaving_holes_dia, "br");
                        Applyholes(tr, modelSpace, line5, line6, p5, p6, p7, releaving_holes_dia, "tr");
                        Applyholes(tr, modelSpace, line8, line9, p8, p9, p10, releaving_holes_dia, "tl");
                        Applyholes(tr, modelSpace, line11, line12, p11, p12, p1, releaving_holes_dia, "bl");

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                        if (!ltTable.Has("HIDDEN"))
                        {
                            db.LoadLineTypeFile("HIDDEN", "acad.lin");
                        }

                        LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                        if (!lt.Has(layerName))
                        {
                            // Open for write to add a new layer
                            lt.UpgradeOpen();

                            // Create new layer
                            LayerTableRecord ltr = new LayerTableRecord
                            {
                                Name = layerName,
                                Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4),
                                LinetypeObjectId = ltTable["HIDDEN"] // Default to "Continuous"
                            };

                            lt.Add(ltr);
                            tr.AddNewlyCreatedDBObject(ltr, true);
                        }

                        Line line13 = new Line(p12, p3);
                        modelSpace.AppendEntity(line13);
                        tr.AddNewlyCreatedDBObject(line13, true);
                        //line13.ColorIndex = 4;
                        //line13.Linetype = "HIDDEN";
                        line13.Layer = layerName;

                        Line line14 = new Line(p3, p6);
                        modelSpace.AppendEntity(line14);
                        tr.AddNewlyCreatedDBObject(line14, true);
                        //line14.ColorIndex = 4;
                        line14.Layer = layerName;

                        Line line15 = new Line(p6, p9);
                        modelSpace.AppendEntity(line15);
                        tr.AddNewlyCreatedDBObject(line15, true);
                        //line15.ColorIndex = 4;
                        line15.Layer = layerName;

                        Line line16 = new Line(p9, p12);
                        modelSpace.AppendEntity(line16);
                        tr.AddNewlyCreatedDBObject(line16, true);
                        //line16.ColorIndex = 4;
                        line16.Layer = layerName;

                        Line line17 = new Line(line2.StartPoint, line12.EndPoint);
                        modelSpace.AppendEntity(line17);
                        tr.AddNewlyCreatedDBObject(line17, true);
                        //line17.ColorIndex = 4;
                        line17.Layer = layerName;

                        Line line18 = new Line(line3.EndPoint, line5.StartPoint);
                        modelSpace.AppendEntity(line18);
                        tr.AddNewlyCreatedDBObject(line18, true);
                        //line18.ColorIndex = 4;
                        line18.Layer = layerName;

                        Line line19 = new Line(line6.EndPoint, line8.StartPoint);
                        modelSpace.AppendEntity(line19);
                        tr.AddNewlyCreatedDBObject(line19, true);
                        //line19.ColorIndex = 4;
                        line19.Layer = layerName;

                        Line line20 = new Line(line9.EndPoint, line11.StartPoint);
                        modelSpace.AppendEntity(line20);
                        tr.AddNewlyCreatedDBObject(line20, true);
                        //line20.ColorIndex = 4;
                        line20.Layer = layerName;

                        // Add lines to model space
                        modelSpace.AppendEntity(line1);
                        modelSpace.AppendEntity(line2);
                        modelSpace.AppendEntity(line3);
                        modelSpace.AppendEntity(line4);
                        modelSpace.AppendEntity(line5);
                        modelSpace.AppendEntity(line6);
                        modelSpace.AppendEntity(line7);
                        modelSpace.AppendEntity(line8);
                        modelSpace.AppendEntity(line9);
                        modelSpace.AppendEntity(line10);
                        modelSpace.AppendEntity(line11);
                        modelSpace.AppendEntity(line12);


                        // Commit the transaction
                        tr.AddNewlyCreatedDBObject(line1, true);
                        tr.AddNewlyCreatedDBObject(line2, true);
                        tr.AddNewlyCreatedDBObject(line3, true);
                        tr.AddNewlyCreatedDBObject(line4, true);
                        tr.AddNewlyCreatedDBObject(line5, true);
                        tr.AddNewlyCreatedDBObject(line6, true);
                        tr.AddNewlyCreatedDBObject(line7, true);
                        tr.AddNewlyCreatedDBObject(line8, true);
                        tr.AddNewlyCreatedDBObject(line9, true);
                        tr.AddNewlyCreatedDBObject(line10, true);
                        tr.AddNewlyCreatedDBObject(line11, true);
                        tr.AddNewlyCreatedDBObject(line12, true);
                    }
                    else if (readside == "DOOR")
                    {
                        double fold1 = fold - thick;
                        double fold2 = height - folding2 * 2 - thick * 2;
                        double fold3 = folding_hingeside - thick;
                        double off = thick * 2;
                        //double wid = width + fold1 + lx;
                        //double hei = height + fold1;

                        Point3d p1 = new Point3d(lx + fold1, ly, 0);
                        Point3d p2 = new Point3d(p1.X + width - off, ly, 0);
                        Point3d p3 = new Point3d(p2.X, fold1 - off + ly, 0);
                        Point3d p4 = new Point3d(p2.X + folding1 - off, p3.Y, 0);
                        Point3d p5 = new Point3d(p4.X, folding2 + p4.Y, 0);
                        Point3d p6 = new Point3d(p4.X + folding2 - thick, p5.Y, 0);
                        Point3d p7 = new Point3d(p6.X, p6.Y + fold2, 0);
                        Point3d p8 = new Point3d(p4.X, p7.Y, 0);
                        Point3d p9 = new Point3d(p4.X, p8.Y + folding2, 0);
                        Point3d p10 = new Point3d(p2.X, p9.Y, 0);
                        Point3d p11 = new Point3d(p10.X, p10.Y + fold1 - off, 0);
                        Point3d p12 = new Point3d(p1.X, p11.Y, 0);
                        Point3d p13 = new Point3d(p1.X, p9.Y, 0);
                        Point3d p14 = new Point3d(p1.X - folding1 + (thick * 2), p13.Y, 0);
                        Point3d p15 = new Point3d(p14.X, p8.Y, 0);
                        Point3d p16 = new Point3d(p15.X - folding_hingeside + thick, p15.Y, 0);
                        Point3d p17 = new Point3d(p16.X, p6.Y, 0);
                        Point3d p18 = new Point3d(p15.X, p6.Y, 0);
                        Point3d p19 = new Point3d(p15.X, p4.Y, 0);
                        Point3d p20 = new Point3d(p13.X, p4.Y, 0);

                        Line line1 = new Line(p1, p2);
                        Line line2 = new Line(p2, p3);
                        Line line3 = new Line(p3, p4);
                        Line line4 = new Line(p4, p5);
                        Line line5 = new Line(p5, p6);
                        Line line6 = new Line(p6, p7);
                        Line line7 = new Line(p7, p8);
                        Line line8 = new Line(p8, p9);
                        Line line9 = new Line(p9, p10);
                        Line line10 = new Line(p10, p11);
                        Line line11 = new Line(p11, p12);
                        Line line12 = new Line(p12, p13);
                        Line line13 = new Line(p13, p14);
                        Line line14 = new Line(p14, p15);
                        Line line15 = new Line(p15, p16);
                        Line line16 = new Line(p16, p17);
                        Line line17 = new Line(p17, p18);
                        Line line18 = new Line(p18, p19);
                        Line line19 = new Line(p19, p20);
                        Line line20 = new Line(p20, p1);


                        Applyholes(tr, modelSpace, line2, line3, p2, p3, p4, releaving_holes_dia, "br");
                        Applyholes(tr, modelSpace, line9, line10, p9, p10, p11, releaving_holes_dia, "tr");
                        Applyholes(tr, modelSpace, line12, line13, p12, p13, p14, releaving_holes_dia, "tl");
                        Applyholes(tr, modelSpace, line19, line20, p19, p20, p1, releaving_holes_dia, "bl");

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                        if (!ltTable.Has("HIDDEN"))
                        {
                            db.LoadLineTypeFile("HIDDEN", "acad.lin");
                        }

                        LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                        if (!lt.Has(layerName))
                        {
                            // Open for write to add a new layer
                            lt.UpgradeOpen();

                            // Create new layer
                            LayerTableRecord ltr = new LayerTableRecord
                            {
                                Name = layerName,
                                Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4),
                                LinetypeObjectId = ltTable["HIDDEN"] // Default to "Continuous"
                            };

                            lt.Add(ltr);
                            tr.AddNewlyCreatedDBObject(ltr, true);
                        }

                        Polyline rectangle = new Polyline(4);
                        rectangle.AddVertexAt(0, new Point2d(p20.X, p20.Y), 0, 0, 0);
                        rectangle.AddVertexAt(1, new Point2d(p3.X, p3.Y), 0, 0, 0);
                        rectangle.AddVertexAt(2, new Point2d(p10.X, p10.Y), 0, 0, 0);
                        rectangle.AddVertexAt(3, new Point2d(p13.X, p13.Y), 0, 0, 0);
                        rectangle.Closed = true;
                        rectangle.Layer = layerName;

                        modelSpace.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);

                        Line line21 = new Line(p5, p8);
                        modelSpace.AppendEntity(line21);
                        tr.AddNewlyCreatedDBObject(line21, true);
                        //line13.ColorIndex = 4;
                        //line13.Linetype = "HIDDEN";
                        line21.Layer = layerName;

                        Line line22 = new Line(p15, p18);
                        modelSpace.AppendEntity(line22);
                        tr.AddNewlyCreatedDBObject(line22, true);
                        //line14.ColorIndex = 4;
                        line22.Layer = layerName;

                        Line line23 = new Line(new Point3d(p10.X, p10.Y + folding1 - (thick * 2), 0), new Point3d(p13.X, p13.Y + folding1 - (thick * 2), 0));
                        modelSpace.AppendEntity(line23);
                        tr.AddNewlyCreatedDBObject(line23, true);
                        //line15.ColorIndex = 4;
                        line23.Layer = layerName;

                        Line line24 = new Line(new Point3d(p20.X, p20.Y - folding1 + (thick * 2), 0), new Point3d(p3.X, p3.Y - folding1 + (thick * 2), 0));
                        modelSpace.AppendEntity(line24);
                        tr.AddNewlyCreatedDBObject(line24, true);
                        //line16.ColorIndex = 4;
                        line24.Layer = layerName;

                        // Add lines to model space
                        modelSpace.AppendEntity(line1);
                        modelSpace.AppendEntity(line2);
                        modelSpace.AppendEntity(line3);
                        modelSpace.AppendEntity(line4);
                        modelSpace.AppendEntity(line5);
                        modelSpace.AppendEntity(line6);
                        modelSpace.AppendEntity(line7);
                        modelSpace.AppendEntity(line8);
                        modelSpace.AppendEntity(line9);
                        modelSpace.AppendEntity(line10);
                        modelSpace.AppendEntity(line11);
                        modelSpace.AppendEntity(line12);
                        modelSpace.AppendEntity(line13);
                        modelSpace.AppendEntity(line14);
                        modelSpace.AppendEntity(line15);
                        modelSpace.AppendEntity(line16);
                        modelSpace.AppendEntity(line17);
                        modelSpace.AppendEntity(line18);
                        modelSpace.AppendEntity(line19);
                        modelSpace.AppendEntity(line20);

                        // Commit the transaction
                        tr.AddNewlyCreatedDBObject(line1, true);
                        tr.AddNewlyCreatedDBObject(line2, true);
                        tr.AddNewlyCreatedDBObject(line3, true);
                        tr.AddNewlyCreatedDBObject(line4, true);
                        tr.AddNewlyCreatedDBObject(line5, true);
                        tr.AddNewlyCreatedDBObject(line6, true);
                        tr.AddNewlyCreatedDBObject(line7, true);
                        tr.AddNewlyCreatedDBObject(line8, true);
                        tr.AddNewlyCreatedDBObject(line9, true);
                        tr.AddNewlyCreatedDBObject(line10, true);
                        tr.AddNewlyCreatedDBObject(line11, true);
                        tr.AddNewlyCreatedDBObject(line12, true);
                        tr.AddNewlyCreatedDBObject(line13, true);
                        tr.AddNewlyCreatedDBObject(line14, true);
                        tr.AddNewlyCreatedDBObject(line15, true);
                        tr.AddNewlyCreatedDBObject(line16, true);
                        tr.AddNewlyCreatedDBObject(line17, true);
                        tr.AddNewlyCreatedDBObject(line18, true);
                        tr.AddNewlyCreatedDBObject(line19, true);
                        tr.AddNewlyCreatedDBObject(line20, true);

                        if(locktype == "CAM_LOCK_&_GRIP")
                        {
                            if(camlockBvalue > 0 && camlockCvalue > 0)
                            {
                                Point3d pcamlock = new Point3d(p10.X - camlockXvalue + thick, p10.Y - 113, 0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK_GRIP", pcamlock, 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK_GRIP", new Point3d(pcamlock.X,pcamlock.Y - camlockBvalue,0), 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK_GRIP", new Point3d(pcamlock.X, pcamlock.Y - camlockBvalue - camlockCvalue, 0), 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK_GRIP", new Point3d(pcamlock.X, pcamlock.Y - (camlockBvalue*2) - camlockCvalue, 0), 1.0);
                            }
                            else
                            {
                                MessageBox.Show("camlock parameter was not found");
                            }
                            
                        }
                        else if(locktype == "CAM_LOCK")
                        {
                            if (camlockBvalue > 0 && camlockCvalue > 0)
                            {
                                Point3d pcamlock = new Point3d(p10.X - camlockXvalue + thick, p10.Y - 113, 0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK", pcamlock, 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK", new Point3d(pcamlock.X, pcamlock.Y - camlockBvalue, 0), 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK", new Point3d(pcamlock.X, pcamlock.Y - camlockBvalue - camlockCvalue, 0), 1.0);
                                InsertBlock(db, sourceDb, tr, modelSpace, "CAMLOCK", new Point3d(pcamlock.X, pcamlock.Y - (camlockBvalue * 2) - camlockCvalue, 0), 1.0);
                            }
                            else
                            {
                                MessageBox.Show("camlock parameter was not found");
                            }
                        }
                        else if(locktype == "KNOB")
                        {

                        }

                        Circle circle = new Circle
                        {
                            Center = new Point3d(((p20.X + p3.X) / 2 )-35, (p3.Y+p10.Y)/2,0), // Set the center point
                            Radius = nameplateholes/2  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle);
                        tr.AddNewlyCreatedDBObject(circle, true);

                        Circle circle2 = new Circle
                        {
                            Center = new Point3d(((p20.X + p3.X) / 2) + 35, (p3.Y + p10.Y) / 2, 0), // Set the center point
                            Radius = nameplateholes / 2  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle2);
                        tr.AddNewlyCreatedDBObject(circle2, true);

                        Circle circle3 = new Circle
                        {
                            Center = new Point3d(p16.X + 4, p14.Y - 56.5, 0), // Set the center point
                            Radius = 2.1  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle3);
                        tr.AddNewlyCreatedDBObject(circle3, true);

                        Circle circle4 = new Circle
                        {
                            Center = new Point3d(p16.X + 4, p14.Y - 56.5 - 13, 0), // Set the center point
                            Radius = 2.1  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle4);
                        tr.AddNewlyCreatedDBObject(circle4, true);

                        // Vector to move down 300mm (Y-axis)
                        Vector3d moveVector = new Vector3d(0, - hingeDvalue - 13, 0);

                        // Clone and move circle3
                        Circle circle3Copy = (Circle)circle3.Clone();
                        circle3Copy.TransformBy(Matrix3d.Displacement(moveVector));
                        modelSpace.AppendEntity(circle3Copy);
                        tr.AddNewlyCreatedDBObject(circle3Copy, true);

                        // Clone and move circle4
                        Circle circle4Copy = (Circle)circle4.Clone();
                        circle4Copy.TransformBy(Matrix3d.Displacement(moveVector));
                        modelSpace.AppendEntity(circle4Copy);
                        tr.AddNewlyCreatedDBObject(circle4Copy, true);

                        // Vector to move down 300mm (Y-axis)
                        Vector3d moveVector2 = new Vector3d(0, -hingeEvalue - 13, 0);

                        // Clone and move circle3
                        Circle circle5Copy = (Circle)circle3Copy.Clone();
                        circle5Copy.TransformBy(Matrix3d.Displacement(moveVector2));
                        modelSpace.AppendEntity(circle5Copy);
                        tr.AddNewlyCreatedDBObject(circle5Copy, true);

                        // Clone and move circle4
                        Circle circle6Copy = (Circle)circle4Copy.Clone();
                        circle6Copy.TransformBy(Matrix3d.Displacement(moveVector2));
                        modelSpace.AppendEntity(circle6Copy);
                        tr.AddNewlyCreatedDBObject(circle6Copy, true);

                        Circle circle7 = new Circle
                        {
                            Center = new Point3d(p16.X + 4, p19.Y + 61.5, 0), // Set the center point
                            Radius = 2.1  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle7);
                        tr.AddNewlyCreatedDBObject(circle7, true);

                        Circle circle8 = new Circle
                        {
                            Center = new Point3d(p16.X + 4, p19.Y + 61.5 + 13, 0), // Set the center point
                            Radius = 2.1  // Set the radius
                        };
                        // Add the circle to the block table record and the transaction
                        modelSpace.AppendEntity(circle8);
                        tr.AddNewlyCreatedDBObject(circle8, true);

                        Point3d dd1 = new Point3d(p11.X, p11.Y + 45, 0);
                        AlignedDimension dim1 = new AlignedDimension(p11, p12, dd1, "", ObjectId.Null);
                        dim1.DimensionStyle = dimStyleId;
                        modelSpace.AppendEntity(dim1);
                        tr.AddNewlyCreatedDBObject(dim1, true);

                        Point3d dd2 = new Point3d(p4.X + 95, p4.Y, 0);
                        AlignedDimension dim2 = new AlignedDimension(p4, p9, dd2, "", ObjectId.Null);
                        dim2.DimensionStyle = dimStyleId;
                        modelSpace.AppendEntity(dim2);
                        tr.AddNewlyCreatedDBObject(dim2, true);

                        //db.Dimstyle = dimStyleId2;

                        Point3d dd3 = new Point3d(p11.X, p11.Y + 105, 0);
                        AlignedDimension dim3 = new AlignedDimension(p16, p7, dd3, "", ObjectId.Null);
                        dim3.DimensionStyle = dimStyleId2;
                        modelSpace.AppendEntity(dim3);
                        tr.AddNewlyCreatedDBObject(dim3, true);

                        Point3d dd4 = new Point3d(p4.X + 140, p4.Y, 0);
                        AlignedDimension dim4 = new AlignedDimension(p2, p11, dd4, "", ObjectId.Null);
                        dim4.DimensionStyle = dimStyleId2;
                        modelSpace.AppendEntity(dim4);
                        tr.AddNewlyCreatedDBObject(dim4, true);

                    }


                    tr.Commit();

                    // Check if Excel is already running


                }
            }


            



            this.Close();
        }
        private void ApplyChamfer(Transaction tr, BlockTableRecord modelSpace,
                          Line line1, Line line2,
                          Point3d p1, Point3d p2, Point3d p3,
                          double distance)
        {
            // Direction vectors from corner to ends
            Vector3d v1 = (p1 - p2).GetNormal();
            Vector3d v2 = (p3 - p2).GetNormal();

            // Chamfer endpoints
            Point3d chamferPt1 = p2 + v1 * distance;
            Point3d chamferPt2 = p2 + v2 * distance;

            // Trim line1
            if (line1.StartPoint.IsEqualTo(p2))
                line1.StartPoint = chamferPt1;
            else if (line1.EndPoint.IsEqualTo(p2))
                line1.EndPoint = chamferPt1;

            // Trim line2
            if (line2.StartPoint.IsEqualTo(p2))
                line2.StartPoint = chamferPt2;
            else if (line2.EndPoint.IsEqualTo(p2))
                line2.EndPoint = chamferPt2;

            // Add chamfer line
            Line chamferLine = new Line(chamferPt1, chamferPt2);
            modelSpace.AppendEntity(chamferLine);
            tr.AddNewlyCreatedDBObject(chamferLine, true);
        }

        private void Applyholes(Transaction tr, BlockTableRecord modelSpace,
                          Line line1, Line line2,
                          Point3d p1, Point3d p2, Point3d p3,
                          double dia, string pos)
        {
            // Direction vectors from corner to ends
            Vector3d v1 = (p1 - p2).GetNormal();
            Vector3d v2 = (p3 - p2).GetNormal();

            // Chamfer endpoints
            Point3d chamferPt1 = p2 + v1 * dia;
            Point3d chamferPt2 = p2 + v2 * dia;

            // Trim line1
            if (line1.StartPoint.IsEqualTo(p2))
                line1.StartPoint = chamferPt1;
            else if (line1.EndPoint.IsEqualTo(p2))
                line1.EndPoint = chamferPt1;

            // Trim line2
            if (line2.StartPoint.IsEqualTo(p2))
                line2.StartPoint = chamferPt2;
            else if (line2.EndPoint.IsEqualTo(p2))
                line2.EndPoint = chamferPt2;

            if(pos == "br")
            {
                Arc arc1 = new Arc(p2, dia , 0 * Math.PI, 1.5 * Math.PI);
                modelSpace.AppendEntity(arc1);
                tr.AddNewlyCreatedDBObject(arc1, true);
            }
            else if (pos == "tr")
            {
                Arc arc1 = new Arc(p2, dia, 0.5 * Math.PI, 0 * Math.PI);
                modelSpace.AppendEntity(arc1);
                tr.AddNewlyCreatedDBObject(arc1, true);
            }
            else if (pos == "tl")
            {
                Arc arc1 = new Arc(p2, dia, 1 * Math.PI, 0.5 * Math.PI);
                modelSpace.AppendEntity(arc1);
                tr.AddNewlyCreatedDBObject(arc1, true);
            }
            else if (pos == "bl")
            {
                Arc arc1 = new Arc(p2, dia, 1.5 * Math.PI, 1 * Math.PI);
                modelSpace.AppendEntity(arc1);
                tr.AddNewlyCreatedDBObject(arc1, true);
            }



        }

        private BlockReference InsertBlock(Database targetDb, Database sourceDb, Transaction transaction, BlockTableRecord blockTableRecord, string blockName, Point3d position, double scaleFactor)
        {
            BlockTable blockTable = transaction.GetObject(blockTableRecord.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

            if (!blockTable.Has(blockName))
            {
                using (Transaction trans = sourceDb.TransactionManager.StartTransaction())
                {
                    BlockTable sourceBlockTable = trans.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    if (!sourceBlockTable.Has(blockName))
                    {
                        MessageBox.Show($"\nBlock '{blockName}' not found in blocks.dwg.");
                        return null;  // Return null if block not found
                    }

                    ObjectId blockId = sourceBlockTable[blockName];

                    IdMapping idMap = new IdMapping();
                    ObjectIdCollection blockIds = new ObjectIdCollection { blockId };
                    sourceDb.WblockCloneObjects(blockIds, targetDb.BlockTableId, idMap, DuplicateRecordCloning.Replace, false);
                }
            }

            BlockTableRecord blockDef = transaction.GetObject(blockTable[blockName], OpenMode.ForRead) as BlockTableRecord;

            BlockReference blockRef = new BlockReference(position, blockDef.Id)
            {
                ScaleFactors = new Scale3d(scaleFactor)
            };

            blockTableRecord.AppendEntity(blockRef);
            transaction.AddNewlyCreatedDBObject(blockRef, true);

            return blockRef; // ✅ Return the inserted block reference
        }

        private void readsidecombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (readsidecombobox.SelectedItem.ToString() == "DOOR")
            {
                locktypecombobox.Visible = true;
                metroLabel5.Visible = true;  
            }
            else
            {
                locktypecombobox.Visible = false;
                metroLabel5.Visible = false;
            }
        }

        private void heightbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Suppress the key press
            }
        }

        private void hbbbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Suppress the key press
            }
        }

        private void widthbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Suppress the key press
            }
        }

        private void partcountbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get selected number from ComboBox
            if (int.TryParse(partcountbox.SelectedItem.ToString(), out int selectedCount))
            {
                // Loop through all 6
                for (int i = 1; i <= 6; i++)
                {
                    // Find label and textbox controls by name
                    Control label = this.Controls[$"part{i}label"];
                    Control textbox = this.Controls["part" + i];

                    if (label != null && textbox != null)
                    {
                        // Show/Hide based on selected count
                        bool isVisible = i <= selectedCount;
                        label.Visible = isVisible;
                        textbox.Visible = isVisible;
                    }
                }
            }
        }

    }
}
