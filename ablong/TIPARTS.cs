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

namespace CAD_AUTOMATION
{
    public partial class TIPARTS : Form
    {
        double lx;
        double ly;
        double c;
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
            double thick = 2;
            double foldling1 = 23;
            double foldling2 = 13;
            double releaving_holes_dia = 2;
            double fold = foldling1 + foldling2;
            string layerName = "BENDING LINE";
            string readside = "";
            string locktype = "";

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
                    break;
                }

            }

            if (!found)
            {
                MessageBox.Show($"NO matching data found");
                return;
            }
            

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                BlockTable blockTable = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)db.CurrentSpaceId.GetObject(OpenMode.ForWrite);

                if(readside == "COVER")
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

                    ApplyChamfer(tr, modelSpace, line1, line2, p1, p2, p3, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line3, line4, p3, p4, p5, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line4, line5, p4, p5, p6, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line6, line7, p6, p7, p8, foldling2 - thick);

                    ApplyChamfer(tr, modelSpace, line7, line8, p7, p8, p9, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line9, line10, p9, p10, p11, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line10, line11, p10, p11, p12, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line12, line1, p12, p1, p2, foldling2 - thick);

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
                else if(readside == "DOOR")
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

                    ApplyChamfer(tr, modelSpace, line1, line2, p1, p2, p3, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line3, line4, p3, p4, p5, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line4, line5, p4, p5, p6, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line6, line7, p6, p7, p8, foldling2 - thick);

                    ApplyChamfer(tr, modelSpace, line7, line8, p7, p8, p9, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line9, line10, p9, p10, p11, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line10, line11, p10, p11, p12, foldling2 - thick);
                    ApplyChamfer(tr, modelSpace, line12, line1, p12, p1, p2, foldling2 - thick);

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


                tr.Commit();

                // Check if Excel is already running
                

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
    }
}
