using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using static Autodesk.AutoCAD.LayerManager.LayerFilter;
using Exception = System.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Arc = Autodesk.AutoCAD.DatabaseServices.Arc;
using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using ExcelApplication = Microsoft.Office.Interop.Excel.Application;
using Excel = Microsoft.Office.Interop.Excel;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.GraphicsSystem;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CAD_AUTOMATION
{
    public class RectangleDrawer
    {
        private static string lastFolderName = string.Empty;
        private static string lastFileName = string.Empty;
        private static double lastoblen;
        private static double lastobwid;

        [CommandMethod("oblong")]
        public void DrawRectangle()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Block Table for read
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block Table Record Model space for write
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Get the user input for length and width
                PromptDoubleOptions lengthOptions = new PromptDoubleOptions("\nEnter the length of the rectangle: ");
                lengthOptions.DefaultValue = lastoblen;
                PromptDoubleResult lengthResult = doc.Editor.GetDouble(lengthOptions);

                if (lengthResult.Status != PromptStatus.OK)
                    return;
                lastoblen = lengthResult.Value;
                PromptDoubleOptions widthOptions = new PromptDoubleOptions("\nEnter the width of the rectangle: ");
                widthOptions.DefaultValue = lastobwid;
                PromptDoubleResult widthResult = doc.Editor.GetDouble(widthOptions);

                if (widthResult.Status != PromptStatus.OK)
                    return;
                lastobwid = widthResult.Value;

                double length = lengthResult.Value;
                double width = widthResult.Value;

                // Get the user input for the center point
                PromptPointResult centerResult = doc.Editor.GetPoint("\nSpecify the center point: ");

                if (centerResult.Status != PromptStatus.OK)
                    return;

                Point3d centerPoint = centerResult.Value;

                // Calculate the top left and bottom right corners of the rectangle
                double halfLength = length / 2.0;
                double halfWidth = width / 2.0;
                double offset = halfWidth;

                Point3d topLeft = new Point3d(centerPoint.X - halfLength + offset, centerPoint.Y + halfWidth, centerPoint.Z);
                Point3d bottomRight = new Point3d(centerPoint.X + halfLength - offset, centerPoint.Y - halfWidth, centerPoint.Z);

                // Create the rectangle
                Line line1 = new Line(topLeft, new Point3d(bottomRight.X, topLeft.Y, bottomRight.Z));
                //Line line2 = new Line(new Point3d(bottomRight.X, topLeft.Y, bottomRight.Z), bottomRight);
                Line line3 = new Line(bottomRight, new Point3d(topLeft.X, bottomRight.Y, topLeft.Z));
                //Line line4 = new Line(new Point3d(topLeft.X, bottomRight.Y, topLeft.Z), topLeft);

                Point3d arcright = new Point3d(bottomRight.X, centerPoint.Y, centerPoint.Z);
                Point3d arcleft = new Point3d(topLeft.X, centerPoint.Y, centerPoint.Z);

                Arc arc1 = new Arc(arcright, offset, 1.5 * Math.PI, 0.5 * Math.PI);
                Arc arc2 = new Arc(arcleft, offset, 0.5 * Math.PI, 1.5 * Math.PI);

                // Add the lines to the drawing
                btr.AppendEntity(line1);
                btr.AppendEntity(arc1);
                btr.AppendEntity(line3);
                btr.AppendEntity(arc2);

                tr.AddNewlyCreatedDBObject(line1, true);
                tr.AddNewlyCreatedDBObject(arc1, true);
                tr.AddNewlyCreatedDBObject(line3, true);
                tr.AddNewlyCreatedDBObject(arc2, true);

                // Commit the transaction
                tr.Commit();

                // Display a message
                doc.Editor.WriteMessage("\nRectangle created successfully.");
            }
        }

        [CommandMethod("enterpartnumbers")]
        public static void NumberPartNumbers()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Block Table for read
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block Table Record Model space for write
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Initialize a counter for the DBText with "PART NUMBER -"
                int partNumberCount = 0;

                // Loop through all entities in the model space
                foreach (ObjectId objId in btr)
                {
                    // Attempt to open the object as DBText
                    DBText dbText = tr.GetObject(objId, OpenMode.ForWrite) as DBText;

                    if (dbText != null)
                    {
                        // Check if the DBText contains the specified text
                        if (dbText.TextString.Contains("PART NUMBER -"))
                        {
                            partNumberCount++;
                            // Format the new text with a three-digit number
                            string newText = $"PART NUMBER - {partNumberCount:D3}";
                            dbText.TextString = newText;
                        }
                    }
                }

                // Display a message box with the total number of changes
                Application.ShowAlertDialog($"Number of 'PART NUMBER -' entries numbered: {partNumberCount}");

                // Commit the transaction to save changes
                tr.Commit();
            }
        }

        [CommandMethod("MECHBOM")]
        public static void BOMcount()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Check if Excel is already running
            Excel.Application excelApp = null;
            try
            {
                excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");
                ed.WriteMessage("\nAutomation By GaMeR.");
            }
            catch (COMException)
            {
                // Excel is not running, show a message and return
                ed.WriteMessage("\nExcel is not running.");
                return;
            }

            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                if (excelApp.Workbooks.Count == 0)
                {
                    ed.WriteMessage("\nNo workbooks are open.");
                    return;
                }

                // Check if the workbook contains a sheet named "CAD"
                foreach (Excel.Workbook wb in excelApp.Workbooks)
                {
                    worksheet = null;

                    foreach (Excel.Worksheet ws in wb.Sheets)
                    {
                        if (ws.Name.ToLower() == "kg - sqft")
                        {
                            worksheet = ws;
                            break;
                        }
                    }

                    if (worksheet != null)
                    {
                        workbook = wb;
                        break;
                    }
                }

                // If the "CAD" sheet is not found
                if (worksheet == null)
                {
                    ed.WriteMessage("\nWorksheet named 'KG - SQFT' not found.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Open the Block Table for read
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block Table Record for Model Space
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    // Set the starting row in the worksheet (C4 = row 4, column 3)
                    int row = 4;
                    int partcount = 1;
                    int layercount = 0;
                    int partnumbercount = int.MinValue;

                    foreach (ObjectId objId in btr)
                    {
                        // Get the entity and check if it's a polyline (representing a rectangle)
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent != null && ent is Polyline)
                        {
                            Polyline poly = ent as Polyline;

                            // Check if the polyline has 4 vertices (closed rectangle) and is on the "PARTS" layer
                            if (poly.NumberOfVertices == 4 && poly.Closed && poly.Layer == "PARTS")
                            {
                                layercount++;
                            }
                        }
                    }

                    foreach (ObjectId objId in btr)
                    {
                        
                        DBText dbText = tr.GetObject(objId, OpenMode.ForRead) as DBText;

                        if (dbText != null)
                        {
                            // Check if the DBText contains the specified text
                            if (dbText.TextString.Contains("PART NUMBER -"))
                            {
                                string[] parts = dbText.TextString.Split(new[] { "PART NUMBER -" }, StringSplitOptions.None);
                                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int partNumber))
                                {
                                    // Compare and store the highest part number
                                    if (partNumber > partnumbercount)
                                    {
                                        partnumbercount = partNumber;
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 1; i <= partnumbercount; i++)
                    {
                        // Example: Format the part number as needed, here appending it to excelpartnumber
                        string excelpartnumber = $"PART NUMBER - {i.ToString("D3")}";
                        worksheet.Cells[row, 1].Value = partcount;
                        worksheet.Cells[row, 2].Value = excelpartnumber;
                        row++;
                        partcount++;
                    }

                    if(layercount != partnumbercount)
                    {
                        MessageBox.Show("Some Parts are not in PARTS layer");
                    }
                    Excel.Range usedrange = worksheet.UsedRange;

                    foreach (ObjectId objId in btr)
                    {
                        // Get the entity and check if it's a polyline (representing a rectangle)
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent != null && ent is Polyline)
                        {
                            Polyline poly = ent as Polyline;

                            // Check if the polyline has 4 vertices (closed rectangle) and is on the "PARTS" layer
                            if (poly.NumberOfVertices == 4 && poly.Closed && poly.Layer == "PARTS")
                            {
                                // Get the rectangle bounds
                                Extents3d polyBounds = poly.GeometricExtents;

                                string partnumber = "";
                                string description = "";
                                string material = "";
                                int quantity = 0;
                                double thickness = 0;
                                double dimension1 = 0;
                                double dimension2 = 0;
                                

                                foreach (ObjectId innerObjId in btr)
                                {
                                    Entity innerEnt = tr.GetObject(innerObjId, OpenMode.ForRead) as Entity;

                                    // Check if the entity is a DBText (single-line text)
                                    if (innerEnt != null && innerEnt is DBText)
                                    {
                                        DBText dbText = innerEnt as DBText;
                                        Point3d textPosition = dbText.Position;

                                        // Check if the text is inside the rectangle's bounds
                                        if (textPosition.X >= polyBounds.MinPoint.X && textPosition.X <= polyBounds.MaxPoint.X &&
                                            textPosition.Y >= polyBounds.MinPoint.Y && textPosition.Y <= polyBounds.MaxPoint.Y)
                                        {
                                            string textValue = dbText.TextString;

                                            
                                            if (textValue.Contains("PART NUMBER - "))
                                            {
                                                partnumber = textValue;
                                            }

                                            if (textValue.Contains("DESCRIPTION - "))
                                            {
                                                description = textValue.Substring(textValue.IndexOf("DESCRIPTION - ") + "DESCRIPTION - ".Length).Trim();
                                            }
                                            
                                            if (textValue.Contains("MATERIAL - "))
                                            {
                                                material = textValue.Substring(textValue.IndexOf("MATERIAL - ") + "MATERIAL - ".Length).Trim();
                                            }
                                            
                                            if (textValue.Contains("QTY - "))
                                            {
                                                string qtyPart = textValue.Substring(textValue.IndexOf("QTY - ") + "QTY - ".Length).Trim();
                                                qtyPart = qtyPart.Split(' ')[0]; 

                                                if (int.TryParse(qtyPart, out int qty))
                                                {
                                                    quantity = qty;
                                                }
                                            }
                                           
                                            if (textValue.Contains("THICK - "))
                                            {
                                                string thickPart = textValue.Substring(textValue.IndexOf("THICK - ") + "THICK - ".Length).Trim();
                                                thickPart = thickPart.Split(' ')[0]; // Get the numeric part before any spaces

                                                if (double.TryParse(thickPart, out double thick))
                                                {
                                                    thickness = thick;
                                                }
                                            }
                                        }
                                    }

                                    // Check if the entity is a Dimension object (for dimension lines)
                                    if (innerEnt is Dimension)
                                    {
                                        Dimension dimension = innerEnt as Dimension;

                                        // Get the geometric extents of the dimension
                                        Extents3d dimBounds = dimension.GeometricExtents;

                                        // Check if the dimension is inside the rectangle's bounds
                                        if (dimBounds.MinPoint.X >= polyBounds.MinPoint.X && dimBounds.MaxPoint.X <= polyBounds.MaxPoint.X &&
                                            dimBounds.MinPoint.Y >= polyBounds.MinPoint.Y && dimBounds.MaxPoint.Y <= polyBounds.MaxPoint.Y)
                                        {
                                            // Check if the dimension style is "BLANK SIZE"
                                            DimStyleTableRecord dimStyle = tr.GetObject(dimension.DimensionStyle, OpenMode.ForRead) as DimStyleTableRecord;
                                            if (dimStyle.Name == "BLANK SIZE")
                                            {
                                                // Get the dimension value (dimension text)
                                                double dimValue = dimension.Measurement;

                                                // Store the first dimension in F and second in G
                                                if (dimension1 == 0)
                                                {
                                                    dimension1 = dimValue; // First dimension value
                                                }
                                                else if (dimension2 == 0)
                                                {
                                                    dimension2 = dimValue; // Second dimension value
                                                }
                                            }
                                        }
                                    }
                                }

                                
                                Excel.Range columnB = usedrange.Columns["B"];
                                bool foundMatch = false;

                                foreach (Excel.Range cell in columnB.Cells)
                                {
                                    
                                    string cellValue = cell.Value2?.ToString();

                                    if (cellValue != null && cellValue == partnumber)
                                    {
                                        worksheet.Cells[cell.Row, 3].Value = description;
                                        worksheet.Cells[cell.Row, 4].Value = thickness;
                                        worksheet.Cells[cell.Row, 5].Value = material;
                                        worksheet.Cells[cell.Row, 6].Value = quantity;
                                        worksheet.Cells[cell.Row, 7].Value = dimension1;
                                        worksheet.Cells[cell.Row, 8].Value = dimension2;

                                        if (partnumber == "" || description == "" || quantity == 0 || thickness == 0 || material == "" || dimension1 == 0 || dimension2 == 0)
                                        {
                                            Excel.Range entireRow = worksheet.Rows[cell.Row];
                                            entireRow.Interior.Color = 49407;
                                        }

                                        if (partnumber == "")
                                        {
                                            worksheet.Cells[cell.Row, 2].Interior.Color = 15773696;
                                        }
                                        if (description == "")
                                        {
                                            worksheet.Cells[cell.Row, 3].Interior.Color = 15773696;
                                        }
                                        if (thickness == 0)
                                        {
                                            worksheet.Cells[cell.Row, 4].Interior.Color = 15773696;
                                        }
                                        if (material == "")
                                        {
                                            worksheet.Cells[cell.Row, 5].Interior.Color = 15773696;
                                        }
                                        if (quantity == 0)
                                        {
                                            worksheet.Cells[cell.Row, 6].Interior.Color = 15773696;
                                        }
                                        if (dimension1 == 0)
                                        {
                                            worksheet.Cells[cell.Row, 7].Interior.Color = 15773696;
                                        }
                                        if (dimension2 == 0)
                                        {
                                            worksheet.Cells[cell.Row, 8].Interior.Color = 15773696;
                                        }

                                        foundMatch = true;
                                        break;
                                    }
                                }

                                if (!foundMatch)
                                {
                                    worksheet.Cells[row, 3].Value = description;
                                    worksheet.Cells[row, 4].Value = thickness;
                                    worksheet.Cells[row, 5].Value = material;
                                    worksheet.Cells[row, 6].Value = quantity;
                                    worksheet.Cells[row, 7].Value = dimension1;
                                    worksheet.Cells[row, 8].Value = dimension2;
                                    if (partnumber == "" || description == "" || quantity == 0 || thickness == 0 || material == "" || dimension1 == 0 || dimension2 == 0)
                                    {
                                        Excel.Range entireRow = worksheet.Rows[row];
                                        entireRow.Interior.Color = 49407;
                                    }

                                    if (partnumber == "")
                                    {
                                        worksheet.Cells[row, 2].Interior.Color = 15773696;
                                    }
                                    if (description == "")
                                    {
                                        worksheet.Cells[row, 3].Interior.Color = 15773696;
                                    }
                                    if (thickness == 0)
                                    {
                                        worksheet.Cells[row, 4].Interior.Color = 15773696;
                                    }
                                    if (material == "")
                                    {
                                        worksheet.Cells[row, 5].Interior.Color = 15773696;
                                    }
                                    if (quantity == 0)
                                    {
                                        worksheet.Cells[row, 6].Interior.Color = 15773696;
                                    }
                                    if (dimension1 == 0)
                                    {
                                        worksheet.Cells[row, 7].Interior.Color = 15773696;
                                    }
                                    if (dimension2 == 0)
                                    {
                                        worksheet.Cells[row, 8].Interior.Color = 15773696;
                                    }

                                    row++; 
                                }

                                



                            }
                        }
                    }

                    Excel.Range columnC = usedrange.Columns["C"];
                    foreach (Excel.Range cell in columnC.Cells)
                    {
                        if (cell.Row < 3)
                        {
                            continue;
                        }

                        if (cell.Row == row)
                        {
                            break;
                        }

                        string cellValue = cell.Value2?.ToString();

                        if (cellValue == null)
                        {
                            Excel.Range entireRow = worksheet.Rows[cell.Row];
                            entireRow.Interior.Color = 49407;
                        }
                    }

                    tr.Commit();
                }

                MessageBox.Show("Automation by GaMeR");
            }
            catch (Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }
            finally
            {
                if (workbook != null)
                {
                    Marshal.ReleaseComObject(workbook);
                }
                if (worksheet != null)
                {
                    Marshal.ReleaseComObject(worksheet);
                }
                if (excelApp != null)
                {
                    Marshal.ReleaseComObject(excelApp);
                }
            }

            
        }

        [CommandMethod("SLD")]
        public void ReadExcelData()
        {
            // Get the AutoCAD editor to write messages to the command line
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Check if Excel is already running
            Excel.Application excelApp = null;
            try
            {
                excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");
                ed.WriteMessage("\nAutomation By GaMeR.");
            }
            catch (COMException)
            {
                // Excel is not running, show a message and return
                ed.WriteMessage("\nExcel is not running.");
                return;
            }

            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                // Check if any workbooks are open
                if (excelApp.Workbooks.Count == 0)
                {
                    ed.WriteMessage("\nNo workbooks are open.");
                    return;
                }

                // Check if the workbook contains a sheet named "CAD"
                foreach (Excel.Workbook wb in excelApp.Workbooks)
                {
                    worksheet = null;

                    foreach (Excel.Worksheet ws in wb.Sheets)
                    {
                        if (ws.Name.ToLower() == "cad")
                        {
                            worksheet = ws;
                            break;
                        }
                    }

                    if (worksheet != null)
                    {
                        workbook = wb;
                        break;
                    }
                }

                // If the "CAD" sheet is not found
                if (worksheet == null)
                {
                    ed.WriteMessage("\nWorksheet named 'CAD' not found.");
                    return;
                }

                // Get the used range of the "CAD" worksheet
                Excel.Range usedRange = worksheet.UsedRange;
                Document acadDoc = Application.DocumentManager.MdiActiveDocument;
                Database db = acadDoc.Database;

                using (Transaction transaction = db.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    double startX = 0.0;
                    double startY = 0.0;
                    double lineLength = 250.0;  // Vertical line length (50mm)
                    double gapBetweenLines = 500.0;  // Gap between each vertical line (100mm)
                    double currentX = startX;

                    // Loop through the used range and find the cell with "outgoing"
                    foreach (Excel.Range cell in usedRange)
                    {
                        if (cell.Value2 != null && cell.Value2.ToString().ToLower() == "outgoing")
                        {
                            // Get the outgoing type (value to the right of "outgoing")
                            Excel.Range typeCell = cell.Offset[0, 1];
                            if (typeCell != null && typeCell.Value2 != null)
                            {
                                string outgoingtype = typeCell.Value2.ToString();

                                // Get the outgoing count (value two columns to the right of "outgoing")
                                Excel.Range countCell = cell.Offset[0, 2];
                                if (countCell != null && countCell.Value2 != null)
                                {
                                    double outgoingCount = Convert.ToDouble(countCell.Value2);

                                    // Draw vertical lines for each outgoing count
                                    for (int i = 0; i < outgoingCount; i++)
                                    {
                                        // Draw vertical line
                                        Line verticalLine = new Line(new Point3d(currentX, startY, 0), new Point3d(currentX, startY + lineLength, 0));
                                        blockTableRecord.AppendEntity(verticalLine);
                                        transaction.AddNewlyCreatedDBObject(verticalLine, true);

                                        // Insert block at the top of the vertical line if outgoing type block exists
                                        if (!string.IsNullOrEmpty(outgoingtype) && blockTable.Has(outgoingtype))
                                        {
                                            BlockTableRecord blockDef = transaction.GetObject(blockTable[outgoingtype], OpenMode.ForRead) as BlockTableRecord;

                                            if (blockDef != null && !blockDef.IsAnonymous && !blockDef.IsLayout)
                                            {
                                                BlockReference blockRef = new BlockReference(new Point3d(currentX, startY + lineLength, 0), blockDef.ObjectId);
                                                blockTableRecord.AppendEntity(blockRef);
                                                transaction.AddNewlyCreatedDBObject(blockRef, true);
                                            }
                                            else
                                            {
                                                ed.WriteMessage($"\nBlock '{outgoingtype}' is not valid or does not exist.");
                                            }
                                        }
                                        else
                                        {
                                            ed.WriteMessage($"\nBlock '{outgoingtype}' not found.");
                                        }

                                        // Move the X position for the next line
                                        currentX += gapBetweenLines;
                                    }
                                }
                                else
                                {
                                    ed.WriteMessage($"\nNo count found for outgoing type '{outgoingtype}'.");
                                    return;
                                }
                            }
                            else
                            {
                                ed.WriteMessage("\nNo outgoing type found.");
                                return;
                            }
                        }
                    }

                    // Join all vertical lines at the top with a horizontal line
                    double endX = currentX - gapBetweenLines;  // Adjust for last line
                    Line horizontalLine = new Line(new Point3d(startX, startY + lineLength, 0), new Point3d(endX, startY + lineLength, 0));
                    blockTableRecord.AppendEntity(horizontalLine);
                    transaction.AddNewlyCreatedDBObject(horizontalLine, true);

                    // Commit the transaction to finalize the drawing
                    transaction.Commit();
                }

                acadDoc.SendStringToExecute("._ZOOM _EXTENTS ", true, false, false);



            }
            catch (Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }
            finally
            {
                // Cleanup: No need to close the workbook or quit Excel because we're attaching to an already running instance.
                if (workbook != null)
                {
                    Marshal.ReleaseComObject(workbook);
                }
                if (worksheet != null)
                {
                    Marshal.ReleaseComObject(worksheet);
                }
                if (excelApp != null)
                {
                    Marshal.ReleaseComObject(excelApp);
                }
            }
        }

        [CommandMethod("YnotPDF")]
        public void ExportPDF()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) throw new InvalidOperationException("Active document is null.");

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                
                PromptEntityOptions options = new PromptEntityOptions("\nSelect a rectangle: ");
                options.SetRejectMessage("\nOnly rectangles (closed polylines) are allowed.");
                options.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult result = ed.GetEntity(options);

                if (result.Status != PromptStatus.OK)
                    return;

                ObjectId rectId = result.ObjectId;

                // Prompt for scale number
                PromptDoubleOptions scaleOptions = new PromptDoubleOptions("\nEnter the scale number: ");
                scaleOptions.DefaultValue = 0.8; // Set default value
                scaleOptions.AllowNegative = false; // Optional: Prevent negative values
                scaleOptions.AllowZero = false; // Optional: Prevent zero value if not desired
                PromptDoubleResult scaleResult = ed.GetDouble(scaleOptions);

                if (scaleResult.Status != PromptStatus.OK)
                    return;

                double userscale = scaleResult.Value;

                // Prompt for scale number
                PromptDoubleOptions linescaleOptions = new PromptDoubleOptions("\nEnter the scale number for line types: ");
                linescaleOptions.DefaultValue = 0.1; // Set default value
                linescaleOptions.AllowNegative = false; // Optional: Prevent negative values
                linescaleOptions.AllowZero = false; // Optional: Prevent zero value if not desired
                PromptDoubleResult linescaleResult = ed.GetDouble(linescaleOptions);

                if (linescaleResult.Status != PromptStatus.OK)
                    return;

                double linescale = linescaleResult.Value;

                PromptKeywordOptions lineweightOptions = new PromptKeywordOptions("\nPLOT WITH LINE WEIGHT OR NOT [True/False]: ");
                lineweightOptions.Keywords.Add("True");
                lineweightOptions.Keywords.Add("False");
                lineweightOptions.AllowNone = true; // Allow pressing Enter without choosing
                lineweightOptions.Message = "\nEnter 'True' or 'False' (default is 'True'): ";

                PromptResult lineweightResult = ed.GetKeywords(lineweightOptions);

                // Handle the default manually
                bool plotWithLineWeight;
                if (lineweightResult.Status == PromptStatus.OK)
                {
                    plotWithLineWeight = lineweightResult.StringResult == "True";
                }
                else if (lineweightResult.Status == PromptStatus.None)
                {
                    plotWithLineWeight = true; // Default to True if Enter is pressed
                }
                else
                {
                    return; // Exit on Cancel or other statuses
                }

                // Prompt for file save location and name
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                saveFileDialog.Title = "Save PDF File";
                saveFileDialog.DefaultExt = "pdf"; 
                saveFileDialog.AddExtension = true; 

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                string filePath = saveFileDialog.FileName;

                double oldlinescale = db.Ltscale;

                db.Ltscale = linescale;

                LayoutManager layoutMgr = LayoutManager.Current;
                string currentLayout = layoutMgr.CurrentLayout;

                if (string.IsNullOrEmpty(currentLayout))
                    throw new InvalidOperationException("Current layout is not set.");

                ObjectId layoutId = layoutMgr.GetLayoutId(currentLayout);
                if (layoutId == ObjectId.Null)
                    throw new InvalidOperationException("Layout ID is invalid.");

                Layout layout;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                    if (layout == null)
                        throw new InvalidOperationException("Layout object is null.");

                    tr.Commit();
                }

                List<Extents3d> allRectangles = new List<Extents3d>();
                //List<Extents3d> outerRectangles = new List<Extents3d>();
                Point2d minPoint = new Point2d(0, 0);
                Point2d maxPoint = new Point2d(0, 0);

                using (Transaction acTrans = doc.TransactionManager.StartTransaction())
                {
                    Polyline rect = acTrans.GetObject(rectId, OpenMode.ForRead) as Polyline;

                    if (rect != null && rect.Closed && rect.NumberOfVertices == 4)
                    {

                        Extents3d selectedExtents = rect.GeometricExtents;
                        minPoint = new Point2d(selectedExtents.MinPoint.X, selectedExtents.MinPoint.Y);
                        maxPoint = new Point2d(selectedExtents.MaxPoint.X, selectedExtents.MaxPoint.Y);

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                            string selectedLayer = rect.Layer;

                            foreach (ObjectId objId in btr)
                            {
                                Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                                // Check if the entity is a BlockReference (i.e., a block)
                                if (entity is BlockReference blockRef)
                                {
                                    // Get extents of the block
                                    Extents3d blockExtents = blockRef.GeometricExtents;

                                    // Check if the block is within the selected rectangle
                                    if (IsRectangleWithin(selectedExtents, blockExtents))
                                    {
                                        // Explode the block to retrieve its components
                                        DBObjectCollection explodedEntities = new DBObjectCollection();
                                        blockRef.Explode(explodedEntities);

                                        // Scan for rectangles within the exploded entities
                                        foreach (DBObject explodedObj in explodedEntities)
                                        {
                                            if (explodedObj is Polyline poly && poly.Closed && poly.NumberOfVertices == 4 && poly.Layer == "YNOT")
                                            {
                                                Extents3d polyExtents = poly.GeometricExtents;

                                                // Avoid including the original rectangle
                                                if (polyExtents.Equals(selectedExtents))
                                                    continue;

                                                if (IsRectangleWithin(selectedExtents, polyExtents))
                                                {
                                                    allRectangles.Add(polyExtents);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (entity is Polyline poly && poly.Closed && poly.NumberOfVertices == 4 && poly.Layer == "YNOT")
                                {
                                    Extents3d polyExtents = poly.GeometricExtents;

                                    if (polyExtents.Equals(selectedExtents))
                                        continue;

                                    if (IsRectangleWithin(selectedExtents, polyExtents))
                                    {
                                        
                                        allRectangles.Add(polyExtents);
                                    }
                                }
                            }

                            tr.Commit();
                        }

                        acTrans.Commit();
                    }
                    else
                    {
                        Application.ShowAlertDialog("NOT A RECTANGLE");
                        db.Ltscale = 1;
                        return;
                    }

                    allRectangles = allRectangles.OrderBy(rect1 => rect1.MinPoint.X).ToList();

                    if (allRectangles.Count > 0)
                    {

                        using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
                        {
                            using (PlotProgressDialog progressDialog = new PlotProgressDialog(false, allRectangles.Count, true))
                            {
                                progressDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plotting to PDF");
                                progressDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                progressDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                progressDialog.LowerPlotProgressRange = 0;
                                progressDialog.UpperPlotProgressRange = 100;
                                progressDialog.SheetProgressPos = 0;

                                progressDialog.OnBeginPlot();
                                progressDialog.IsVisible = true;

                                int pageNumber = 1;

                                plotEngine.BeginPlot(progressDialog, null);
                                // Create a new layout for each page
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    foreach (var polyExtents in allRectangles)
                                    {

                                        LayoutManager layoutManager = LayoutManager.Current;
                                        Layout newLayout = new Layout();
                                        newLayout.LayoutName = $"Page {pageNumber}";
                                        layoutManager.CreateLayout(newLayout.LayoutName);
                                        layoutManager.CurrentLayout = newLayout.LayoutName;

                                        // Get the new layout's ID
                                        ObjectId newLayoutId = LayoutManager.Current.GetLayoutId($"Page {pageNumber}");


                                        Layout layout1 = tr.GetObject(newLayoutId, OpenMode.ForWrite) as Layout;
                                        layout1.PrintLineweights = plotWithLineWeight;

                                        // Set the layout page size to A4 landscape
                                        PlotSettingsValidator validator = PlotSettingsValidator.Current;
                                        validator.SetPlotConfigurationName(layout1, "DWG To PDF.pc3", "ISO_A4_(210.00_x_297.00_MM)");
                                        validator.SetPlotPaperUnits(layout1, PlotPaperUnit.Millimeters);
                                        validator.SetPlotRotation(layout1, PlotRotation.Degrees090);
                                        validator.SetCurrentStyleSheet(layout1, "Monochrome.ctb");



                                        // Get the block table record associated with the layout
                                        BlockTableRecord layoutBlock = tr.GetObject(layout1.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;                                      

                                        foreach (ObjectId id in layoutBlock)
                                        {
                                            if (id.ObjectClass.DxfName == "VIEWPORT")
                                            {
                                                Viewport vp2 = tr.GetObject(id, OpenMode.ForWrite) as Viewport;
                                                vp2.Erase();
                                                //vp2.Visible = false;
                                            }
                                        }

                                        Viewport vp = new Viewport();
                                        layoutBlock.AppendEntity(vp);
                                        tr.AddNewlyCreatedDBObject(vp, true);
                                        vp.SetUcsToWorld();

                                        // Calculate the center and dimensions of the extents
                                        Point2d center1 = new Point2d(
                                            (polyExtents.MinPoint.X + polyExtents.MaxPoint.X) / 2,
                                            (polyExtents.MinPoint.Y + polyExtents.MaxPoint.Y) / 2
                                        );
                                        vp.ViewCenter = center1;
                                        
                                        double paperWidth = layout1.PlotPaperSize.X;
                                        double paperHeight = layout1.PlotPaperSize.Y;

                                        // Set the viewport size to match the paper size
                                        vp.Width = paperHeight;
                                        vp.Height = paperWidth;

                                        // Calculate the center of the paper
                                        double paperCenterX = paperHeight / 2;
                                        double paperCenterY = paperWidth / 2;

                                        // Move the viewport to the center of the paper
                                        vp.CenterPoint = new Point3d(131.5, 100, 0);

                                        // Calculate the width and height of the rectangle in model space
                                        double rectWidth = polyExtents.MaxPoint.X - polyExtents.MinPoint.X;
                                        double rectHeight = polyExtents.MaxPoint.Y - polyExtents.MinPoint.Y;

                                        // Calculate the scale factors for width and height
                                        double scaleX = paperWidth / rectWidth;
                                        double scaleY = paperHeight / rectHeight;

                                        // Choose the smaller scale factor to ensure the rectangle fits within the viewport
                                        double scale = Math.Min(scaleX, scaleY);
                                        scale = scale / userscale;
                                        vp.CustomScale = scale;
                                        vp.On = true;

                                        validator.SetPlotType(layout1, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
                                        
                                        validator.SetStdScaleType(layout1, StdScaleType.ScaleToFit);


                                        PlotInfo plotInfo = new PlotInfo
                                        {
                                            Layout = newLayoutId,
                                            OverrideSettings = layout1
                                        };

                                        PlotInfoValidator plotInfoValidator = new PlotInfoValidator();
                                        plotInfoValidator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                                        plotInfoValidator.Validate(plotInfo);

                                        if (pageNumber == 1)
                                        {
                                            plotEngine.BeginDocument(plotInfo, doc.Name, null, 1, true, filePath);
                                        }

                                        PlotPageInfo plotPageInfo = new PlotPageInfo();
                                        plotEngine.BeginPage(plotPageInfo, plotInfo, pageNumber == allRectangles.Count, null);
                                        progressDialog.SheetProgressPos = (int)((pageNumber / (double)allRectangles.Count) * 100);
                                        plotEngine.BeginGenerateGraphics(null);
                                        plotEngine.EndGenerateGraphics(null);
                                        plotEngine.EndPage(null);

                                        pageNumber++;
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, $"Processing page {pageNumber} of {allRectangles.Count}");
                                        
                                    }

                                    plotEngine.EndDocument(null);
                                    plotEngine.EndPlot(null);
                                    progressDialog.OnEndPlot();
                                    progressDialog.IsVisible = false;

                                    tr.Commit();
                                }
                            }
                        }

                        using (Transaction tr2 = db.TransactionManager.StartTransaction())
                        {
                            DBDictionary layoutDict = tr2.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                            LayoutManager layoutManager = LayoutManager.Current;

                            int pageNumberToDel = 1;

                            foreach (var polyExtents in allRectangles) // Replace with your actual collection
                            {
                                string layoutName = $"Page {pageNumberToDel}";

                                // Check if the layout exists
                                if (layoutDict.Contains(layoutName))
                                {
                                    // Delete the layout
                                    layoutManager.DeleteLayout(layoutName);
                                }

                                pageNumberToDel++;
                            }

                            tr2.Commit();

                            layoutManager.CurrentLayout = "MODEL";
                        }
                    }

                    db.Ltscale = oldlinescale;

                }
            }
            catch (Exception ex)
            {

                Application.ShowAlertDialog($"Error exporting PDF: {ex.Message}");
            }
            finally
            {
                
            }
            
        }

        [CommandMethod("YnotPDFALL")]
        public void ExportPDFall()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Active document is null.");

                Database db = doc.Database;
                Editor ed = doc.Editor;

                // Prompt for a rectangle selection
                PromptEntityOptions options = new PromptEntityOptions("\nSelect a rectangle: ");
                options.SetRejectMessage("\nOnly rectangles (closed polylines) are allowed.");
                options.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult result = ed.GetEntity(options);

                if (result.Status != PromptStatus.OK)
                    return;

                ObjectId rectId = result.ObjectId;

                // Prompt for scale number
                PromptDoubleOptions scaleOptions = new PromptDoubleOptions("\nEnter the scale number: ");
                scaleOptions.DefaultValue = 0.8; // Set default value
                scaleOptions.AllowNegative = false; // Optional: Prevent negative values
                scaleOptions.AllowZero = false; // Optional: Prevent zero value if not desired
                PromptDoubleResult scaleResult = ed.GetDouble(scaleOptions);

                if (scaleResult.Status != PromptStatus.OK)
                    return;

                double userscale = scaleResult.Value;

                // Prompt for scale number
                PromptDoubleOptions linescaleOptions = new PromptDoubleOptions("\nEnter the scale number for line types: ");
                linescaleOptions.DefaultValue = 0.1; // Set default value
                linescaleOptions.AllowNegative = false; // Optional: Prevent negative values
                linescaleOptions.AllowZero = false; // Optional: Prevent zero value if not desired
                PromptDoubleResult linescaleResult = ed.GetDouble(linescaleOptions);

                if (linescaleResult.Status != PromptStatus.OK)
                    return;

                double linescale = linescaleResult.Value;

                PromptKeywordOptions lineweightOptions = new PromptKeywordOptions("\nPLOT WITH LINE WEIGHT OR NOT [True/False]: ");
                lineweightOptions.Keywords.Add("True");
                lineweightOptions.Keywords.Add("False");
                lineweightOptions.AllowNone = true; // Allow pressing Enter without choosing
                lineweightOptions.Message = "\nEnter 'True' or 'False' (default is 'True'): ";

                PromptResult lineweightResult = ed.GetKeywords(lineweightOptions);

                // Handle the default manually
                bool plotWithLineWeight;
                if (lineweightResult.Status == PromptStatus.OK)
                {
                    plotWithLineWeight = lineweightResult.StringResult == "True";
                }
                else if (lineweightResult.Status == PromptStatus.None)
                {
                    plotWithLineWeight = true; // Default to True if Enter is pressed
                }
                else
                {
                    return; // Exit on Cancel or other statuses
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Select a folder",
                    Filter = "Folders|\n", // Trick to allow folder selection
                    FileName = "Select this folder", // Dummy file name
                    CheckFileExists = false,
                    CheckPathExists = true
                };

                // Show the dialog and get result
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return; // Exit if no folder is selected
                }
                string folderPath = Path.GetDirectoryName(saveFileDialog.FileName);

                double oldlinescale = db.Ltscale;

                db.Ltscale = linescale;

                LayoutManager layoutMgr = LayoutManager.Current;
                string currentLayout = layoutMgr.CurrentLayout;

                if (string.IsNullOrEmpty(currentLayout))
                    throw new InvalidOperationException("Current layout is not set.");

                ObjectId layoutId = layoutMgr.GetLayoutId(currentLayout);
                if (layoutId == ObjectId.Null)
                    throw new InvalidOperationException("Layout ID is invalid.");

                Layout layout;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                    if (layout == null)
                        throw new InvalidOperationException("Layout object is null.");

                    tr.Commit();
                }

                List<Extents3d> allRectanglesfull = new List<Extents3d>();
                //List<Extents3d> outerRectanglesfull = new List<Extents3d>();
                Point2d minPointfull = new Point2d(0, 0);
                Point2d maxPointfull = new Point2d(0, 0);

                using (Transaction acTransfull = doc.TransactionManager.StartTransaction())
                {
                    Polyline rect = acTransfull.GetObject(rectId, OpenMode.ForRead) as Polyline;

                    if (rect != null && rect.Closed && rect.NumberOfVertices == 4)
                    {

                        Extents3d selectedExtents = rect.GeometricExtents;
                        minPointfull = new Point2d(selectedExtents.MinPoint.X, selectedExtents.MinPoint.Y);
                        maxPointfull = new Point2d(selectedExtents.MaxPoint.X, selectedExtents.MaxPoint.Y);


                        using (Transaction trfull = db.TransactionManager.StartTransaction())
                        {
                            BlockTable btfull = trfull.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btrfull = trfull.GetObject(btfull[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                            string selectedLayerfull = rect.Layer;

                            foreach (ObjectId objId in btrfull)
                            {
                                Entity entity = trfull.GetObject(objId, OpenMode.ForRead) as Entity;

                                if (entity is Polyline poly && poly.Closed && poly.NumberOfVertices == 4 && poly.Layer == "YNOT")
                                {
                                    Extents3d polyExtents = poly.GeometricExtents;

                                    if (polyExtents.Equals(selectedExtents))
                                        continue;

                                    if (IsRectangleWithin(selectedExtents, polyExtents))
                                    {
                                        allRectanglesfull.Add(polyExtents);
                                    }
                                }
                            }

                            trfull.Commit();
                        }

                        acTransfull.Commit();
                    }
                    else
                    {
                        Application.ShowAlertDialog("NOT A RECTANGLE");
                        db.Ltscale = 1;
                        return;
                    }
                }

                allRectanglesfull = allRectanglesfull.OrderByDescending(rect => rect.MaxPoint.Y).ToList();

                int pdfnumber = 1;

                foreach (var polyExtents2 in allRectanglesfull)
                {
                    List<Extents3d> allRectangles = new List<Extents3d>();
                    //List<Extents3d> outerRectangles = new List<Extents3d>();
                    Point2d minPoint = new Point2d(0, 0);
                    Point2d maxPoint = new Point2d(0, 0);

                    string pdfName = null;
                    bool namefound = false;

                    using (Transaction acTrans = doc.TransactionManager.StartTransaction())
                    {

                        Extents3d selectedExtents = polyExtents2;
                        minPoint = new Point2d(selectedExtents.MinPoint.X, selectedExtents.MinPoint.Y);
                        maxPoint = new Point2d(selectedExtents.MaxPoint.X, selectedExtents.MaxPoint.Y);


                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                            //string selectedLayer = rect.Layer;

                            foreach (ObjectId objId in btr)
                            {
                                Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                                // Check if the entity is a BlockReference (i.e., a block)
                                if (entity is BlockReference blockRef)
                                {
                                    // Get extents of the block
                                    Extents3d blockExtents = blockRef.GeometricExtents;

                                    // Check if the block is within the selected rectangle
                                    if (IsRectangleWithin(selectedExtents, blockExtents))
                                    {
                                        // Explode the block to retrieve its components
                                        DBObjectCollection explodedEntities = new DBObjectCollection();
                                        blockRef.Explode(explodedEntities);

                                        // Scan for rectangles within the exploded entities
                                        foreach (DBObject explodedObj in explodedEntities)
                                        {
                                            if (explodedObj is Polyline poly && poly.Closed && poly.NumberOfVertices == 4 && poly.Layer == "YNOT")
                                            {
                                                Extents3d polyExtents = poly.GeometricExtents;

                                                // Avoid including the original rectangle
                                                if (polyExtents.Equals(selectedExtents))
                                                    continue;

                                                if (IsRectangleWithin(selectedExtents, polyExtents))
                                                {
                                                    allRectangles.Add(polyExtents);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (entity is Polyline poly && poly.Closed && poly.NumberOfVertices == 4 && poly.Layer == "YNOT")
                                {
                                    Extents3d polyExtents = poly.GeometricExtents;

                                    if (polyExtents.Equals(selectedExtents))
                                        continue;

                                    if (IsRectangleWithin(selectedExtents, polyExtents))
                                    {
                                        allRectangles.Add(polyExtents);
                                    }
                                }
                            }

                            

                            tr.Commit();
                        }

                        acTrans.Commit();

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;


                            foreach (ObjectId entId in btr)
                            {
                                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                                if (ent is DBText dbText)
                                {
                                    Point3d insertionPoint = dbText.Position; ;

                                    if (insertionPoint.X >= minPoint.X && insertionPoint.X <= maxPoint.X &&
                                        insertionPoint.Y >= minPoint.Y && insertionPoint.Y <= maxPoint.Y)
                                    {
                                        bool isInsideInnerRectangle = false;


                                        foreach (var innerRect in allRectangles)
                                        {
                                            Point3d innerMinPoint = innerRect.MinPoint;
                                            Point3d innerMaxPoint = innerRect.MaxPoint;

                                            if (insertionPoint.X >= innerMinPoint.X && insertionPoint.X <= innerMaxPoint.X &&
                                                insertionPoint.Y >= innerMinPoint.Y && insertionPoint.Y <= innerMaxPoint.Y)
                                            {
                                                isInsideInnerRectangle = true;
                                                break;
                                            }
                                        }

                                        if (!isInsideInnerRectangle && !namefound)
                                        {
                                            string todayDate = DateTime.Now.ToString("dd-MM-yyyy");
                                            string timeWithSeconds = DateTime.Now.ToString("HH-mm-ss");
                                            pdfName = $"{dbText.TextString}-GA-{todayDate}-{timeWithSeconds}";
                                            namefound = true;

                                        }
                                    }
                                }
                            }

                            tr.Commit();
                        }

                        allRectangles = allRectangles.OrderBy(rect1 => rect1.MinPoint.X).ToList();

                        if (allRectangles.Count > 0)
                        {

                            using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
                            {
                                using (PlotProgressDialog progressDialog = new PlotProgressDialog(false, allRectangles.Count, true))
                                {
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plotting to PDF");
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                    progressDialog.LowerPlotProgressRange = 0;
                                    progressDialog.UpperPlotProgressRange = 100;
                                    progressDialog.SheetProgressPos = 0;

                                    progressDialog.OnBeginPlot();
                                    progressDialog.IsVisible = true;

                                    int pageNumber = 1;

                                    plotEngine.BeginPlot(progressDialog, null);
                                    // Create a new layout for each page
                                    using (Transaction tr = db.TransactionManager.StartTransaction())
                                    {
                                        foreach (var polyExtents in allRectangles)
                                        {

                                            LayoutManager layoutManager = LayoutManager.Current;
                                            Layout newLayout = new Layout();
                                            newLayout.LayoutName = $"Page {pageNumber}";
                                            layoutManager.CreateLayout(newLayout.LayoutName);
                                            layoutManager.CurrentLayout = newLayout.LayoutName;

                                            // Get the new layout's ID
                                            ObjectId newLayoutId = LayoutManager.Current.GetLayoutId($"Page {pageNumber}");


                                            Layout layout1 = tr.GetObject(newLayoutId, OpenMode.ForWrite) as Layout;
                                            layout1.PrintLineweights = plotWithLineWeight;

                                            // Set the layout page size to A4 landscape
                                            PlotSettingsValidator validator = PlotSettingsValidator.Current;
                                            validator.SetPlotConfigurationName(layout1, "DWG To PDF.pc3", "ISO_A4_(210.00_x_297.00_MM)");
                                            validator.SetPlotPaperUnits(layout1, PlotPaperUnit.Millimeters);
                                            validator.SetPlotRotation(layout1, PlotRotation.Degrees090);
                                            validator.SetCurrentStyleSheet(layout1, "Monochrome.ctb");



                                            // Get the block table record associated with the layout
                                            BlockTableRecord layoutBlock = tr.GetObject(layout1.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;

                                            foreach (ObjectId id in layoutBlock)
                                            {
                                                if (id.ObjectClass.DxfName == "VIEWPORT")
                                                {
                                                    Viewport vp2 = tr.GetObject(id, OpenMode.ForWrite) as Viewport;
                                                    vp2.Erase();
                                                    //vp2.Visible = false;
                                                }
                                            }

                                            Viewport vp = new Viewport();
                                            layoutBlock.AppendEntity(vp);
                                            tr.AddNewlyCreatedDBObject(vp, true);
                                            vp.SetUcsToWorld();

                                            // Calculate the center and dimensions of the extents
                                            Point2d center1 = new Point2d(
                                                (polyExtents.MinPoint.X + polyExtents.MaxPoint.X) / 2,
                                                (polyExtents.MinPoint.Y + polyExtents.MaxPoint.Y) / 2
                                            );
                                            vp.ViewCenter = center1;

                                            double paperWidth = layout1.PlotPaperSize.X;
                                            double paperHeight = layout1.PlotPaperSize.Y;

                                            // Set the viewport size to match the paper size
                                            vp.Width = paperHeight;
                                            vp.Height = paperWidth;

                                            // Calculate the center of the paper
                                            double paperCenterX = paperHeight / 2;
                                            double paperCenterY = paperWidth / 2;

                                            // Move the viewport to the center of the paper
                                            vp.CenterPoint = new Point3d(131.5, 100, 0);

                                            // Calculate the width and height of the rectangle in model space
                                            double rectWidth = polyExtents.MaxPoint.X - polyExtents.MinPoint.X;
                                            double rectHeight = polyExtents.MaxPoint.Y - polyExtents.MinPoint.Y;

                                            // Calculate the scale factors for width and height
                                            double scaleX = paperWidth / rectWidth;
                                            double scaleY = paperHeight / rectHeight;

                                            // Choose the smaller scale factor to ensure the rectangle fits within the viewport
                                            double scale = Math.Min(scaleX, scaleY);
                                            scale = scale / userscale;
                                            vp.CustomScale = scale;
                                            vp.On = true;


                                            validator.SetPlotOrigin(layout1, new Point2d(0, 0));
                                            
                                            validator.SetPlotType(layout1, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
                                            
                                            validator.SetStdScaleType(layout1, StdScaleType.ScaleToFit);

                                            PlotInfo plotInfo = new PlotInfo
                                            {
                                                Layout = newLayoutId,
                                                OverrideSettings = layout1
                                            };

                                            PlotInfoValidator plotInfoValidator = new PlotInfoValidator();
                                            plotInfoValidator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                                            plotInfoValidator.Validate(plotInfo);

                                            // Construct file path (with a default name)

                                            string fileName = "pdf.pdf";

                                            if (pdfName != null)
                                            {
                                                fileName = $"{pdfName}.pdf";
                                            }
                                            else
                                            {
                                                fileName = $"{pdfnumber}.pdf";
                                            }

                                            string pdfPath = Path.Combine(folderPath, fileName);

                                            if (pageNumber == 1)
                                            {
                                                plotEngine.BeginDocument(plotInfo, doc.Name, null, 1, true, pdfPath);
                                            }

                                            PlotPageInfo plotPageInfo = new PlotPageInfo();
                                            plotEngine.BeginPage(plotPageInfo, plotInfo, pageNumber == allRectangles.Count, null);
                                            progressDialog.SheetProgressPos = (int)((pageNumber / (double)allRectangles.Count) * 100);
                                            plotEngine.BeginGenerateGraphics(null);
                                            plotEngine.EndGenerateGraphics(null);
                                            plotEngine.EndPage(null);

                                            pageNumber++;
                                            progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, $"Processing page {pageNumber} of {allRectangles.Count}");
                                        }

                                        plotEngine.EndDocument(null);
                                        plotEngine.EndPlot(null);
                                        progressDialog.OnEndPlot();
                                        progressDialog.IsVisible = false;



                                        tr.Commit();
                                    }
                                }
                            }

                            using (Transaction tr2 = db.TransactionManager.StartTransaction())
                            {
                                DBDictionary layoutDict = tr2.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                                LayoutManager layoutManager = LayoutManager.Current;

                                int pageNumberToDel = 1;

                                foreach (var polyExtents in allRectangles) // Replace with your actual collection
                                {
                                    string layoutName = $"Page {pageNumberToDel}";

                                    // Check if the layout exists
                                    if (layoutDict.Contains(layoutName))
                                    {
                                        // Delete the layout
                                        layoutManager.DeleteLayout(layoutName);
                                    }

                                    pageNumberToDel++;
                                }

                                tr2.Commit();

                                layoutManager.CurrentLayout = "MODEL";
                            }
                        }

                        
                    }
                    pdfnumber++;
                    System.Threading.Thread.Sleep(2000);
                }

                db.Ltscale = oldlinescale;
                

            }
            catch (Exception ex)
            {

                Application.ShowAlertDialog($"Error exporting PDF: {ex.Message}");
            }

        }

        [CommandMethod("DESCRPTION")]
        public void ENTERDESCRPTION()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            // start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // open the block Table for read
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // open the block Table Record Model space for write
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                // Ask the user for a point
                PromptPointOptions pointOptions = new PromptPointOptions("Specify a point: ");
                PromptPointResult pointResult = ed.GetPoint(pointOptions);

                if (pointResult.Status != PromptStatus.OK)
                {
                    return;
                }

                Point3d descPoint = pointResult.Value;
                int offset = 30;


                // ask the user for a word
                PromptStringOptions Descrption = new PromptStringOptions("Enter a des: ");
                Descrption.AllowSpaces = true;

                // geting the value we typed
                PromptResult desresult = ed.GetString(Descrption);
                String descrption = desresult.StringResult.ToUpper();
                String DESFINAL = $"DESCRPTION - {descrption}";
                //MessageBox.Show(DESFINAL);


                DBText textdesc = new DBText();
                textdesc.Position = descPoint;
                textdesc.Height = 15;
                textdesc.TextString = DESFINAL;
                textdesc.ColorIndex = 2;

                // Add the text object to the model space
                btr.AppendEntity(textdesc);
                tr.AddNewlyCreatedDBObject(textdesc, true);




                // for thickness
                // ask the user for a word
                PromptStringOptions whatthick = new PromptStringOptions("Enter a THICKNESS: ");

                // geting the value we typed
                PromptResult enteredthick = ed.GetString(whatthick);
                String thickness = enteredthick.StringResult.ToUpper();
                String thickfinal = $"THICKNESS - {thickness} mm";
                //MessageBox.Show(thickfinal);


                Point3d thickpoint = new Point3d(descPoint.X, descPoint.Y - offset, descPoint.Z);
                offset = offset + 30;


                DBText textthick = new DBText();
                textthick.Position = thickpoint;
                textthick.Height = 15;
                textthick.TextString = thickfinal;
                textthick.ColorIndex = 3;

                // Add the text object to the model space
                btr.AppendEntity(textthick);
                tr.AddNewlyCreatedDBObject(textthick, true);



                // for QUANTITY
                // ask the user for a word
                PromptStringOptions whatqty = new PromptStringOptions("Enter a QUANTITY: ");

                // geting the value we typed
                PromptResult enteredqty = ed.GetString(whatqty);
                String quantity = enteredqty.StringResult.ToUpper();
                String qtyfinal = $"QTY - {quantity} NOS";
                //MessageBox.Show(qtyfinal);


                Point3d qtypoint = new Point3d(descPoint.X, descPoint.Y - offset, descPoint.Z);
                offset = offset + 30;


                DBText textqty = new DBText();
                textqty.Position = qtypoint;
                textqty.Height = 15;
                textqty.TextString = qtyfinal;
                textqty.ColorIndex = 1;

                // Add the text object to the model space
                btr.AppendEntity(textqty);
                tr.AddNewlyCreatedDBObject(textqty, true);



                // for BENDING
                // ask the user for a word
                PromptStringOptions whatbend = new PromptStringOptions("Enter a BENDING: ");

                // geting the value we typed
                PromptResult enteredbend = ed.GetString(whatbend);
                String bending = enteredbend.StringResult.ToUpper();
                if (bending != null && bending != "")
                {
                    String bendfinal = $"BENDING - BEND {bending}";
                    Point3d bendpoint = new Point3d(descPoint.X, descPoint.Y - offset, descPoint.Z);
                    offset = offset + 30;


                    DBText textbend = new DBText();
                    textbend.Position = bendpoint;
                    textbend.Height = 15;
                    textbend.TextString = bendfinal;
                    textbend.ColorIndex = 3;
                    // Add the text object to the model space
                    btr.AppendEntity(textbend);
                    tr.AddNewlyCreatedDBObject(textbend, true);
                }




                // for MATERIAL
                // ask the user for a word
                PromptStringOptions whatmaterial = new PromptStringOptions("Enter a MATERIAL: ");

                // geting the value we typed
                PromptResult enteredmaterial = ed.GetString(whatmaterial);
                String material = enteredmaterial.StringResult.ToUpper();
                String materialfinal = $"MATERIAL - {material}";
                //MessageBox.Show(bendfinal);


                Point3d materialpoint = new Point3d(descPoint.X, descPoint.Y - offset, descPoint.Z);
                offset = offset + 30;


                DBText textmaterial = new DBText();
                textmaterial.Position = materialpoint;
                textmaterial.Height = 15;
                textmaterial.TextString = materialfinal;
                textmaterial.ColorIndex = 2;

                // Add the text object to the model space
                btr.AppendEntity(textmaterial);
                tr.AddNewlyCreatedDBObject(textmaterial, true);

                Point3d point1 = new Point3d(descPoint.X - 30, descPoint.Y + 45, descPoint.Z);
                Point3d point2 = new Point3d(descPoint.X + 250, descPoint.Y + 45, descPoint.Z);
                Point3d point3 = new Point3d(descPoint.X - 30, descPoint.Y - offset - 30, descPoint.Z);
                Point3d point4 = new Point3d(descPoint.X + 250, descPoint.Y - offset - 30, descPoint.Z);

                Polyline rectangle = new Polyline();

                // Add the rectangle's vertices
                rectangle.AddVertexAt(0, new Point2d(point1.X, point1.Y), 0, 0, 0); // Start point
                rectangle.AddVertexAt(1, new Point2d(point2.X, point2.Y), 0, 0, 0); // Top-right point
                rectangle.AddVertexAt(2, new Point2d(point4.X, point4.Y), 0, 0, 0); // Bottom-right point
                rectangle.AddVertexAt(3, new Point2d(point3.X, point3.Y), 0, 0, 0); // Bottom-left point

                // Close the polyline to form a rectangle
                rectangle.Closed = true;

                // Set the color index
                rectangle.ColorIndex = 8; // Set to color index 8 (gray)

                // Add the polyline to the model space
                btr.AppendEntity(rectangle);
                tr.AddNewlyCreatedDBObject(rectangle, true);



                tr.Commit();
            }
        }


        private bool IsRectangleWithin(Extents3d outer, Extents3d inner)
        {
            return outer.MinPoint.X <= inner.MinPoint.X && outer.MinPoint.Y <= inner.MinPoint.Y &&
                   outer.MaxPoint.X >= inner.MaxPoint.X && outer.MaxPoint.Y >= inner.MaxPoint.Y;
        }

    }
    }
