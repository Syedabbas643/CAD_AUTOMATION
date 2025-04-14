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
using Color = Autodesk.AutoCAD.Colors.Color;
using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using ExcelApplication = Microsoft.Office.Interop.Excel.Application;
using Excel = Microsoft.Office.Interop.Excel;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using Microsoft.WindowsAPICodePack.Dialogs;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.GraphicsSystem;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using System.Windows.Documents;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Windows.Media.Media3D;
using System.Security.Cryptography;
using Autodesk.AutoCAD.Colors;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Drawing;
using Point = System.Drawing.Point;
using System.Windows;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Net.NetworkInformation;
using System.Net;
using System.Security.Policy;
using System.Windows.Media;
using System.Runtime.ConstrainedExecution;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Runtime.InteropServices.ComTypes;
using Autodesk.AutoCAD.Internal.Calculator;

namespace CAD_AUTOMATION
{
    
    public class RectangleDrawer : IExtensionApplication
    {
        private static string lastFolderName = string.Empty;
        private static string lastFileName = string.Empty;
        private static double lastoblen;
        private static double lastobwid;
        private static bool isEnabled = false;
        //update 28feb
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_RESTORE = 9; // Restore if minimized
        private const int SW_MAXIMIZE = 3;

        public static void BringExcelToFront(Excel.Application excelApp)
        {
            if (excelApp == null) return;

            IntPtr excelHandle = new IntPtr(excelApp.Hwnd);
            ShowWindow(excelHandle, SW_MAXIMIZE);
            SetForegroundWindow(excelHandle);
        }
        public void Initialize()
        {
            
            string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Define the path to the http.exe
            string httpExePath = Path.Combine(pluginDirectory, "http.exe");

            if (File.Exists(httpExePath))
            {
                // Start the external http.exe with command-line argument
                ProcessStartInfo startInfo = new ProcessStartInfo(httpExePath, "/fromautocad");
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                // Start the process (http.exe)
                Process process = Process.Start(startInfo);

                // Listen to the pipe for the message
                Thread pipeListenerThread = new Thread(() =>
                {
                    try
                    {
                        // Create a named pipe server to listen for messages
                        using (var pipeServer = new NamedPipeServerStream("AutoCADPipe", PipeDirection.In))
                        {
                            pipeServer.WaitForConnection();

                            // Read the message sent by the WinForms app (http.exe)
                            using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
                            {
                                string message = reader.ReadLine();
                                if (message == "OK") 
                                {
                                    isEnabled = true;
                                }
                            }

                            pipeServer.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Error listening to pipe: {ex.Message}");
                    }
                });

                pipeListenerThread.Start();
                //process.WaitForExit();
            }
            else
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("http.exe not found in the same directory as the plugin.");
            }

            RibbonClass ribbon = new RibbonClass();
            ribbon.CreateRibbon();

        }
        public void Terminate()
        {
            // Clean up any resources if needed
        }

        [CommandMethod("oblong")]
        public void DrawRectangle()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
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

                Line line1 = new Line(topLeft, new Point3d(bottomRight.X, topLeft.Y, bottomRight.Z));
                Line line3 = new Line(bottomRight, new Point3d(topLeft.X, bottomRight.Y, topLeft.Z));
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
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
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

        [CommandMethod("`")]
        public static void CopyObjectsToNewDrawing()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }

            Document activeDoc = Application.DocumentManager.MdiActiveDocument;
            Database activeDb = activeDoc.Database;

            // Prompt the user to select objects
            PromptSelectionResult selResult = activeDoc.Editor.GetSelection();
            if (selResult.Status != PromptStatus.OK)
            {
                activeDoc.Editor.WriteMessage("\nSelection canceled.");
                return;
            }

            SelectionSet selSet = selResult.Value;
            string filename = "";
            string foldername = "";
            bool error = false;
            string errorText = "";

            try
            {
                using (Transaction tr = activeDb.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject obj in selSet)
                    {
                        if (obj != null)
                        {
                            DBText textEntity = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as DBText;
                            if (textEntity != null && textEntity.TextString.StartsWith("PART NUMBER -"))
                            {
                                filename = textEntity.TextString;
                                break;
                            }
                        }
                    }

                    foreach (SelectedObject obj in selSet)
                    {
                        if (obj != null)
                        {
                            DBText textEntity = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as DBText;
                            if (textEntity != null && textEntity.TextString.StartsWith("QTY -"))
                            {
                                filename = filename +"-"+ textEntity.TextString.Replace("QTY -","");
                                break;
                            }
                        }
                    }

                    foreach (SelectedObject obj in selSet)
                    {
                        if (obj != null)
                        {
                            DBText textEntity2 = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as DBText;
                            if (textEntity2 != null && (textEntity2.TextString.StartsWith("THICK -") || (textEntity2.TextString.StartsWith("THICKNESS -"))))
                            {
                                foldername = textEntity2.TextString.Replace("THICK - ", "").Replace("THICKNESS - ", "");
                                break;
                            }
                        }
                    }

                    tr.Commit();
                }

                if (string.IsNullOrEmpty(filename))
                {
                    Application.ShowAlertDialog("No 'PART NUMBER -' text found in the selection.");
                    return;
                }
                if (string.IsNullOrEmpty(foldername))
                {
                    Application.ShowAlertDialog("No 'THICK -' text found in the selection.");
                    return;
                }
                // Create a new drawing based on the template
                Document newDoc = Application.DocumentManager.Add("acad.dwt");
                Database newDb = newDoc.Database;

                // Use a lock on the new document
                using (DocumentLock docLock = newDoc.LockDocument())
                {
                    // Start a transaction in the active database
                    using (Transaction activeTr = activeDb.TransactionManager.StartTransaction())
                    {
                        // Start a transaction in the new database
                        using (Transaction newTr = newDb.TransactionManager.StartTransaction())
                        {
                            // Convert selected objects to ObjectIdCollection
                            ObjectIdCollection objectIdCollection = new ObjectIdCollection(selSet.GetObjectIds());

                            // Clone selected objects into the new database
                            IdMapping idMapping = new IdMapping();
                            activeDb.WblockCloneObjects(
                                objectIdCollection,
                                newDb.CurrentSpaceId,
                                idMapping,
                                DuplicateRecordCloning.Replace,
                                false
                            );

                            //newTr.Commit();

                            // Open the Block Table Record for the new drawing's Model Space
                            BlockTable newBt = newTr.GetObject(newDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord newBtr = newTr.GetObject(newBt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            
                            foreach (ObjectId objId in newBtr)
                            {
                                Entity entity = newTr.GetObject(objId, OpenMode.ForWrite) as Entity;

                                if (entity != null)
                                {
                                    // Check if it's a BlockReference (block)
                                    if (entity is BlockReference blockRef)
                                    {
                                        BlockReference blockRefForWrite = newTr.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as BlockReference;
                                        //MessageBox.Show(blockRefForWrite.Name);
                                        if (blockRefForWrite != null)
                                        {

                                            if (blockRef.IsDynamicBlock)
                                            {
                                                DBObjectCollection explodedEntities = new DBObjectCollection();
                                                blockRefForWrite.Explode(explodedEntities);
                                                try
                                                {
                                                    foreach (Entity explodedEntity in explodedEntities)
                                                    {
                                                        // Clone the entity to get a fresh ObjectId
                                                        Entity clonedEntity = explodedEntity.Clone() as Entity;

                                                        if (clonedEntity != null)
                                                        {
                                                            newBtr.AppendEntity(clonedEntity);
                                                            newTr.AddNewlyCreatedDBObject(clonedEntity, true);
                                                        }
                                                    }
                                                }
                                                catch (System.Exception ex)
                                                {
                                                    error = true;
                                                    errorText = ex.Message;
                                                }

                                                blockRefForWrite.Erase();

                                            }
                                            else
                                            {
                                                DBObjectCollection explodedEntities = new DBObjectCollection();
                                                blockRefForWrite.Explode(explodedEntities);

                                                try
                                                {
                                                    foreach (Entity explodedEntity in explodedEntities)
                                                    {
                                                        // Check if the entity is already in the model space by checking the ObjectId
                                                        bool isAlreadyInDb = false;
                                                        foreach (ObjectId existingObjId in newBtr)
                                                        {
                                                            Entity existingEntity = newTr.GetObject(existingObjId, OpenMode.ForRead) as Entity;
                                                            if (existingEntity != null && explodedEntity.GetType() == existingEntity.GetType() && explodedEntity.ObjectId == existingEntity.ObjectId)
                                                            {
                                                                isAlreadyInDb = true;
                                                                break;
                                                            }
                                                        }

                                                        if (!isAlreadyInDb)
                                                        {
                                                            newBtr.AppendEntity(explodedEntity);
                                                            newTr.AddNewlyCreatedDBObject(explodedEntity, true);
                                                        }
                                                    }
                                                }
                                                catch (System.Exception ex)
                                                {
                                                    error = true;
                                                    errorText = ex.Message;
                                                }

                                                blockRefForWrite.Erase();
                                            }

                                            
                                        }
                                    }
                                    
                                }
                            }

                            newBt = newTr.GetObject(newDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                            newBtr = newTr.GetObject(newBt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            // Loop through the objects in the new drawing and filter out unwanted types
                            foreach (ObjectId objId in newBtr)
                            {
                                Entity entity = newTr.GetObject(objId, OpenMode.ForWrite) as Entity;

                                if (entity != null)
                                {
                                        string lineType = entity.Linetype;
                                        if (lineType == "HIDDEN" || lineType == "DASHED" || lineType == "CENTER" || lineType == "CENTERX2" || lineType == "HIDDEN2")
                                        {
                                            entity.Erase();
                                            continue; // Skip the rest of the checks once deleted
                                        }

                                        if (entity.ColorIndex == 8)
                                        {
                                            entity.Erase();
                                            continue;
                                        }

                                        if (entity.Layer == "PARTS")
                                        {
                                            entity.Erase();
                                            continue;
                                        }

                                        if (entity.Layer == "SIDEVIEW")
                                        {
                                            entity.Erase();
                                            continue;
                                        }

                                        if (entity is Dimension)
                                        {
                                            entity.Erase();
                                            continue;
                                        }

                                        if (entity is DBText)
                                        {
                                            entity.Erase();
                                            continue;
                                        }
                                    }
                                }

                            List<Entity> entitiesToDelete = new List<Entity>();

                            HashSet<string> entityHashes = new HashSet<string>();

                            foreach (ObjectId objId in newBtr)
                            {
                                Entity entity = (Entity)newTr.GetObject(objId, OpenMode.ForWrite);
                                string entityKey = null; // Unique identifier for each entity type

                                if (entity is Line line)
                                {
                                    // Create a unique key for lines based on StartPoint & EndPoint
                                    entityKey = $"{line.StartPoint}-{line.EndPoint}";
                                }
                                else if (entity is Circle circle)
                                {
                                    // Create a unique key for circles based on Center & Radius
                                    entityKey = $"{circle.Center}-{circle.Radius}";
                                }
                                else if (entity is Polyline polyline && polyline.NumberOfVertices == 4)
                                {
                                    // Handle rectangles (Polyline with 4 vertices)
                                    List<string> points = new List<string>();
                                    for (int i = 0; i < 4; i++)
                                    {
                                        points.Add(polyline.GetPoint2dAt(i).ToString());
                                    }
                                    // Sort points to handle different rectangle orientations
                                    points.Sort();
                                    entityKey = string.Join("-", points);
                                }

                                // Check if this entity is already found
                                if (entityKey != null)
                                {
                                    if (entityHashes.Contains(entityKey))
                                    {
                                        entity.Erase(); // Duplicate detected, erase it
                                    }
                                    else
                                    {
                                        entityHashes.Add(entityKey); // Store the unique entity
                                    }
                                }
                            }

                            newTr.Commit();
                        }

                        
                        activeTr.Commit();
                    }

                }
                //Application.DocumentManager.MdiActiveDocument = newDoc;

                string originalPath = activeDb.Filename;

                if (string.IsNullOrEmpty(originalPath) || originalPath.Contains(@"AppData\Local\Temp"))
                {
                    // Get the last known saved location using the DWGPREFIX system variable
                    originalPath = Application.GetSystemVariable("DWGPREFIX") as string;

                    // If still not found, ask the user to provide a save location
                    if (string.IsNullOrEmpty(originalPath))
                    {
                        MessageBox.Show("The drawing has never been saved. Please save it first.", "Save Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string folderPath = System.IO.Path.GetDirectoryName(originalPath);

                // Create folder if it doesn't exist
                string partFolderPath = System.IO.Path.Combine(folderPath, foldername);
                System.IO.Directory.CreateDirectory(partFolderPath);

                // Save the new document in the created folder
                string newFilePath = System.IO.Path.Combine(partFolderPath, $"{filename}.dxf");
                newDb.DxfOut(newFilePath, 16, DwgVersion.AC1024);

                newDoc.CloseAndDiscard();

                Document newDoc2 = Application.DocumentManager.Open(newFilePath, false);
                Application.DocumentManager.MdiActiveDocument = newDoc2;

                newDoc2.SendStringToExecute("._ZOOM _EXTENTS ", true, false, false);
                
                //newDoc2.SendStringToExecute("._OVERKILL ", true, false, false);

                if(error)
                {
                    MessageBox.Show($"Automation Completed with error : {errorText} ");
                }
               

            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog($"Error: {ex.Message}");
            }
        }

        [CommandMethod("1")]
        static public void ZoomExtents()
        {
            // Zoom to the extents of the current space
            Zoom(new Point3d(), new Point3d(), new Point3d(), 1.01075);
        }

        [CommandMethod("2")]
        static public void overkill()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("._OVERKILL ", true, false, false);
        }

        [CommandMethod("PARTS_AUTOMATOR")]
        public static void BOMcount2()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            string error = "";
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Open the Block Table for read
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block Table Record for Model Space
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

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

                    if (layercount != partnumbercount)
                    {
                        MessageBox.Show("Some Parts are not in PARTS layer");
                        return;
                    }

                    string filename = "";
                    string foldername = "";
                    


                    foreach (ObjectId objId2 in btr)
                    {
                        // Get the entity and check if it's a polyline (representing a rectangle)
                        Entity ent = tr.GetObject(objId2, OpenMode.ForRead) as Entity;

                        if (ent != null && ent is Polyline)
                        {
                            Polyline poly = ent as Polyline;

                            // Check if the polyline has 4 vertices (closed rectangle) and is on the "PARTS" layer
                            if (poly.NumberOfVertices == 4 && poly.Closed && poly.Layer == "PARTS")
                            {
                                // Get the rectangle bounds
                                Extents3d polyBounds = poly.GeometricExtents;

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

                                            if (dbText != null && dbText.TextString.StartsWith("PART NUMBER -"))
                                            {
                                                filename = dbText.TextString;
                                                break;
                                            }
                                        }
                                    }
                                }

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
                                            if (dbText != null && dbText.TextString.StartsWith("QTY -"))
                                            {
                                                filename = filename + "-" + dbText.TextString.Replace("QTY -", "");
                                                break;
                                            }
                                        }
                                    }
                                }

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
                                            if (dbText != null && (dbText.TextString.StartsWith("THICK -") || (dbText.TextString.StartsWith("THICKNESS -"))))
                                            {
                                                foldername = dbText.TextString.Replace("THICK - ", "").Replace("THICKNESS - ", "");
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(filename))
                                {
                                    Application.ShowAlertDialog("No 'PART NUMBER -' text found in the selection.");
                                    return;
                                }
                                if (string.IsNullOrEmpty(foldername))
                                {
                                    Application.ShowAlertDialog("No 'THICK -' text found in the selection.");
                                    return;
                                }

                                // Create a new drawing based on the template
                                Document newDoc = Application.DocumentManager.Add("acad.dwt");
                                Database newDb = newDoc.Database;
                                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                                // Use a lock on the new document
                                using (DocumentLock docLock = newDoc.LockDocument())
                                {
                                    using (Transaction newTr = newDb.TransactionManager.StartTransaction())
                                    {

                                        foreach (ObjectId objId in btr)
                                        {
                                            Entity ent2 = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                                            if (ent2 != null)
                                            {
                                                Extents3d? entityBounds = ent2.Bounds;
                                                if (!entityBounds.HasValue) continue;

                                                Extents3d entityExtents = entityBounds.Value;

                                                if (polyBounds.MinPoint.X <= entityExtents.MinPoint.X && polyBounds.MaxPoint.X >= entityExtents.MaxPoint.X &&
                                                    polyBounds.MinPoint.Y <= entityExtents.MinPoint.Y && polyBounds.MaxPoint.Y >= entityExtents.MaxPoint.Y)
                                                {
                                                    objectIdCollection.Add(objId);
                                                }

                                            }
                                        }

                                        if (objectIdCollection.Count == 0)
                                        {
                                            Application.ShowAlertDialog($"\nNo objects found inside the {filename}.");
                                            return;
                                        }
                                        // Convert selected objects to ObjectIdCollection
                                        //ObjectIdCollection objectIdCollection = new ObjectIdCollection(selSet.GetObjectIds());

                                        // Clone selected objects into the new database
                                        IdMapping idMapping = new IdMapping();
                                        db.WblockCloneObjects(
                                            objectIdCollection,
                                            newDb.CurrentSpaceId,
                                            idMapping,
                                            DuplicateRecordCloning.Replace,
                                            false
                                        );

                                        // Open the Block Table Record for the new drawing's Model Space
                                        BlockTable newBt = newTr.GetObject(newDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                                        BlockTableRecord newBtr = newTr.GetObject(newBt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                                        //DBObjectCollection explodedEntities = new DBObjectCollection();

                                        try
                                        {
                                            foreach (ObjectId objId in newBtr)
                                            {
                                                Entity entity = newTr.GetObject(objId, OpenMode.ForWrite) as Entity;

                                                if (entity != null)
                                                {
                                                    // Check if it's a BlockReference (block)
                                                    if (entity is BlockReference blockRef)
                                                    {
                                                        BlockReference blockRefForWrite = newTr.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as BlockReference;
                                                        //MessageBox.Show(blockRefForWrite.Name);
                                                        if (blockRefForWrite != null)
                                                        {

                                                            if (blockRef.IsDynamicBlock)
                                                            {
                                                                DBObjectCollection explodedEntities = new DBObjectCollection();
                                                                blockRefForWrite.Explode(explodedEntities);
                                                                foreach (Entity explodedEntity in explodedEntities)
                                                                {
                                                                    // Clone the entity to get a fresh ObjectId
                                                                    Entity clonedEntity = explodedEntity.Clone() as Entity;

                                                                    if (clonedEntity != null)
                                                                    {
                                                                        newBtr.AppendEntity(clonedEntity);
                                                                        newTr.AddNewlyCreatedDBObject(clonedEntity, true);
                                                                    }
                                                                }
                                                                blockRefForWrite.Erase();

                                                            }
                                                            else
                                                            {
                                                                DBObjectCollection explodedEntities = new DBObjectCollection();
                                                                blockRefForWrite.Explode(explodedEntities);


                                                                foreach (Entity explodedEntity in explodedEntities)
                                                                {
                                                                    // Check if the entity is already in the model space by checking the ObjectId
                                                                    bool isAlreadyInDb = false;
                                                                    foreach (ObjectId existingObjId in newBtr)
                                                                    {
                                                                        Entity existingEntity = newTr.GetObject(existingObjId, OpenMode.ForRead) as Entity;
                                                                        if (existingEntity != null && explodedEntity.GetType() == existingEntity.GetType() && explodedEntity.ObjectId == existingEntity.ObjectId)
                                                                        {
                                                                            isAlreadyInDb = true;
                                                                            break;
                                                                        }
                                                                    }

                                                                    if (!isAlreadyInDb)
                                                                    {
                                                                        newBtr.AppendEntity(explodedEntity);
                                                                        newTr.AddNewlyCreatedDBObject(explodedEntity, true);
                                                                    }
                                                                }
                                                            }
                                                            blockRefForWrite.Erase();
                                                        }


                                                    }
                                                }

                                            }
                                        }
                                        catch (System.Exception ex)
                                        {
                                            error = error + $"\n Error: {ex.Message} in {filename}";
                                        }
                                        

                                        //newTr.Commit();

                                        newBt = newTr.GetObject(newDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                                        newBtr = newTr.GetObject(newBt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                                        // Loop through the objects in the new drawing and filter out unwanted types
                                        foreach (ObjectId objId in newBtr)
                                            {
                                                Entity entity = newTr.GetObject(objId, OpenMode.ForWrite) as Entity;

                                                if (entity != null)
                                                {
                                                    // Check for specific linetypes to delete
                                                    string lineType = entity.Linetype;
                                                    if (lineType == "HIDDEN" || lineType == "DASHED" || lineType == "CENTER" || lineType == "CENTERX2" || lineType == "HIDDEN2")
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }

                                                    if (entity.ColorIndex == 8)
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }

                                                    if (entity.Layer == "PARTS")
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }

                                                    if (entity.Layer == "SIDEVIEW")
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }

                                                    if (entity is Dimension)
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }

                                                    if (entity is DBText)
                                                    {
                                                        entity.Erase();
                                                        continue;
                                                    }
                                                }
                                            }
                                        List<Entity> entitiesToDelete = new List<Entity>();

                                        HashSet<string> entityHashes = new HashSet<string>();

                                        foreach (ObjectId objId in newBtr)
                                        {
                                            Entity entity = (Entity)newTr.GetObject(objId, OpenMode.ForWrite);
                                            string entityKey = null; // Unique identifier for each entity type

                                            if (entity is Line line)
                                            {
                                                // Create a unique key for lines based on StartPoint & EndPoint
                                                entityKey = $"{line.StartPoint}-{line.EndPoint}";
                                            }
                                            else if (entity is Circle circle)
                                            {
                                                // Create a unique key for circles based on Center & Radius
                                                entityKey = $"{circle.Center}-{circle.Radius}";
                                            }
                                            else if (entity is Polyline polyline && polyline.NumberOfVertices == 4)
                                            {
                                                // Handle rectangles (Polyline with 4 vertices)
                                                List<string> points = new List<string>();
                                                for (int i = 0; i < 4; i++)
                                                {
                                                    points.Add(polyline.GetPoint2dAt(i).ToString());
                                                }
                                                // Sort points to handle different rectangle orientations
                                                points.Sort();
                                                entityKey = string.Join("-", points);
                                            }

                                            // Check if this entity is already found
                                            if (entityKey != null)
                                            {
                                                if (entityHashes.Contains(entityKey))
                                                {
                                                    entity.Erase(); // Duplicate detected, erase it
                                                }
                                                else
                                                {
                                                    entityHashes.Add(entityKey); // Store the unique entity
                                                }
                                            }
                                        }

                                        newTr.Commit();
                                    }

                                        

                                }
                                //Application.DocumentManager.MdiActiveDocument = newDoc;

                                string originalPath = db.Filename;

                                if (string.IsNullOrEmpty(originalPath) || originalPath.Contains(@"AppData\Local\Temp"))
                                {
                                    // Get the last known saved location using the DWGPREFIX system variable
                                    originalPath = Application.GetSystemVariable("DWGPREFIX") as string;

                                    // If still not found, ask the user to provide a save location
                                    if (string.IsNullOrEmpty(originalPath))
                                    {
                                        MessageBox.Show("The drawing has never been saved. Please save it first.", "Save Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }
                                }
                                string folderPath = System.IO.Path.GetDirectoryName(originalPath);

                                // Create folder if it doesn't exist
                                string partFolderPath = System.IO.Path.Combine(folderPath, foldername);
                                System.IO.Directory.CreateDirectory(partFolderPath);

                                // Save the new document in the created folder
                                string newFilePath = System.IO.Path.Combine(partFolderPath, $"{filename}.dxf");
                                newDb.DxfOut(newFilePath, 16, DwgVersion.AC1024);

                                //Thread.Sleep(500);
                                newDb.CloseInput(true);
                                newDoc.CloseAndDiscard();
                                //Thread.Sleep(500);

                            }
                        }
                    }
                   tr.Commit();
                }
                if (error == "")
                {
                    MessageBox.Show("Automation by GaMeR");
                }
                else 
                {
                    MessageBox.Show($"Automation Completed with error \n {error}");
                }
                
            }
            catch (Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }
        }

        [CommandMethod("MECHBOM")]
        public static void BOMcount()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
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
                                                int dimValue = Convert.ToInt32(dimension.Measurement);

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

        [CommandMethod("DRAW_GA")]
        public void DrawRectanglesFromExcel()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }

            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get Excel application instance
            Excel.Application excelApp = null;
            Excel.Range selectedRange;
            Excel.Range descRange;
            bool error = false;
            string errorText = "";

            // Ask the user for a point
            PromptPointOptions pointOptions = new PromptPointOptions("Specify a point: ");
            PromptPointResult pointResult = ed.GetPoint(pointOptions);

            if (pointResult.Status != PromptStatus.OK)
            {
                return;
            }

            Point3d descPoint = pointResult.Value;
            double baseheight = 0;
            bool rearcabling = false;
            string view = "";
            string paneltype = "";

            using (panelselection panelform = new panelselection())
            {
                if (panelform.ShowDialog() == DialogResult.OK)
                {

                    string lineweightResult = panelform.BaseSize;

                    paneltype = panelform.paneltype;

                    if (lineweightResult == "ISMC75")
                    {
                        baseheight = 75;
                    }
                    else if (lineweightResult == "ISMC100")
                    {
                        baseheight = 100;
                    }
                    else if (lineweightResult == "CRCA50")
                    {
                        baseheight = 50;
                    }

                    view = panelform.ViewPosition;


                    if (panelform.cablealley == "REAR CABLING")
                    {
                        rearcabling = true;
                    }

                }
            }

            try
            {
                excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");
                BringExcelToFront(excelApp);

                Form topmostForm = new Form
                {
                    TopMost = true, // Keep it on top
                    //TopLevel = true,
                    FormBorderStyle = FormBorderStyle.None,
                    Opacity = 0, // Hide the form itself
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                };
                topmostForm.Load += (sender, e) =>
                {
                    topmostForm.Location = new Point(0, 0);
                    topmostForm.Activate(); // Ensure it remains active
                };

                topmostForm.Show();

                // Show the message box with an OK and Cancel button
                DialogResult result = MessageBox.Show(
                    topmostForm,
                    "Please select the first range in Excel and click OK.",
                    "Select First Range",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );

                selectedRange = excelApp.Selection as Excel.Range;

                if (result == DialogResult.Cancel || selectedRange == null || selectedRange.Cells.Count < 1)
                {
                    MessageBox.Show("Invalid first selection. Please restart the command and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DialogResult result2 = MessageBox.Show(
                    topmostForm,
                    "Please select the second range in Excel and click OK.",
                    "Select second Range",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );

                // Close the topmost form after MessageBox is closed
                topmostForm.Close();

                descRange = excelApp.Selection as Excel.Range;

                if (result2 == DialogResult.Cancel || descRange == null || descRange.Cells.Count < 1)
                {
                    MessageBox.Show("Invalid second selection. Please restart the command and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (COMException)
            {
                MessageBox.Show("\nExcel is not running.");
                return;
            }

            if(paneltype == "TI")
            {
                try
                {
                    if (selectedRange == null || selectedRange.Cells.Count < 2)
                    {
                        MessageBox.Show("\nPlease select at least two cells in Excel.");
                        return;
                    }

                    Application.MainWindow.WindowState = Autodesk.AutoCAD.Windows.Window.State.Maximized;
                    Application.MainWindow.Focus();
                    // AutoCAD document setup
                    Document acadDoc = Application.DocumentManager.MdiActiveDocument;
                    Database db = acadDoc.Database;

                    // Load blocks from external DWG file
                    string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string cadFilePath = Path.Combine(pluginDirectory, "blocks.dwg");

                    if (!File.Exists(cadFilePath))
                    {
                        MessageBox.Show("\nBlocks DWG file not found.");
                        return;
                    }

                    //ImportBlocksFromDWG(db, cadFilePath);

                    using (Database sourceDb = new Database(false, true))
                    {
                        sourceDb.ReadDwgFile(cadFilePath, FileOpenMode.OpenForReadAndReadShare, false, null);

                        using (Transaction transaction = db.TransactionManager.StartTransaction())
                        {
                            BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord blockTableRecord = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            DrawOrderTable drawOrderTable = transaction.GetObject(blockTableRecord.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                            double startX = descPoint.X;
                            double startY = descPoint.Y;
                            double sidechannel = 30;
                            double bottomchannel = 50;
                            double topchannel = 80;
                            int bottomchannelcolor = 7;
                            int basecolor = 4;
                            double shippingleftX = 0.0;
                            double shippingrigthX = 0.0;
                            double shippingcolor = 0.0;
                            double shippingcount = 0.0;
                            double panelheight = 0.0;
                            double feedernumbercol = 1;
                            double maxdepth = 0;
                            string busbarposition = "";
                            bool lastbusbar = false;
                            double busbarheight = 0;
                            bool sidedoor = false;
                            List<string> mergeaddress = new List<string>();
                            List<List<double>> feederheights = new List<List<double>>();

                            // Check if "GaMeR" dimension style exists
                            DimStyleTable dimStyleTable = transaction.GetObject(db.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;
                            string dimStyleName = "GaMeR";
                            ObjectId dimStyleId;

                            if (!dimStyleTable.Has(dimStyleName))
                            {
                                DimStyleTableRecord newDimStyle = new DimStyleTableRecord
                                {
                                    Name = dimStyleName,

                                    // Set dimension style properties here
                                    Dimclrd = Color.FromColorIndex(ColorMethod.ByColor, 6),
                                    Dimclrt = Color.FromColorIndex(ColorMethod.ByColor, 3),
                                    Dimclre = Color.FromColorIndex(ColorMethod.ByColor, 6),
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

                                // Open the dim style table for write before adding
                                dimStyleTable.UpgradeOpen();

                                // Add the new dimension style to the table and transaction
                                dimStyleId = dimStyleTable.Add(newDimStyle);
                                transaction.AddNewlyCreatedDBObject(newDimStyle, true);
                            }
                            else
                            {
                                // If it already exists, get its ObjectId
                                dimStyleId = dimStyleTable[dimStyleName];
                            }

                            // Set the new dimension style as the current one
                            db.Dimstyle = dimStyleId;

                            for (int col = 1; col <= selectedRange.Columns.Count; col++) // Left to right
                            {
                                double width = 0.0;
                                bool horizontallink = false;
                                double previouswidth = 0.0;
                                bool feederfound = false;
                                bool vbbfound = false;
                                bool cablechamberfound = false;
                                List<Point3d> feederaddress = new List<Point3d>();
                                feederheights.Add(new List<double>());
                                string whichside = "";
                                bool instrumentfound = false;
                                double checkpanelheight = 0.0;
                                double depth = 0;


                                for (int row = selectedRange.Rows.Count; row >= 1; row--) // Bottom to top
                                {

                                    Excel.Range cell = selectedRange.Cells[row, col];
                                    double height = 0.0;
                                    string[] splitValues = null;

                                    if (cell.Value2?.ToString().ToLower() == "rhs")
                                    {
                                        whichside = "rhs";
                                        continue;
                                    }
                                    else if (cell.Value2?.ToString().ToLower() == "lhs")
                                    {
                                        whichside = "lhs";
                                        continue;
                                    }
                                    else if (cell.Value2?.ToString().ToLower() == "-")
                                    {
                                        continue;
                                    }

                                    if (cell.Interior.Color != 16777215)
                                    {

                                        if (row != selectedRange.Rows.Count - 1)
                                        {
                                            transaction.Commit();
                                            MessageBox.Show($"\nInterior color are only allowed in bottom cells for vertical width.");
                                            return;
                                        }

                                        //MessageBox.Show(cell.Font.Color.ToString());

                                        width = double.Parse(cell.Value2.ToString());
                                        previouswidth = width;

                                        if (shippingcolor == cell.Interior.Color)
                                        {
                                            shippingrigthX += width;
                                        }
                                        else
                                        {
                                            shippingcolor = cell.Interior.Color;

                                            if (col != 1)
                                            {
                                                shippingcount++;
                                                Point3d bottomLeft1 = new Point3d(shippingleftX, descPoint.Y - bottomchannel, 0);
                                                Point3d topRight1 = new Point3d(shippingrigthX, descPoint.Y, 0);

                                                Polyline bottomrect = Addrectangle(transaction, blockTableRecord, bottomLeft1, topRight1, bottomchannelcolor);
                                                //Hatch bottomhatch = new Hatch();
                                                //bottomhatch.SetDatabaseDefaults();

                                                //bottomhatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                                //bottomhatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 254);

                                                //// Add hatch to drawing
                                                //blockTableRecord.AppendEntity(bottomhatch);
                                                //transaction.AddNewlyCreatedDBObject(bottomhatch, true);

                                                //// Associate the hatch with the rectangle boundary
                                                //ObjectIdCollection bottomboundaryIds = new ObjectIdCollection();
                                                //bottomboundaryIds.Add(bottomrect.ObjectId);
                                                //bottomhatch.Associative = true;
                                                //bottomhatch.AppendLoop(HatchLoopTypes.External, bottomboundaryIds);

                                                //// Finalize the hatch
                                                //bottomhatch.EvaluateHatch(true);


                                                //drawOrderTable.MoveToBottom(new ObjectIdCollection { bottomhatch.ObjectId });

                                                //Point3d bottomLeft3 = new Point3d(shippingleftX + 10, descPoint.Y + panelheight - 5, 0);
                                                //Point3d topRight3 = new Point3d(shippingrigthX - 10, descPoint.Y + panelheight - 5 + topchannel, 0);
                                                Point3d bottomLeft3 = new Point3d(shippingleftX, descPoint.Y + panelheight, 0);
                                                Point3d topRight3 = new Point3d(shippingrigthX, descPoint.Y + panelheight + topchannel, 0);
                                                Polyline Toprect = Addrectangle(transaction, blockTableRecord, bottomLeft3, topRight3);
                                                Hatch tophatch = new Hatch();
                                                tophatch.SetDatabaseDefaults();

                                                tophatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                                tophatch.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(16, 86, 137);

                                                // Add hatch to drawing
                                                blockTableRecord.AppendEntity(tophatch);
                                                transaction.AddNewlyCreatedDBObject(tophatch, true);

                                                // Associate the hatch with the rectangle boundary
                                                ObjectIdCollection topboundaryIds = new ObjectIdCollection();
                                                topboundaryIds.Add(Toprect.ObjectId);
                                                tophatch.Associative = true;
                                                tophatch.AppendLoop(HatchLoopTypes.External, topboundaryIds);

                                                // Finalize the hatch
                                                tophatch.EvaluateHatch(true);

                                                drawOrderTable.MoveToBottom(new ObjectIdCollection { tophatch.ObjectId });

                                                if (baseheight > 0)
                                                {
                                                    Point3d basebottomLeft = new Point3d(shippingleftX, bottomLeft1.Y - baseheight, 0);
                                                    Point3d basetopRight = new Point3d(shippingrigthX, bottomLeft1.Y, 0);
                                                    Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                                    Hatch hatch = new Hatch();
                                                    hatch.SetDatabaseDefaults();

                                                    hatch.PatternScale = 4.0;
                                                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                                    hatch.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(204, 204, 204);
                                                    // Add hatch to drawing
                                                    blockTableRecord.AppendEntity(hatch);
                                                    transaction.AddNewlyCreatedDBObject(hatch, true);

                                                    // Associate the hatch with the rectangle boundary
                                                    ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                    boundaryIds.Add(baserect.ObjectId);
                                                    hatch.Associative = true;
                                                    hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                    hatch.EvaluateHatch(true);

                                                    BlockReference bases = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TIBASE", new Point3d(shippingleftX + 35, descPoint.Y - bottomchannel - (baseheight / 2), 0), 1.0);

                                                    int arraycount = (int)((shippingrigthX - shippingleftX - 65) / 14);

                                                    var source = (BlockReference)transaction.GetObject(bases.ObjectId, OpenMode.ForWrite);
                                                    var parameters = new AssocArrayRectangularParameters(14, 14, 0, arraycount, 1, 0, 0, 0);
                                                    var vertexRef = new VertexRef(source.Position);
                                                    var assocArray = AssocArray.CreateArray(new ObjectIdCollection { source.ObjectId }, vertexRef, parameters);
                                                    var assocArrayBlock = (BlockReference)transaction.GetObject(assocArray.EntityId, OpenMode.ForWrite);
                                                    assocArrayBlock.Position = source.Position;
                                                    source.Erase();
                                                }

                                                AlignedDimension dim2 = new AlignedDimension(
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + topchannel + 180, 0),
                                                    $"<>(SS-{shippingcount})", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                blockTableRecord.AppendEntity(dim2);
                                                transaction.AddNewlyCreatedDBObject(dim2, true);

                                                //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING HOOK", new Point3d(shippingleftX + 78, descPoint.Y + panelheight, 0), 1.0);
                                                //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING HOOK", new Point3d(shippingrigthX - 78, descPoint.Y + panelheight, 0), 1.0);
                                                if (shippingcount != 1)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOGO", new Point3d((shippingleftX + shippingrigthX) / 2, descPoint.Y + panelheight + 35, 0), 1.0);
                                                }

                                            }
                                            shippingleftX = startX;
                                            shippingrigthX = startX + width;
                                        }



                                        continue;
                                    }

                                    if (cell.MergeCells)
                                    {
                                        if (mergeaddress.Contains(cell.MergeArea.Cells[1, 1].Address))
                                        {
                                            if (cell.MergeArea.Columns.Count > 1)
                                            {
                                                if (cell.Column != cell.MergeArea.Cells[1, 1].Column)
                                                {
                                                    Excel.Range firstCellInMerge = cell.MergeArea.Cells[1, 1];

                                                    string cellValue = firstCellInMerge.Value2.ToString();

                                                    string[] splitValues2 = cellValue.Split('#');

                                                    if (splitValues2.Length >= 2)
                                                    {
                                                        double leftCellValue = double.Parse(splitValues2[1]);
                                                        startY += leftCellValue;
                                                        checkpanelheight += leftCellValue;
                                                        if (feederheights.Count > 0)
                                                        {
                                                            feederheights[feederheights.Count - 1].Add(leftCellValue);
                                                        }
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        transaction.Commit();
                                                        MessageBox.Show($"\nInvalid cell format in {firstCellInMerge.Address}. Expected format: FEEDER ID # WIDTH");
                                                        return;
                                                    }
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                continue;
                                            }


                                        }
                                    }

                                    if (cell.MergeCells)
                                    {
                                        if (cell.Column == cell.MergeArea.Cells[1, 1].Column)
                                        {
                                            if (cell.MergeArea.Columns.Count > 1)
                                            {
                                                // Loop through all columns in the merged range
                                                for (int mergedCol = col + 1; mergedCol <= col + cell.MergeArea.Columns.Count - 1; mergedCol++)
                                                {
                                                    if (mergedCol > selectedRange.Columns.Count) // Ensure within the selected range
                                                        break;

                                                    Excel.Range rightCellBottom = selectedRange.Cells[selectedRange.Rows.Count - 1, mergedCol];

                                                    if (rightCellBottom.Interior.Color != 16777215) // Check if it's valid
                                                    {
                                                        double rightCellWidth = double.Parse(rightCellBottom.Value2.ToString());
                                                        width += rightCellWidth;
                                                        horizontallink = true;
                                                        //MessageBox.Show($"\nWidth from column {mergedCol}: {rightCellWidth}, Total Width: {width}");
                                                    }
                                                    else
                                                    {
                                                        transaction.Commit();
                                                        MessageBox.Show($"\nInvalid or missing width value in cell: {rightCellBottom.Address}.");
                                                        return;
                                                    }
                                                }
                                            }
                                            else
                                            {

                                            }

                                        }
                                    }

                                    if (cell.MergeCells)
                                    {
                                        Range mergedRange = cell.MergeArea; // Get the merged range
                                        Range lastCell = mergedRange.Cells[mergedRange.Cells.Count]; // Get the last cell in the merged range

                                        // Get the right-side cell of the last cell
                                        Range rightCell = lastCell.Offset[0, 1];
                                        int selectedFirstCol = selectedRange.Column;
                                        int selectedLastCol = selectedFirstCol + selectedRange.Columns.Count - 1;

                                        // Check if the right cell is out of the selected range
                                        if (rightCell.Column > selectedLastCol)
                                        {
                                            lastbusbar = true;
                                        }

                                        Excel.Range cell2 = cell.MergeArea.Cells[1, 1];
                                        // Now check the value of the cell (either merged or not)
                                        if (cell2.Value2 != null)
                                        {
                                            mergeaddress.Add(cell2.Address);

                                            string cellValue = cell2.Value2.ToString(); // Get cell value as string

                                            // Split the string by '#' and check if it has at least 3 parts
                                            splitValues = cellValue.Split('#');

                                            if (splitValues.Length >= 2)
                                            {
                                                height = double.Parse(splitValues[1]);
                                                if (col == 1)
                                                {
                                                    panelheight += double.Parse(splitValues[1]);
                                                }
                                                checkpanelheight += double.Parse(splitValues[1]);

                                                if (feederheights.Count > 0)
                                                {
                                                    feederheights[feederheights.Count - 1].Add(height);
                                                }

                                                if (row == 2)
                                                {
                                                    if (checkpanelheight != panelheight)
                                                    {
                                                        transaction.Commit();
                                                        Application.ShowAlertDialog($"\nInvalid height value in {col} Vertical. Expected height: {panelheight + bottomchannel}");
                                                        return;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                transaction.Commit();
                                                Application.ShowAlertDialog($"\nInvalid cell format in {cell2.Address}. Expected format: FEEDER ID # WIDTH");
                                                return;
                                            }



                                        }
                                    }
                                    else if (cell.Value2 != null)
                                    {
                                        mergeaddress.Add(cell.Address);
                                        string cellValue = cell.Value2.ToString(); // Get cell value as string

                                        // Split the string by '#' and check if it has at least 3 parts
                                        splitValues = cellValue.Split('#');

                                        Range rightCell = cell.Offset[0, 1];
                                        int selectedFirstCol = selectedRange.Column;
                                        int selectedLastCol = selectedFirstCol + selectedRange.Columns.Count - 1;

                                        // Check if the right cell is out of the selected range
                                        if (rightCell.Column > selectedLastCol)
                                        {
                                            lastbusbar = true;
                                        }

                                        if (splitValues.Length >= 2)
                                        {
                                            height = double.Parse(splitValues[1]);
                                            if (col == 1)
                                            {
                                                panelheight += double.Parse(splitValues[1]);
                                            }
                                            checkpanelheight += double.Parse(splitValues[1]);
                                            if (feederheights.Count > 0)
                                            {
                                                feederheights[feederheights.Count - 1].Add(height);
                                            }
                                            if (row == 2)
                                            {
                                                if (checkpanelheight != panelheight)
                                                {
                                                    transaction.Commit();
                                                    Application.ShowAlertDialog($"\nInvalid height value in {col}th Vertical. Expected height: {panelheight + bottomchannel}");
                                                    return;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (row == 1)
                                            {
                                                if (checkpanelheight != panelheight)
                                                {
                                                    transaction.Commit();
                                                    Application.ShowAlertDialog($"\nInvalid height value in {col}th Vertical. Expected height: {panelheight + bottomchannel}");
                                                    return;
                                                }
                                            }

                                            if (double.TryParse(cellValue, out double tempdepth))
                                            {
                                                if (row == 1)
                                                {
                                                    // FOR TOP AND BOTTOM VIEW
                                                    if (maxdepth < tempdepth)
                                                    {
                                                        maxdepth = tempdepth;
                                                    }
                                                    depth = tempdepth;

                                                    AlignedDimension dim = new AlignedDimension(
                                                    new Point3d(startX, descPoint.Y + panelheight + topchannel, 0), // First point
                                                    new Point3d(startX + width, descPoint.Y + panelheight + topchannel, 0), // Second point
                                                    new Point3d(startX, descPoint.Y + panelheight + topchannel + 90, 0), // Dimension line position (50mm down)
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                    blockTableRecord.AppendEntity(dim);
                                                    transaction.AddNewlyCreatedDBObject(dim, true);

                                                    if (view == "TOPVIEW")
                                                    {

                                                        if (col == 1)
                                                        {
                                                            Point3d sideBottomleft = new Point3d(startX - sidechannel, descPoint.Y + panelheight + 800, 0);
                                                            Point3d sideTopright = new Point3d(startX, descPoint.Y + panelheight + 800 + depth, 0);
                                                            Polyline sidetop = Addrectangle(transaction, blockTableRecord, sideBottomleft, sideTopright);

                                                            Hatch tophatch = new Hatch();
                                                            tophatch.SetDatabaseDefaults();

                                                            tophatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                                            tophatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(tophatch);
                                                            transaction.AddNewlyCreatedDBObject(tophatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection topboundaryIds = new ObjectIdCollection();
                                                            topboundaryIds.Add(sidetop.ObjectId);
                                                            tophatch.Associative = true;
                                                            tophatch.AppendLoop(HatchLoopTypes.External, topboundaryIds);

                                                            // Finalize the hatch
                                                            tophatch.EvaluateHatch(true);

                                                            drawOrderTable.MoveToBottom(new ObjectIdCollection { tophatch.ObjectId });

                                                            AlignedDimension dimtopheight = new AlignedDimension(
                                                            new Point3d(sideBottomleft.X, sideBottomleft.Y, 0),
                                                            new Point3d(sideBottomleft.X, sideTopright.Y, 0),
                                                            new Point3d(sideBottomleft.X - 90, sideBottomleft.Y, 0),
                                                            "", // Dimension text (Auto-generated)
                                                            db.Dimstyle // Use current dimension style
                                                            );

                                                            blockTableRecord.AppendEntity(dimtopheight);
                                                            transaction.AddNewlyCreatedDBObject(dimtopheight, true);

                                                        }
                                                        else if (col == selectedRange.Columns.Count)
                                                        {
                                                            Point3d sideBottomleft = new Point3d(startX + width, descPoint.Y + panelheight + 800, 0);
                                                            Point3d sideTopright = new Point3d(startX + width + sidechannel, descPoint.Y + panelheight + 800 + depth, 0);
                                                            Polyline sidetop = Addrectangle(transaction, blockTableRecord, sideBottomleft, sideTopright);

                                                            Hatch tophatch = new Hatch();
                                                            tophatch.SetDatabaseDefaults();

                                                            tophatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                                            tophatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(tophatch);
                                                            transaction.AddNewlyCreatedDBObject(tophatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection topboundaryIds = new ObjectIdCollection();
                                                            topboundaryIds.Add(sidetop.ObjectId);
                                                            tophatch.Associative = true;
                                                            tophatch.AppendLoop(HatchLoopTypes.External, topboundaryIds);

                                                            // Finalize the hatch
                                                            tophatch.EvaluateHatch(true);

                                                            drawOrderTable.MoveToBottom(new ObjectIdCollection { tophatch.ObjectId });
                                                        }


                                                        Point3d Bottomleft = new Point3d(startX, descPoint.Y + panelheight + 800, 0);
                                                        Point3d Topright = new Point3d(startX + width, descPoint.Y + panelheight + 800 + depth, 0);
                                                        Addrectangle(transaction, blockTableRecord, Bottomleft, Topright, 10);
                                                        Polyline door = Addrectangle(transaction, blockTableRecord, new Point3d(Bottomleft.X + 30, Bottomleft.Y - 20, 0), new Point3d(Topright.X - 30, Bottomleft.Y, 0), 10);
                                                        if (whichside == "rhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(15 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Topright.X - 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);
                                                            Point3d startPoint = Bottomleft;
                                                            Point3d endPoint = door.GetPoint3dAt(3);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), -0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), -0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        else if (whichside == "lhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(345 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Bottomleft.X + 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);
                                                            Point3d startPoint = new Point3d(Topright.X, Bottomleft.Y, 0);
                                                            Point3d endPoint = door.GetPoint3dAt(2);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), 0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        Polyline doorline = new Polyline();
                                                        doorline.AddVertexAt(0, new Point2d(Bottomleft.X + 17, Topright.Y), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(Bottomleft.X + 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(Topright.X - 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(Topright.X - 17, Topright.Y), 0, 0, 0);
                                                        //doorline.ColorIndex = 10;

                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }

                                                        if (feederfound)
                                                        {

                                                            DBText widthText = new DBText
                                                            {
                                                                Position = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0),
                                                                Height = 40,
                                                                TextString = $"{feedernumbercol}F",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0)
                                                            };
                                                            blockTableRecord.AppendEntity(widthText);
                                                            transaction.AddNewlyCreatedDBObject(widthText, true);
                                                            DrawCircle(transaction, blockTableRecord, new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0), 45, 10);


                                                        }



                                                        if (cablechamberfound || (feederfound && rearcabling))
                                                        {
                                                            Point3d glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - 350, 0);
                                                            Point3d glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);

                                                            if (maxdepth < 430)
                                                            {
                                                                glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - maxdepth + 100, 0);
                                                                glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);
                                                            }
                                                            // FOR GLAND PLATE
                                                            Polyline gland = new Polyline(4);
                                                            gland.AddVertexAt(0, new Point2d(glandTopright.X, glandTopright.Y), 0, 0, 0);
                                                            gland.AddVertexAt(1, new Point2d(glandTopright.X, glandBottomleft.Y), 0, 0, 0);
                                                            gland.AddVertexAt(2, new Point2d(((glandBottomleft.X + glandTopright.X) / 2) + 40, ((glandBottomleft.Y + glandTopright.Y) / 2) + 60), 0, 0, 0);
                                                            gland.AddVertexAt(3, new Point2d(glandBottomleft.X, glandTopright.Y), 0, 0, 0);
                                                            gland.Closed = true;
                                                            gland.ColorIndex = 10;
                                                            blockTableRecord.AppendEntity(gland);
                                                            transaction.AddNewlyCreatedDBObject(gland, true);


                                                            Polyline glandrec = Addrectangle(transaction, blockTableRecord, glandBottomleft, glandTopright, 10);

                                                            double textX = (glandBottomleft.X + glandTopright.X) / 2;
                                                            double textY = (glandBottomleft.Y + glandTopright.Y) / 2;

                                                            MText glandText = new MText
                                                            {
                                                                Location = new Point3d(textX - 5, textY - 20, 0), // Corrected midpoint calculation
                                                                Height = 30,
                                                                TextHeight = 25,
                                                                Width = 200,
                                                                Contents = "GLAND PLATE",
                                                                Attachment = AttachmentPoint.MiddleCenter // Better alignment
                                                            };

                                                            blockTableRecord.AppendEntity(glandText);
                                                            transaction.AddNewlyCreatedDBObject(glandText, true);

                                                            Hatch hatch = new Hatch();
                                                            hatch.SetDatabaseDefaults();

                                                            hatch.PatternScale = 2.5;
                                                            hatch.SetHatchPattern(HatchPatternType.PreDefined, "DASH");
                                                            hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(hatch);
                                                            transaction.AddNewlyCreatedDBObject(hatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                            boundaryIds.Add(gland.ObjectId);
                                                            hatch.Associative = true;
                                                            hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                            hatch.EvaluateHatch(false);


                                                        }
                                                        else if (vbbfound)
                                                        {
                                                            DBText feederText = new DBText
                                                            {
                                                                Position = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Height = 35,
                                                                TextString = "V.B.C",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                            };
                                                            blockTableRecord.AppendEntity(feederText);
                                                            transaction.AddNewlyCreatedDBObject(feederText, true);
                                                        }
                                                    }
                                                    else if (view == "BOTTOMVIEW")
                                                    {

                                                        if (col == 1)
                                                        {
                                                            Point3d sideBottomleft = new Point3d(startX - sidechannel, descPoint.Y - 700 - depth, 0);
                                                            Point3d sideTopright = new Point3d(startX, descPoint.Y - 700, 0);
                                                            Polyline sidebottom = Addrectangle(transaction, blockTableRecord, sideBottomleft, sideTopright);

                                                            Hatch bottomhatch = new Hatch();
                                                            bottomhatch.SetDatabaseDefaults();

                                                            bottomhatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                                            bottomhatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(bottomhatch);
                                                            transaction.AddNewlyCreatedDBObject(bottomhatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection bottomboundaryIds = new ObjectIdCollection();
                                                            bottomboundaryIds.Add(sidebottom.ObjectId);
                                                            bottomhatch.Associative = true;
                                                            bottomhatch.AppendLoop(HatchLoopTypes.External, bottomboundaryIds);

                                                            // Finalize the hatch
                                                            bottomhatch.EvaluateHatch(true);

                                                            drawOrderTable.MoveToBottom(new ObjectIdCollection { bottomhatch.ObjectId });

                                                            AlignedDimension dimtopheight = new AlignedDimension(
                                                            new Point3d(sideBottomleft.X, sideBottomleft.Y, 0),
                                                            new Point3d(sideBottomleft.X, sideTopright.Y, 0),
                                                            new Point3d(sideBottomleft.X - 90, sideBottomleft.Y, 0),
                                                            "", // Dimension text (Auto-generated)
                                                            db.Dimstyle // Use current dimension style
                                                            );

                                                            blockTableRecord.AppendEntity(dimtopheight);
                                                            transaction.AddNewlyCreatedDBObject(dimtopheight, true);

                                                        }
                                                        else if (col == selectedRange.Columns.Count)
                                                        {
                                                            Point3d sideBottomleft = new Point3d(startX + width, descPoint.Y - 700 - depth, 0);
                                                            Point3d sideTopright = new Point3d(startX + width + sidechannel, descPoint.Y - 700, 0);
                                                            Polyline sidebottom = Addrectangle(transaction, blockTableRecord, sideBottomleft, sideTopright);

                                                            Hatch bottomhatch = new Hatch();
                                                            bottomhatch.SetDatabaseDefaults();

                                                            bottomhatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                                            bottomhatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(bottomhatch);
                                                            transaction.AddNewlyCreatedDBObject(bottomhatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection bottomboundaryIds = new ObjectIdCollection();
                                                            bottomboundaryIds.Add(sidebottom.ObjectId);
                                                            bottomhatch.Associative = true;
                                                            bottomhatch.AppendLoop(HatchLoopTypes.External, bottomboundaryIds);

                                                            // Finalize the hatch
                                                            bottomhatch.EvaluateHatch(true);

                                                            drawOrderTable.MoveToBottom(new ObjectIdCollection { bottomhatch.ObjectId });
                                                        }


                                                        Point3d Bottomleft = new Point3d(startX, descPoint.Y - 700 - depth, 0);
                                                        Point3d Topright = new Point3d(startX + width, descPoint.Y - 700, 0);
                                                        Addrectangle(transaction, blockTableRecord, Bottomleft, Topright, 10);
                                                        Polyline door = Addrectangle(transaction, blockTableRecord, new Point3d(Bottomleft.X + 30, Bottomleft.Y - 20, 0), new Point3d(Topright.X - 30, Bottomleft.Y, 0), 10);
                                                        if (whichside == "rhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(15 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Topright.X - 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);

                                                            Point3d startPoint = Bottomleft;
                                                            Point3d endPoint = door.GetPoint3dAt(3);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), -0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), -0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        else if (whichside == "lhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(345 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Bottomleft.X + 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);

                                                            Point3d startPoint = new Point3d(Topright.X, Bottomleft.Y, 0);
                                                            Point3d endPoint = door.GetPoint3dAt(2);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), 0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        Polyline doorline = new Polyline();
                                                        doorline.AddVertexAt(0, new Point2d(Bottomleft.X + 17, Topright.Y), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(Bottomleft.X + 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(Topright.X - 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(Topright.X - 17, Topright.Y), 0, 0, 0);
                                                        //doorline.ColorIndex = 10;

                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }

                                                        if (feederfound)
                                                        {

                                                            DBText widthText = new DBText
                                                            {
                                                                Position = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0),
                                                                Height = 40,
                                                                TextString = $"{feedernumbercol}F",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0)
                                                            };
                                                            blockTableRecord.AppendEntity(widthText);
                                                            transaction.AddNewlyCreatedDBObject(widthText, true);
                                                            DrawCircle(transaction, blockTableRecord, new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0), 45, 10);




                                                        }

                                                        if (cablechamberfound || (feederfound && rearcabling))
                                                        {
                                                            Point3d glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - 350, 0);
                                                            Point3d glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);

                                                            if (maxdepth < 430)
                                                            {
                                                                glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - maxdepth + 100, 0);
                                                                glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);
                                                            }

                                                            Polyline gland = new Polyline(4);
                                                            gland.AddVertexAt(0, new Point2d(glandTopright.X, glandTopright.Y), 0, 0, 0);
                                                            gland.AddVertexAt(1, new Point2d(glandTopright.X, glandBottomleft.Y), 0, 0, 0);
                                                            gland.AddVertexAt(2, new Point2d(((glandBottomleft.X + glandTopright.X) / 2) + 40, ((glandBottomleft.Y + glandTopright.Y) / 2) + 60), 0, 0, 0);
                                                            gland.AddVertexAt(3, new Point2d(glandBottomleft.X, glandTopright.Y), 0, 0, 0);
                                                            gland.Closed = true;
                                                            gland.ColorIndex = 10;
                                                            blockTableRecord.AppendEntity(gland);
                                                            transaction.AddNewlyCreatedDBObject(gland, true);

                                                            Polyline glandrec = Addrectangle(transaction, blockTableRecord, glandBottomleft, glandTopright, 10);

                                                            double textX = (glandBottomleft.X + glandTopright.X) / 2;
                                                            double textY = (glandBottomleft.Y + glandTopright.Y) / 2;

                                                            MText glandText = new MText
                                                            {
                                                                Location = new Point3d(textX - 5, textY - 20, 0), // Corrected midpoint calculation
                                                                Height = 30,
                                                                TextHeight = 25,
                                                                Width = 200,
                                                                Contents = "GLAND PLATE",
                                                                Attachment = AttachmentPoint.MiddleCenter // Better alignment
                                                            };

                                                            blockTableRecord.AppendEntity(glandText);
                                                            transaction.AddNewlyCreatedDBObject(glandText, true);

                                                            Hatch hatch = new Hatch();
                                                            hatch.SetDatabaseDefaults();

                                                            hatch.PatternScale = 2.5;
                                                            hatch.SetHatchPattern(HatchPatternType.PreDefined, "DASH");
                                                            hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(hatch);
                                                            transaction.AddNewlyCreatedDBObject(hatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                            boundaryIds.Add(gland.ObjectId);
                                                            hatch.Associative = true;
                                                            hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                            hatch.EvaluateHatch(false);

                                                        }
                                                        else if (vbbfound)
                                                        {
                                                            DBText feederText = new DBText
                                                            {
                                                                Position = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Height = 35,
                                                                TextString = "V.B.C",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                            };
                                                            blockTableRecord.AppendEntity(feederText);
                                                            transaction.AddNewlyCreatedDBObject(feederText, true);
                                                        }


                                                    }


                                                    continue;
                                                }
                                                else
                                                {
                                                    transaction.Commit();
                                                    MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                transaction.Commit();
                                                MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                                return;
                                            }

                                        }
                                    }

                                    if (height == 0)
                                    {
                                        transaction.Commit();
                                        MessageBox.Show($"\nInvalid or missing height value in cell: {cell.Address}.");
                                        return;
                                    }

                                    if (width == 0)
                                    {
                                        transaction.Commit();
                                        MessageBox.Show($"\nInvalid or missing width value in cell: {cell.Address}.");
                                        return;
                                    }

                                    // Draw rectangle
                                    Point3d bottomLeft = new Point3d(startX, startY, 0);
                                    Point3d topRight = new Point3d(startX + width, startY + height, 0);


                                    Polyline rectangle = new Polyline(4);
                                    rectangle.AddVertexAt(0, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(topRight.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(bottomLeft.X, topRight.Y), 0, 0, 0);
                                    rectangle.Closed = true;
                                    rectangle.ColorIndex = 10;

                                    blockTableRecord.AppendEntity(rectangle);
                                    transaction.AddNewlyCreatedDBObject(rectangle, true);



                                    if (splitValues.Length >= 2)
                                    {
                                        string feederid = splitValues[0];
                                        string feederidlow = feederid.Replace(" ", "").ToLower();
                                        string feedername = "";

                                        for (int row2 = 1; row2 <= descRange.Rows.Count; row2++)
                                        {
                                            Excel.Range feederidcell = descRange.Cells[row2, 1];
                                            if (feederidcell.Value2 != null)
                                            {
                                                string feederidcellvalue = feederidcell.Value2.ToString();
                                                if (feederidlow.Contains(feederidcellvalue.Replace(" ", "").ToLower()))
                                                {
                                                    feedername = descRange.Cells[row2, 2].Value2.ToString();
                                                    feedername = feedername.ToLower();
                                                    break;
                                                }
                                                else if (feederidlow.Contains("mcb"))
                                                {
                                                    feedername = "MCB";
                                                }
                                            }
                                        }

                                        if (feederidlow == "cc")
                                        {
                                            cablechamberfound = true;
                                            if (height > 500)
                                            {
                                                feedername = "CABLE CHAMBER";
                                            }
                                            else
                                            {
                                                feedername = "cc";
                                            }
                                        }
                                        else if (feederidlow == "hbb" || feederidlow == "bb" || feederidlow == "vbb")
                                        {
                                            feedername = "BUSBAR CHAMBER";
                                            if (feederidlow == "vbb")
                                            {
                                                vbbfound = true;
                                            }

                                            if (row == selectedRange.Rows.Count - 2 && feederidlow == "hbb")
                                            {
                                                busbarheight = height;
                                                busbarposition = "bottom";
                                            }
                                            else if (feederidlow == "hbb")
                                            {
                                                busbarheight = height;
                                                busbarposition = "top";
                                            }

                                        }
                                        else if (feederidlow == "v1")
                                        {
                                            feedername = "V1";
                                        }
                                        else if (feederidlow.Contains("met"))
                                        {
                                            feedername = "METERING CHAMBER";
                                        }
                                        else if (feederidlow.Contains("inst"))
                                        {
                                            feedername = "INSTRUMENT CHAMBER";
                                            instrumentfound = true;
                                        }
                                        else if (feederidlow == "d" || feederidlow == "vacant")
                                        {
                                            feedername = "VACANT";
                                        }

                                        if (string.IsNullOrEmpty(feedername))
                                        {
                                            error = true;
                                            errorText += $"\nFeeder ID {feederid} not found in description range.";
                                        }

                                        string feedertext = "";
                                        if (feedername.Contains("METERING CHAMBER"))
                                        {
                                            feedertext = "METERING CHAMBER";
                                        }
                                        else if (feedername.Contains("INSTRUMENT CHAMBER"))
                                        {
                                            feedertext = "INSTRUMENT CHAMBER";
                                        }
                                        else if (feedername.Contains("VACANT"))
                                        {
                                            feedertext = "VACANT";
                                        }
                                        else if (feedername == "cc")
                                        {
                                            feedertext = "CABLE CHAMBER";
                                        }
                                        else if (feedername.Contains("mpcb"))
                                        {
                                            string[] mpcb = feedername.Split('a');
                                            if (mpcb.Length > 1)
                                            {
                                                feedertext += " " + $"{mpcb[0]}A MPCB";
                                            }
                                            else
                                            {
                                                feedertext += " " + "MPCB";
                                            }

                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "mccb", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                        }
                                        else if (feedername.Contains("mccb"))
                                        {
                                            if (feedername.Contains("dol") || feedername.Contains("sds"))
                                            {
                                                string[] mpcb = feedername.Split('a');
                                                if (mpcb.Length > 1)
                                                {
                                                    feedertext += " " + $"{mpcb[0]}A";
                                                }
                                                else
                                                {
                                                    Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                                    if (ampsMatch.Success)
                                                    {
                                                        feedertext = ampsMatch.Value.ToUpper();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                                if (ampsMatch.Success)
                                                {
                                                    feedertext = ampsMatch.Value.ToUpper();
                                                }
                                            }


                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }

                                            }

                                            feedertext += " " + "MCCB";

                                            if (feedername.Contains("630") || feedername.Contains("400") || feedername.Contains("320"))
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "DN3_MCCB", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }
                                            else
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "mccb", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }

                                        }
                                        else if (feedername.Contains("acb"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "ACB";
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "ACB_GA", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 10, 0), 1.0);
                                        }
                                        else if (feedername.Contains("rccb") || feedername.Contains("rcbo"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "RCBO";

                                            if (poleTypeMatch.Value.ToUpper() == "FP" || poleTypeMatch.Value.ToUpper() == "4P")
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "FP_RCCB", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }
                                        }
                                        else if (feedername.Contains("mcb"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "MCB";

                                            if (poleTypeMatch.Value.ToUpper() == "SP" || poleTypeMatch.Value.ToUpper() == "1P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 22.5;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "SP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "DP" || poleTypeMatch.Value.ToUpper() == "2P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 40;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "DP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "TP" || poleTypeMatch.Value.ToUpper() == "3P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 57.5;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "4P" || poleTypeMatch.Value.ToUpper() == "FP")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 75;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                        }

                                        if (feedername.Contains(" eb"))
                                        {
                                            feedertext += " " + "EB";
                                        }
                                        else if (feedername.Contains(" dg"))
                                        {
                                            feedertext += " " + "DG";
                                        }

                                        if (feedername.Contains("incomer") || feedername.Contains("incoming"))
                                        {
                                            feedertext += " " + "INCOMER";
                                        }
                                        else if (feedername.Contains("outgoing") || feedername.Contains("out"))
                                        {
                                            feedertext += " " + "O/G";
                                        }

                                        if (whichside == "rhs")
                                        {
                                            if (!feedername.Contains("CABLE CHAMBER") && !feedername.Contains("BUSBAR CHAMBER") && !feedername.Contains("V1") && !feedername.Contains("VACANT"))
                                            {
                                                feederfound = true;

                                                feederaddress.Add(new Point3d(topRight.X - 45, bottomLeft.Y + 25, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(topRight.X - 45, bottomLeft.Y + 60, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(topRight.X - 50, bottomLeft.Y + 60, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                MText feederText = new MText
                                                {
                                                    Location = new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 10, 0),
                                                    Height = 30,
                                                    TextHeight = 30,
                                                    Width = width - ((height <= 240) ? 167 : 200),
                                                    Contents = feedertext,
                                                    Attachment = AttachmentPoint.BottomCenter
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);


                                                if (!feederidlow.Contains("`"))
                                                {

                                                    Point3d meterpos = new Point3d(topRight.X - 80, topRight.Y - 130, 0);
                                                    Point3d lamppos = new Point3d(topRight.X - 80, topRight.Y - 42, 0);

                                                    if (feedername.Contains("mfm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "MFM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);

                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }


                                                    }
                                                    if (feedername.Contains("elr"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "ELR", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("vm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "VM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("am"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "AM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("ryb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RYB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("rg"))
                                                    {
                                                        if (feedername.Contains("rga"))
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGA", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }
                                                        else
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RG", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }

                                                    }
                                                    if (feedername.Contains("rgpb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGPB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("ss"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "SS", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                }

                                                Addrectangle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 10, topRight.Y - 40, 0), new Point3d(bottomLeft.X + 110, topRight.Y - 10, 0), 10);
                                                //Addrectangle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 12, bottomLeft.Y + 12, 0), new Point3d(bottomLeft.X + 132, bottomLeft.Y + 29, 0), 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 17, bottomLeft.Y + 21, 0), 3.5, 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 127, bottomLeft.Y + 21, 0), 3.5, 10);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }

                                            }

                                            if (feedername.Contains("CABLE CHAMBER"))
                                            {
                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }


                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 35,
                                                    TextString = feedername,
                                                    ColorIndex = 4,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);
                                            }
                                            else if (feedername == "VACANT")
                                            {
                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 25,
                                                    TextString = "VACANT",
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                feederfound = true;

                                                feederaddress.Add(new Point3d(topRight.X - 45, bottomLeft.Y + 25, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(topRight.X - 45, bottomLeft.Y + 60, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(topRight.X - 50, bottomLeft.Y + 60, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!feedername.Contains("CABLE CHAMBER") && !feedername.Contains("BUSBAR CHAMBER") && !feedername.Contains("V1") && !feedername.Contains("VACANT"))
                                            {
                                                feederfound = true;
                                                feederaddress.Add(new Point3d(bottomLeft.X + 45, bottomLeft.Y + 25, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(bottomLeft.X + 45, bottomLeft.Y + 60, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(bottomLeft.X + 50, bottomLeft.Y + 60, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                MText feederText = new MText
                                                {
                                                    Location = new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 10, 0),
                                                    Height = 30,
                                                    TextHeight = 30,
                                                    Width = width - ((height <= 240) ? 167 : 200),
                                                    Contents = feedertext,
                                                    Attachment = AttachmentPoint.BottomCenter
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                if (!feederidlow.Contains("`"))
                                                {


                                                    Point3d meterpos = new Point3d(bottomLeft.X + 80, topRight.Y - 130, 0);
                                                    Point3d lamppos = new Point3d(bottomLeft.X + 80, topRight.Y - 42, 0);
                                                    if (feedername.Contains("mfm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "MFM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("elr"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "ELR", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("vm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "VM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("am"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "AM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (!feedername.Contains("rga"))
                                                        {
                                                            if (lamppos.X == meterpos.X)
                                                            {
                                                                lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                            }
                                                        }
                                                    }
                                                    if (feedername.Contains("ryb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RYB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("rg"))
                                                    {
                                                        if (feedername.Contains("rga"))
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGA", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }
                                                        else
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RG", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }

                                                    }
                                                    if (feedername.Contains("rgpb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGPB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("ss"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "SS", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                }



                                                Addrectangle(transaction, blockTableRecord, new Point3d(topRight.X - 110, topRight.Y - 40, 0), new Point3d(topRight.X - 10, topRight.Y - 10, 0), 10);
                                                //Addrectangle(transaction, blockTableRecord, new Point3d(topRight.X - 132, topRight.Y - height + 12, 0), new Point3d(topRight.X - 12, topRight.Y - height + 29, 0), 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + width - 17, bottomLeft.Y + 21, 0), 3.5, 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + width - 127, bottomLeft.Y + 21, 0), 3.5, 10);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }
                                            }
                                            if (feedername.Contains("CABLE CHAMBER"))
                                            {
                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }


                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 35,
                                                    TextString = feedername,
                                                    ColorIndex = 4,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);
                                            }
                                            else if (feedername == "VACANT")
                                            {
                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 25,
                                                    TextString = "VACANT",
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                feederfound = true;
                                                feederaddress.Add(new Point3d(bottomLeft.X + 45, bottomLeft.Y + 25, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(bottomLeft.X + 45, bottomLeft.Y + 60, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(bottomLeft.X + 50, bottomLeft.Y + 60, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }
                                            }
                                        }

                                        if (feederfound)
                                        {
                                            if (!sidedoor)
                                            {
                                                if (feedernumbercol == 1)
                                                {
                                                    string linetypeName = "HIDDEN";
                                                    using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                    {
                                                        if (!linetypeTable.Has(linetypeName))
                                                        {
                                                            db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                        }
                                                    }
                                                    double depth1 = 0;
                                                    if (maxdepth < 1)
                                                    {
                                                        if (rearcabling)
                                                        {
                                                            depth1 = 600;
                                                        }
                                                        else
                                                        {
                                                            depth1 = 440;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        depth1 = maxdepth;
                                                    }
                                                    Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                    partitionline.ColorIndex = 150;
                                                    partitionline.Linetype = linetypeName;
                                                    blockTableRecord.AppendEntity(partitionline);
                                                    transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                    Polyline doorline = new Polyline(4);
                                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                    //doorline.ColorIndex = 10;
                                                    blockTableRecord.AppendEntity(doorline);
                                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                                    double offsetDistance2 = -2; // Negative value for inside offset
                                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                    foreach (Entity offsetEntity in offsetCurves2)
                                                    {
                                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                                        if (offsetPolyline != null)
                                                        {
                                                            blockTableRecord.AppendEntity(offsetPolyline);
                                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                        }
                                                    }

                                                    AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X - sidechannel, bottomLeft.Y, 0),
                                                    new Point3d(descPoint.X - sidechannel, topRight.Y, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                    blockTableRecord.AppendEntity(dimbase);
                                                    transaction.AddNewlyCreatedDBObject(dimbase, true);



                                                }

                                            }
                                        }

                                        if (feedername.Contains("BUSBAR CHAMBER"))
                                        {
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 20, bottomLeft.Y + 40, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 20, bottomLeft.Y + 40, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 20, topRight.Y - 40, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 20, topRight.Y - 40, 0), 1.0);

                                            if (height > 600)
                                            {
                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d(((bottomLeft.X + topRight.X) / 2) - 35, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 35,
                                                    TextString = feedername,
                                                    ColorIndex = 4,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(((bottomLeft.X + topRight.X) / 2) - 35, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 20, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 20, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);

                                                if (vbbfound)
                                                {
                                                    if (busbarposition == "bottom")
                                                    {
                                                        Polyline busbarview = new Polyline(2);
                                                        busbarview.AddVertexAt(0, new Point2d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y - (busbarheight / 2)), 0, 0, 0);
                                                        busbarview.AddVertexAt(1, new Point2d((bottomLeft.X + topRight.X) / 2, topRight.Y - 70), 0, 0, 0);
                                                        busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                        busbarview.SetStartWidthAt(0, 12.5);
                                                        busbarview.SetEndWidthAt(0, 12.5);
                                                        busbarview.SetStartWidthAt(1, 12.5);
                                                        busbarview.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarview);
                                                        transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                        Polyline busbarviewside = new Polyline(2);
                                                        busbarviewside.AddVertexAt(0, new Point2d(((bottomLeft.X + topRight.X) / 2) - 45, topRight.Y - 70), 0, 0, 0);
                                                        busbarviewside.AddVertexAt(1, new Point2d(((bottomLeft.X + topRight.X) / 2) + 45, topRight.Y - 70), 0, 0, 0);
                                                        busbarviewside.ColorIndex = 2;
                                                        busbarviewside.SetStartWidthAt(0, 12.5);
                                                        busbarviewside.SetEndWidthAt(0, 12.5);
                                                        busbarviewside.SetStartWidthAt(1, 12.5);
                                                        busbarviewside.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarviewside);
                                                        transaction.AddNewlyCreatedDBObject(busbarviewside, true);

                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBARJOINT", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y - (busbarheight / 2), 0), 1.0);


                                                        drawOrderTable.MoveToBottom(new ObjectIdCollection { busbarview.ObjectId });
                                                    }
                                                    else if (busbarposition == "top")
                                                    {
                                                        Polyline busbarview = new Polyline(2);
                                                        busbarview.AddVertexAt(0, new Point2d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarview.AddVertexAt(1, new Point2d((bottomLeft.X + topRight.X) / 2, topRight.Y + (busbarheight / 2)), 0, 0, 0);
                                                        busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                        busbarview.SetStartWidthAt(0, 12.5);
                                                        busbarview.SetEndWidthAt(0, 12.5);
                                                        busbarview.SetStartWidthAt(1, 12.5);
                                                        busbarview.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarview);
                                                        transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                        Polyline busbarviewside = new Polyline(2);
                                                        busbarviewside.AddVertexAt(0, new Point2d(((bottomLeft.X + topRight.X) / 2) - 45, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarviewside.AddVertexAt(1, new Point2d(((bottomLeft.X + topRight.X) / 2) + 45, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarviewside.ColorIndex = 2;
                                                        busbarviewside.SetStartWidthAt(0, 12.5);
                                                        busbarviewside.SetEndWidthAt(0, 12.5);
                                                        busbarviewside.SetStartWidthAt(1, 12.5);
                                                        busbarviewside.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarviewside);
                                                        transaction.AddNewlyCreatedDBObject(busbarviewside, true);

                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBARJOINT", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y + (busbarheight / 2), 0), 1.0);
                                                        drawOrderTable.MoveToBottom(new ObjectIdCollection { busbarview.ObjectId });
                                                    }
                                                    else
                                                    {
                                                        Polyline busbarview = new Polyline(2);
                                                        busbarview.AddVertexAt(0, new Point2d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarview.AddVertexAt(1, new Point2d((bottomLeft.X + topRight.X) / 2, topRight.Y - 70), 0, 0, 0);
                                                        busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                        busbarview.SetStartWidthAt(0, 12.5);
                                                        busbarview.SetEndWidthAt(0, 12.5);
                                                        busbarview.SetStartWidthAt(1, 12.5);
                                                        busbarview.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarview);
                                                        transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                        Polyline busbarviewside = new Polyline(2);
                                                        busbarviewside.AddVertexAt(0, new Point2d(((bottomLeft.X + topRight.X) / 2) - 45, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarviewside.AddVertexAt(1, new Point2d(((bottomLeft.X + topRight.X) / 2) + 45, bottomLeft.Y + 70), 0, 0, 0);
                                                        busbarviewside.ColorIndex = 2;
                                                        busbarviewside.SetStartWidthAt(0, 12.5);
                                                        busbarviewside.SetEndWidthAt(0, 12.5);
                                                        busbarviewside.SetStartWidthAt(1, 12.5);
                                                        busbarviewside.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarviewside);
                                                        transaction.AddNewlyCreatedDBObject(busbarviewside, true);

                                                        Polyline busbarviewside2 = new Polyline(2);
                                                        busbarviewside2.AddVertexAt(0, new Point2d(((bottomLeft.X + topRight.X) / 2) - 45, topRight.Y - 70), 0, 0, 0);
                                                        busbarviewside2.AddVertexAt(1, new Point2d(((bottomLeft.X + topRight.X) / 2) + 45, topRight.Y - 70), 0, 0, 0);
                                                        busbarviewside2.ColorIndex = 2;
                                                        busbarviewside2.SetStartWidthAt(0, 12.5);
                                                        busbarviewside2.SetEndWidthAt(0, 12.5);
                                                        busbarviewside2.SetStartWidthAt(1, 12.5);
                                                        busbarviewside2.SetEndWidthAt(1, 12.5);
                                                        blockTableRecord.AppendEntity(busbarviewside2);
                                                        transaction.AddNewlyCreatedDBObject(busbarviewside2, true);
                                                    }
                                                }



                                            }
                                            else if (height > 300)
                                            {
                                                if (width > 300)
                                                {
                                                    DBText feederText = new DBText
                                                    {
                                                        Position = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 45, 0),
                                                        Height = 45,
                                                        TextString = "H.B.C",
                                                        ColorIndex = 3,
                                                        HorizontalMode = TextHorizontalMode.TextCenter,
                                                        VerticalMode = TextVerticalMode.TextVerticalMid,
                                                        AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 45, 0)
                                                    };
                                                    blockTableRecord.AppendEntity(feederText);
                                                    transaction.AddNewlyCreatedDBObject(feederText, true);

                                                    DBText feederText2 = new DBText
                                                    {
                                                        Position = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) - 45, 0),
                                                        Height = 45,
                                                        TextString = "BUS",
                                                        ColorIndex = 3,
                                                        HorizontalMode = TextHorizontalMode.TextCenter,
                                                        VerticalMode = TextVerticalMode.TextVerticalMid,
                                                        AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) - 45, 0)
                                                    };
                                                    blockTableRecord.AppendEntity(feederText2);
                                                    transaction.AddNewlyCreatedDBObject(feederText2, true);
                                                }

                                                if (col == 1)
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X + 50, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                    Polyline busbarviewside = new Polyline(2);
                                                    busbarviewside.AddVertexAt(0, new Point2d(bottomLeft.X + 50, ((bottomLeft.Y + topRight.Y) / 2) + 45), 0, 0, 0);
                                                    busbarviewside.AddVertexAt(1, new Point2d(bottomLeft.X + 50, ((bottomLeft.Y + topRight.Y) / 2) - 45), 0, 0, 0);
                                                    busbarviewside.ColorIndex = 2;
                                                    busbarviewside.SetStartWidthAt(0, 12.5);
                                                    busbarviewside.SetEndWidthAt(0, 12.5);
                                                    busbarviewside.SetStartWidthAt(1, 12.5);
                                                    busbarviewside.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarviewside);
                                                    transaction.AddNewlyCreatedDBObject(busbarviewside, true);
                                                }
                                                else if (lastbusbar)
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X - 50, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                    Polyline busbarviewside = new Polyline(2);
                                                    busbarviewside.AddVertexAt(0, new Point2d(topRight.X - 50, ((bottomLeft.Y + topRight.Y) / 2) + 45), 0, 0, 0);
                                                    busbarviewside.AddVertexAt(1, new Point2d(topRight.X - 50, ((bottomLeft.Y + topRight.Y) / 2) - 45), 0, 0, 0);
                                                    busbarviewside.ColorIndex = 2;
                                                    busbarviewside.SetStartWidthAt(0, 12.5);
                                                    busbarviewside.SetEndWidthAt(0, 12.5);
                                                    busbarviewside.SetStartWidthAt(1, 12.5);
                                                    busbarviewside.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarviewside);
                                                    transaction.AddNewlyCreatedDBObject(busbarviewside, true);
                                                }
                                                else
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);
                                                }


                                                if (busbarposition == "bottom")
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBAR_IMG", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y - 45, 0), 1.0);
                                                }
                                                else if (busbarposition == "top")
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBAR_IMG", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 45, 0), 1.0);
                                                }

                                                if (!sidedoor)
                                                {
                                                    if (feedernumbercol == 1)
                                                    {
                                                        string linetypeName = "HIDDEN";
                                                        using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                        {
                                                            if (!linetypeTable.Has(linetypeName))
                                                            {
                                                                db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                            }
                                                        }
                                                        double depth1 = 0;
                                                        if (maxdepth < 1)
                                                        {
                                                            if (rearcabling)
                                                            {
                                                                depth1 = 600;
                                                            }
                                                            else
                                                            {
                                                                depth1 = 440;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            depth1 = maxdepth;
                                                        }
                                                        Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                        partitionline.ColorIndex = 150;
                                                        partitionline.Linetype = linetypeName;
                                                        blockTableRecord.AppendEntity(partitionline);
                                                        transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                        Polyline doorline = new Polyline(4);
                                                        doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                        //doorline.ColorIndex = 10;
                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }
                                                        AlignedDimension dimbase = new AlignedDimension(
                                                        new Point3d(descPoint.X - sidechannel, bottomLeft.Y, 0),
                                                        new Point3d(descPoint.X - sidechannel, topRight.Y, 0),
                                                        new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                        "", // Dimension text (Auto-generated)
                                                        db.Dimstyle // Use current dimension style
                                                        );

                                                        blockTableRecord.AppendEntity(dimbase);
                                                        transaction.AddNewlyCreatedDBObject(dimbase, true);
                                                    }

                                                }

                                            }
                                            else
                                            {
                                                if (width > 300)
                                                {
                                                    DBText feederText = new DBText
                                                    {
                                                        Position = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 25, 0),
                                                        Height = 30,
                                                        TextString = "H.B.C",
                                                        ColorIndex = 3,
                                                        HorizontalMode = TextHorizontalMode.TextCenter,
                                                        VerticalMode = TextVerticalMode.TextVerticalMid,
                                                        AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 30, 0)
                                                    };
                                                    blockTableRecord.AppendEntity(feederText);
                                                    transaction.AddNewlyCreatedDBObject(feederText, true);

                                                    DBText feederText2 = new DBText
                                                    {
                                                        Position = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) - 25, 0),
                                                        Height = 30,
                                                        TextString = "BUS",
                                                        ColorIndex = 3,
                                                        HorizontalMode = TextHorizontalMode.TextCenter,
                                                        VerticalMode = TextVerticalMode.TextVerticalMid,
                                                        AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) - 30, 0)
                                                    };
                                                    blockTableRecord.AppendEntity(feederText2);
                                                    transaction.AddNewlyCreatedDBObject(feederText2, true);
                                                }

                                                if (col == 1)
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X + 50, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                    Polyline busbarviewside = new Polyline(2);
                                                    busbarviewside.AddVertexAt(0, new Point2d(bottomLeft.X + 50, ((bottomLeft.Y + topRight.Y) / 2) + 45), 0, 0, 0);
                                                    busbarviewside.AddVertexAt(1, new Point2d(bottomLeft.X + 50, ((bottomLeft.Y + topRight.Y) / 2) - 45), 0, 0, 0);
                                                    busbarviewside.ColorIndex = 2;
                                                    busbarviewside.SetStartWidthAt(0, 12.5);
                                                    busbarviewside.SetEndWidthAt(0, 12.5);
                                                    busbarviewside.SetStartWidthAt(1, 12.5);
                                                    busbarviewside.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarviewside);
                                                    transaction.AddNewlyCreatedDBObject(busbarviewside, true);
                                                }
                                                else if (lastbusbar)
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X - 50, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);

                                                    Polyline busbarviewside = new Polyline(2);
                                                    busbarviewside.AddVertexAt(0, new Point2d(topRight.X - 50, ((bottomLeft.Y + topRight.Y) / 2) + 45), 0, 0, 0);
                                                    busbarviewside.AddVertexAt(1, new Point2d(topRight.X - 50, ((bottomLeft.Y + topRight.Y) / 2) - 45), 0, 0, 0);
                                                    busbarviewside.ColorIndex = 2;
                                                    busbarviewside.SetStartWidthAt(0, 12.5);
                                                    busbarviewside.SetEndWidthAt(0, 12.5);
                                                    busbarviewside.SetStartWidthAt(1, 12.5);
                                                    busbarviewside.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarviewside);
                                                    transaction.AddNewlyCreatedDBObject(busbarviewside, true);
                                                }
                                                else
                                                {
                                                    Polyline busbarview = new Polyline(2);
                                                    busbarview.AddVertexAt(0, new Point2d(bottomLeft.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.AddVertexAt(1, new Point2d(topRight.X, (bottomLeft.Y + topRight.Y) / 2), 0, 0, 0);
                                                    busbarview.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(153, 0, 0);
                                                    busbarview.SetStartWidthAt(0, 12.5);
                                                    busbarview.SetEndWidthAt(0, 12.5);
                                                    busbarview.SetStartWidthAt(1, 12.5);
                                                    busbarview.SetEndWidthAt(1, 12.5);
                                                    blockTableRecord.AppendEntity(busbarview);
                                                    transaction.AddNewlyCreatedDBObject(busbarview, true);
                                                }


                                                if (busbarposition == "bottom")
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBAR_IMG", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y - 35, 0), 1.0);
                                                }
                                                else if (busbarposition == "top")
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "BUSBAR_IMG", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 35, 0), 1.0);
                                                }


                                                if (!sidedoor)
                                                {
                                                    if (feedernumbercol == 1)
                                                    {
                                                        string linetypeName = "HIDDEN";
                                                        using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                        {
                                                            if (!linetypeTable.Has(linetypeName))
                                                            {
                                                                db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                            }
                                                        }
                                                        double depth1 = 0;
                                                        if (maxdepth < 1)
                                                        {
                                                            if (rearcabling)
                                                            {
                                                                depth1 = 600;
                                                            }
                                                            else
                                                            {
                                                                depth1 = 440;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            depth1 = maxdepth;
                                                        }
                                                        Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                        partitionline.ColorIndex = 150;
                                                        partitionline.Linetype = linetypeName;
                                                        blockTableRecord.AppendEntity(partitionline);
                                                        transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                        Polyline doorline = new Polyline(4);
                                                        doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                        //doorline.ColorIndex = 10;
                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }
                                                        AlignedDimension dimbase = new AlignedDimension(
                                                        new Point3d(descPoint.X - sidechannel, bottomLeft.Y, 0),
                                                        new Point3d(descPoint.X - sidechannel, topRight.Y, 0),
                                                        new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                        "", // Dimension text (Auto-generated)
                                                        db.Dimstyle // Use current dimension style
                                                        );

                                                        blockTableRecord.AppendEntity(dimbase);
                                                        transaction.AddNewlyCreatedDBObject(dimbase, true);
                                                    }

                                                }

                                            }

                                        }

                                        if (feedername == "V1")
                                        {
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 20, bottomLeft.Y + (height / 2), 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 20, bottomLeft.Y + (height / 2), 0), 1.0);


                                            DBText feederText = new DBText
                                            {
                                                Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                Height = 25,
                                                TextString = "V1",
                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                            };
                                            blockTableRecord.AppendEntity(feederText);
                                            transaction.AddNewlyCreatedDBObject(feederText, true);

                                            if (!sidedoor)
                                            {
                                                if (feedernumbercol == 1)
                                                {
                                                    string linetypeName = "HIDDEN";
                                                    using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                    {
                                                        if (!linetypeTable.Has(linetypeName))
                                                        {
                                                            db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                        }
                                                    }
                                                    double depth1 = 0;
                                                    if (maxdepth < 1)
                                                    {
                                                        if (rearcabling)
                                                        {
                                                            depth1 = 600;
                                                        }
                                                        else
                                                        {
                                                            depth1 = 440;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        depth1 = maxdepth;
                                                    }
                                                    Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                    partitionline.ColorIndex = 150;
                                                    partitionline.Linetype = linetypeName;
                                                    blockTableRecord.AppendEntity(partitionline);
                                                    transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                    Polyline doorline = new Polyline(4);
                                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                    //doorline.ColorIndex = 10;
                                                    blockTableRecord.AppendEntity(doorline);
                                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                                    double offsetDistance2 = -2; // Negative value for inside offset
                                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                    foreach (Entity offsetEntity in offsetCurves2)
                                                    {
                                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                                        if (offsetPolyline != null)
                                                        {
                                                            blockTableRecord.AppendEntity(offsetPolyline);
                                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                        }
                                                    }
                                                    AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X - sidechannel, bottomLeft.Y, 0),
                                                    new Point3d(descPoint.X - sidechannel, topRight.Y, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                    blockTableRecord.AppendEntity(dimbase);
                                                    transaction.AddNewlyCreatedDBObject(dimbase, true);
                                                }

                                            }

                                        }


                                        if (instrumentfound)
                                        {

                                            if (feedername.Contains("INSTRUMENT CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("inst", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }

                                                List<string> metersToPlace = new List<string>();
                                                Dictionary<string, string> blocks = new Dictionary<string, string>
                                            {
                                                { "mfm", "MFM" },
                                                { "elr", "ELR" },
                                                { "vm", "VM" },
                                                { "am", "AM" }
                                            };

                                                // Identify meters present in feedername
                                                foreach (var kvp in blocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        metersToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int meterCount = metersToPlace.Count;
                                                if (meterCount == 0) return;  // No meters found, exit

                                                // Calculate Center Position
                                                Point3d centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 100, 0);

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (meterCount == 1)
                                                {
                                                    meterPositions.Add(centerPos);
                                                }
                                                else if (meterCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X - 90, centerPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 90, centerPos.Y, 0)); // Right
                                                }
                                                else if (meterCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));       // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));  // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));  // Right
                                                }
                                                else if (meterCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - 110, 0));  // Below Center
                                                }
                                                else
                                                {
                                                    // First three in row, rest stack below center
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right

                                                    int yOffset = 110;
                                                    for (int i = 3; i < meterCount; i++)
                                                    {
                                                        meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - yOffset, 0));
                                                        yOffset += 110;
                                                    }
                                                }

                                                // Insert meters at calculated positions
                                                for (int i = 0; i < metersToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, metersToPlace[i], meterPositions[i], 1.0);
                                                }
                                            }
                                            if (feedername.Contains("METERING CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("met", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }
                                                List<string> lampsToPlace = new List<string>();
                                                Dictionary<string, string> lampBlocks = new Dictionary<string, string>
                                            {
                                                { "ryb", "RYB" },
                                                { "rga", "RGA" },
                                                { "rgpb", "RGPB" },
                                                { "ss", "SS" }
                                            };

                                                // Identify other lamps in feedername
                                                foreach (var kvp in lampBlocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        lampsToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int lampCount = lampsToPlace.Count;
                                                if (lampCount == 0) return;  // No lamps found, exit

                                                // Starting Position for Lamp Placement
                                                Point3d lampPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 80, 0);

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (lampCount == 1)
                                                {
                                                    meterPositions.Add(lampPos);
                                                }
                                                else if (lampCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                }
                                                else if (lampCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    meterPositions.Add(new Point3d(lampPos.X, lampPos.Y - 60, 0));  // Right
                                                }
                                                else if (lampCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y - 60, 0));  // Right
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y - 60, 0));  // Right
                                                }

                                                for (int i = 0; i < lampsToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, lampsToPlace[i], meterPositions[i], 1.0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (feedername.Contains("METERING CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("met", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }
                                                List<string> lampsToPlace = new List<string>();
                                                Dictionary<string, string> lampBlocks = new Dictionary<string, string>
                                            {
                                                { "ryb", "RYB" },
                                                { "rga", "RGA" },
                                                { "rgpb", "RGPB" },
                                                { "ss", "SS" }
                                            };

                                                // Identify other lamps in feedername
                                                foreach (var kvp in lampBlocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        lampsToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int lampCount = lampsToPlace.Count;

                                                // Starting Position for Lamp Placement
                                                Point3d lampPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 80, 0);

                                                // Positioning Logic
                                                List<Point3d> lampPositions = new List<Point3d>();

                                                if (lampCount == 1)
                                                {
                                                    lampPositions.Add(lampPos);
                                                }
                                                else if (lampCount == 2)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 80, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 80, lampPos.Y, 0)); // Right
                                                }
                                                else if (lampCount == 3)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 80, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 80, lampPos.Y, 0)); // Right
                                                    lampPositions.Add(new Point3d(lampPos.X, lampPos.Y - 60, 0));  // Right
                                                }
                                                else if (lampCount == 4)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 80, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 80, lampPos.Y, 0)); // Right
                                                    lampPositions.Add(new Point3d(lampPos.X - 80, lampPos.Y - 60, 0));  // Right
                                                    lampPositions.Add(new Point3d(lampPos.X + 80, lampPos.Y - 60, 0));  // Right
                                                }

                                                for (int i = 0; i < lampsToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, lampsToPlace[i], lampPositions[i], 1.0);
                                                }

                                                List<string> metersToPlace = new List<string>();
                                                Dictionary<string, string> blocks = new Dictionary<string, string>
                                            {
                                                { "mfm", "MFM" },
                                                { "elr", "ELR" },
                                                { "vm", "VM" },
                                                { "am", "AM" }
                                            };

                                                // Identify meters present in feedername
                                                foreach (var kvp in blocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        metersToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int meterCount = metersToPlace.Count;

                                                Point3d centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 100, 0);

                                                if (lampCount > 2)
                                                {
                                                    centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 215, 0);
                                                }
                                                else if (lampCount >= 1)
                                                {
                                                    centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 160, 0);
                                                }

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (meterCount == 1)
                                                {
                                                    meterPositions.Add(centerPos);
                                                }
                                                else if (meterCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X - 90, centerPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 90, centerPos.Y, 0)); // Right
                                                }
                                                else if (meterCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));       // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));  // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));  // Right
                                                }
                                                else if (meterCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - 110, 0));  // Below Center
                                                }
                                                else
                                                {
                                                    // First three in row, rest stack below center
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right

                                                    int yOffset = 110;
                                                    for (int i = 3; i < meterCount; i++)
                                                    {
                                                        meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - yOffset, 0));
                                                        yOffset += 110;
                                                    }
                                                }

                                                // Insert meters at calculated positions
                                                for (int i = 0; i < metersToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, metersToPlace[i], meterPositions[i], 1.0);
                                                }

                                            }
                                        }


                                    }
                                    else
                                    {
                                        MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                        return;
                                    }

                                    // Update startY for stacking rectangles
                                    startY += height;
                                    if (horizontallink)
                                    {
                                        width = previouswidth;
                                        horizontallink = false;
                                    }
                                }

                                if (feederfound)
                                {
                                    sidedoor = true;
                                    int feedernumberrow = 1;

                                    for (int i = feederaddress.Count - 1; i >= 0; i--)
                                    {
                                        Point3d point = feederaddress[i];
                                        DBText widthText = new DBText
                                        {
                                            Position = new Point3d(point.X, point.Y, 0),
                                            Height = 23,
                                            TextString = $"{feedernumbercol}F{feedernumberrow}",
                                            ColorIndex = 3,
                                            HorizontalMode = TextHorizontalMode.TextCenter,
                                            VerticalMode = TextVerticalMode.TextVerticalMid,
                                            AlignmentPoint = new Point3d(point.X, point.Y, 0)
                                        };
                                        blockTableRecord.AppendEntity(widthText);
                                        transaction.AddNewlyCreatedDBObject(widthText, true);
                                        feedernumberrow++;
                                    }
                                    feedernumbercol++;
                                }
                                else
                                {
                                    if (feederheights.Count > 0)
                                    {
                                        feederheights.RemoveAt(feederheights.Count - 1);
                                    }
                                }

                                if (col == 1)
                                {
                                    Point3d bottomLeft = new Point3d(startX - sidechannel, descPoint.Y, 0);
                                    Point3d topRight = new Point3d(startX, descPoint.Y + panelheight, 0);

                                    Polyline rectangle = new Polyline(4);
                                    rectangle.AddVertexAt(0, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(topRight.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(bottomLeft.X, topRight.Y), 0, 0, 0);
                                    rectangle.Closed = true;
                                    //rectangle.ColorIndex = 10;

                                    blockTableRecord.AppendEntity(rectangle);
                                    transaction.AddNewlyCreatedDBObject(rectangle, true);

                                    Hatch tophatch = new Hatch();
                                    tophatch.SetDatabaseDefaults();

                                    tophatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                    tophatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                    // Add hatch to drawing
                                    blockTableRecord.AppendEntity(tophatch);
                                    transaction.AddNewlyCreatedDBObject(tophatch, true);

                                    // Associate the hatch with the rectangle boundary
                                    ObjectIdCollection topboundaryIds = new ObjectIdCollection();
                                    topboundaryIds.Add(rectangle.ObjectId);
                                    tophatch.Associative = true;
                                    tophatch.AppendLoop(HatchLoopTypes.External, topboundaryIds);

                                    // Finalize the hatch
                                    tophatch.EvaluateHatch(true);

                                    drawOrderTable.MoveToBottom(new ObjectIdCollection { tophatch.ObjectId });

                                }
                                else if (col == selectedRange.Columns.Count)
                                {
                                    shippingcount++;
                                    Point3d bottomLeft = new Point3d(startX + width, descPoint.Y, 0);
                                    Point3d topRight = new Point3d(startX + width + sidechannel, descPoint.Y + panelheight, 0);
                                    Polyline rectangle = new Polyline(4);
                                    rectangle.AddVertexAt(0, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(topRight.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(bottomLeft.X, topRight.Y), 0, 0, 0);
                                    rectangle.Closed = true;
                                    //rectangle.ColorIndex = 10;
                                    blockTableRecord.AppendEntity(rectangle);
                                    transaction.AddNewlyCreatedDBObject(rectangle, true);

                                    Hatch sidehatch = new Hatch();
                                    sidehatch.SetDatabaseDefaults();

                                    sidehatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                    sidehatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                    // Add hatch to drawing
                                    blockTableRecord.AppendEntity(sidehatch);
                                    transaction.AddNewlyCreatedDBObject(sidehatch, true);

                                    // Associate the hatch with the rectangle boundary
                                    ObjectIdCollection sideboundaryIds = new ObjectIdCollection();
                                    sideboundaryIds.Add(rectangle.ObjectId);
                                    sidehatch.Associative = true;
                                    sidehatch.AppendLoop(HatchLoopTypes.External, sideboundaryIds);

                                    // Finalize the hatch
                                    sidehatch.EvaluateHatch(true);

                                    drawOrderTable.MoveToBottom(new ObjectIdCollection { sidehatch.ObjectId });

                                    Point3d bottomLeft1 = new Point3d(shippingleftX, descPoint.Y - bottomchannel, 0);
                                    Point3d topRight1 = new Point3d(shippingrigthX, descPoint.Y, 0);
                                    Polyline bottomrect = Addrectangle(transaction, blockTableRecord, bottomLeft1, topRight1, bottomchannelcolor);
                                    bottomrect.ColorIndex = 2;
                                    //Hatch bottomhatch = new Hatch();
                                    //bottomhatch.SetDatabaseDefaults();

                                    //bottomhatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                    //bottomhatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 254);

                                    //// Add hatch to drawing
                                    //blockTableRecord.AppendEntity(bottomhatch);
                                    //transaction.AddNewlyCreatedDBObject(bottomhatch, true);

                                    //// Associate the hatch with the rectangle boundary
                                    //ObjectIdCollection bottomboundaryIds = new ObjectIdCollection();
                                    //bottomboundaryIds.Add(bottomrect.ObjectId);
                                    //bottomhatch.Associative = true;
                                    //bottomhatch.AppendLoop(HatchLoopTypes.External, bottomboundaryIds);

                                    //// Finalize the hatch
                                    //bottomhatch.EvaluateHatch(true);

                                    //drawOrderTable.MoveToBottom(new ObjectIdCollection { bottomhatch.ObjectId });



                                    Point3d bottomLeft3 = new Point3d(shippingleftX, descPoint.Y + panelheight, 0);
                                    Point3d topRight3 = new Point3d(shippingrigthX, descPoint.Y + panelheight + topchannel, 0);
                                    Polyline toprect = Addrectangle(transaction, blockTableRecord, bottomLeft3, topRight3);

                                    Hatch tophatch = new Hatch();
                                    tophatch.SetDatabaseDefaults();

                                    tophatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                    tophatch.Color = Color.FromRgb(16, 86, 137); // Custom RGB color

                                    // Add hatch to drawing
                                    blockTableRecord.AppendEntity(tophatch);
                                    transaction.AddNewlyCreatedDBObject(tophatch, true);

                                    // Associate the hatch with the rectangle boundary
                                    ObjectIdCollection topboundaryIds = new ObjectIdCollection();
                                    topboundaryIds.Add(toprect.ObjectId);
                                    tophatch.Associative = true;
                                    tophatch.AppendLoop(HatchLoopTypes.External, topboundaryIds);

                                    // Finalize the hatch
                                    tophatch.EvaluateHatch(true);

                                    drawOrderTable.MoveToBottom(new ObjectIdCollection { tophatch.ObjectId });

                                    if (baseheight > 0)
                                    {
                                        Point3d basebottomLeft = new Point3d(shippingleftX, bottomLeft1.Y - baseheight, 0);
                                        Point3d basetopRight = new Point3d(shippingrigthX, bottomLeft1.Y, 0);
                                        Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                        Hatch hatch = new Hatch();
                                        hatch.SetDatabaseDefaults();

                                        hatch.PatternScale = 4.0;
                                        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                        hatch.Color = Color.FromRgb(204, 204, 204);
                                        // Add hatch to drawing
                                        blockTableRecord.AppendEntity(hatch);
                                        transaction.AddNewlyCreatedDBObject(hatch, true);

                                        // Associate the hatch with the rectangle boundary
                                        ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                        boundaryIds.Add(baserect.ObjectId);
                                        hatch.Associative = true;
                                        hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                        hatch.EvaluateHatch(true);

                                        drawOrderTable.MoveToBottom(new ObjectIdCollection { hatch.ObjectId });

                                        BlockReference bases = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TIBASE", new Point3d(shippingleftX + 35, descPoint.Y - bottomchannel - (baseheight / 2), 0), 1.0);

                                        int arraycount = (int)((shippingrigthX - shippingleftX - 65) / 14);

                                        var source = (BlockReference)transaction.GetObject(bases.ObjectId, OpenMode.ForWrite);
                                        var parameters = new AssocArrayRectangularParameters(14, 14, 0, arraycount, 1, 0, 0, 0);
                                        var vertexRef = new VertexRef(source.Position);
                                        var assocArray = AssocArray.CreateArray(new ObjectIdCollection { source.ObjectId }, vertexRef, parameters);
                                        var assocArrayBlock = (BlockReference)transaction.GetObject(assocArray.EntityId, OpenMode.ForWrite);
                                        assocArrayBlock.Position = source.Position;
                                        source.Erase();

                                        AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y - bottomchannel, 0),
                                                    new Point3d(descPoint.X, descPoint.Y - baseheight - bottomchannel, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                        blockTableRecord.AppendEntity(dimbase);
                                        transaction.AddNewlyCreatedDBObject(dimbase, true);
                                    }

                                    AlignedDimension dim2 = new AlignedDimension(
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + topchannel + 180, 0),
                                                    $"<>(SS-{shippingcount})", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dim2);
                                    transaction.AddNewlyCreatedDBObject(dim2, true);
                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING HOOK", new Point3d(shippingleftX + 78, descPoint.Y + panelheight, 0), 1.0);
                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING HOOK", new Point3d(shippingrigthX - 78, descPoint.Y + panelheight, 0), 1.0);
                                    BlockReference logo = InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOGO", new Point3d(descPoint.X + 130, descPoint.Y + panelheight + 35, 0), 1.0);
                                    drawOrderTable.MoveToTop(new ObjectIdCollection { logo.ObjectId });
                                    logo = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TI", new Point3d(descPoint.X + 550, descPoint.Y + panelheight + 35, 0), 1.0);
                                    drawOrderTable.MoveToTop(new ObjectIdCollection { logo.ObjectId });
                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOGO", new Point3d(shippingrigthX - 120, descPoint.Y + panelheight + 35, 0), 1.0);
                                    logo = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TI", new Point3d(shippingrigthX - 120, descPoint.Y + panelheight + 35, 0), 1.0);
                                    drawOrderTable.MoveToTop(new ObjectIdCollection { logo.ObjectId });

                                    AlignedDimension dim = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight + topchannel, 0),
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight + 270 + topchannel, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dim);
                                    transaction.AddNewlyCreatedDBObject(dim, true);

                                    AlignedDimension dimheight = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y - bottomchannel, 0),
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight, 0),
                                                    new Point3d(descPoint.X - 210, descPoint.Y + panelheight, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimheight);
                                    transaction.AddNewlyCreatedDBObject(dimheight, true);

                                    Point3d sidebottomleft = new Point3d(descPoint.X - 800 - maxdepth, descPoint.Y, 0);
                                    Point3d sidetopright = new Point3d(descPoint.X - 800, descPoint.Y + panelheight, 0);
                                    Addrectangle(transaction, blockTableRecord, sidebottomleft, sidetopright, 10);
                                    Polyline sidebottom = Addrectangle(transaction, blockTableRecord, new Point3d(sidebottomleft.X, descPoint.Y - bottomchannel, 0), new Point3d(sidetopright.X, descPoint.Y, 0), bottomchannelcolor);
                                    sidebottom.ColorIndex = 2;
                                    //Hatch sidebottomhatch = new Hatch();
                                    //sidebottomhatch.SetDatabaseDefaults();

                                    //sidebottomhatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID"); // Solid fill
                                    //sidebottomhatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 254);

                                    //// Add hatch to drawing
                                    //blockTableRecord.AppendEntity(sidebottomhatch);
                                    //transaction.AddNewlyCreatedDBObject(sidebottomhatch, true);

                                    //// Associate the hatch with the rectangle boundary
                                    //ObjectIdCollection sidebottomboundaryIds = new ObjectIdCollection();
                                    //sidebottomboundaryIds.Add(sidebottom.ObjectId);
                                    //sidebottomhatch.Associative = true;
                                    //sidebottomhatch.AppendLoop(HatchLoopTypes.External, sidebottomboundaryIds);

                                    //// Finalize the hatch
                                    //sidebottomhatch.EvaluateHatch(true);

                                    //drawOrderTable.MoveToBottom(new ObjectIdCollection { sidebottomhatch.ObjectId });
                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "TIBASE", new Point3d(sidebottomleft.X + 40, descPoint.Y - (bottomchannel / 2), 0), 1.0);

                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "HEADER", sidetopright, 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "SIDE_HOOK", new Point3d(sidebottomleft.X + 45, descPoint.Y + panelheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "SIDE_HOOK", new Point3d(sidetopright.X - 45, descPoint.Y + panelheight, 0), 1.0);

                                    if (baseheight > 0)
                                    {
                                        Point3d basebottomLeft = new Point3d(sidebottomleft.X, descPoint.Y - baseheight - bottomchannel, 0);
                                        Point3d basetopRight = new Point3d(sidetopright.X, descPoint.Y - bottomchannel, 0);
                                        Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                        Hatch hatch = new Hatch();
                                        hatch.SetDatabaseDefaults();

                                        hatch.PatternScale = 4.0;
                                        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                        hatch.Color = Color.FromRgb(204, 204, 204);
                                        // Add hatch to drawing
                                        blockTableRecord.AppendEntity(hatch);
                                        transaction.AddNewlyCreatedDBObject(hatch, true);

                                        // Associate the hatch with the rectangle boundary
                                        ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                        boundaryIds.Add(baserect.ObjectId);
                                        hatch.Associative = true;
                                        hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                        hatch.EvaluateHatch(true);

                                        drawOrderTable.MoveToBottom(new ObjectIdCollection { hatch.ObjectId });
                                    }

                                    Polyline doorline = new Polyline(4);
                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800 - maxdepth, descPoint.Y + 10), 0, 0, 0);
                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 - 20 - maxdepth, descPoint.Y + 10), 0, 0, 0);
                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 - 20 - maxdepth, descPoint.Y + panelheight - 10), 0, 0, 0);
                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800 - maxdepth, descPoint.Y + panelheight - 10), 0, 0, 0);
                                    //doorline.ColorIndex = 10;
                                    blockTableRecord.AppendEntity(doorline);
                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                    double offsetDistance2 = -2; // Negative value for inside offset
                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                    foreach (Entity offsetEntity in offsetCurves2)
                                    {
                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                        if (offsetPolyline != null)
                                        {
                                            blockTableRecord.AppendEntity(offsetPolyline);
                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                        }
                                    }

                                    AlignedDimension dimsideheight = new AlignedDimension(
                                                    new Point3d(descPoint.X - 800 - maxdepth, descPoint.Y - bottomchannel - baseheight, 0),
                                                    new Point3d(sidebottomleft.X, sidetopright.Y, 0),
                                                    new Point3d(sidebottomleft.X - 150, sidetopright.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimsideheight);
                                    transaction.AddNewlyCreatedDBObject(dimsideheight, true);

                                    AlignedDimension dimdepth = new AlignedDimension(
                                                    new Point3d(sidebottomleft.X, sidetopright.Y, 0),
                                                    new Point3d(sidetopright.X, sidetopright.Y, 0),
                                                    new Point3d(sidebottomleft.X, sidetopright.Y + 150, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimdepth);
                                    transaction.AddNewlyCreatedDBObject(dimdepth, true);

                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(descPoint.X - 1150 - maxdepth, descPoint.Y - bottomchannel - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(descPoint.X - 450, descPoint.Y - bottomchannel - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(shippingrigthX + 450, descPoint.Y - bottomchannel - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(descPoint.X - 500, descPoint.Y + (panelheight / 2), 0), 1.0);

                                    MText frontviewText = new MText
                                    {
                                        Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y - 230, 0),
                                        Height = 55,
                                        TextHeight = 55,
                                        Contents = @"\LFRONT VIEW",  // "\L" applies underline
                                        Attachment = AttachmentPoint.MiddleCenter
                                    };
                                    blockTableRecord.AppendEntity(frontviewText);
                                    transaction.AddNewlyCreatedDBObject(frontviewText, true);

                                    MText sideviewText = new MText
                                    {
                                        Location = new Point3d(descPoint.X - 800 - (maxdepth / 2), descPoint.Y - 230, 0),
                                        Height = 55,
                                        TextHeight = 55,
                                        Contents = @"\LSIDE VIEW",  // "\L" applies underline
                                        Attachment = AttachmentPoint.MiddleCenter
                                    };
                                    blockTableRecord.AppendEntity(sideviewText);
                                    transaction.AddNewlyCreatedDBObject(sideviewText, true);

                                    if (view == "TOPVIEW")
                                    {
                                        MText topviewText = new MText
                                        {
                                            Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y + panelheight + 600, 0),
                                            Height = 55,
                                            TextHeight = 55,
                                            Contents = @"\LTOP VIEW",  // "\L" applies underline
                                            Attachment = AttachmentPoint.MiddleCenter
                                        };
                                        blockTableRecord.AppendEntity(topviewText);
                                        transaction.AddNewlyCreatedDBObject(topviewText, true);

                                        BlockReference toptext1 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) - 450, descPoint.Y + panelheight + 550, 0), 1.0);
                                        BlockReference toptext2 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) + 450, descPoint.Y + panelheight + 550, 0), 1.0);
                                        toptext1.Rotation = -Math.PI / 2;
                                        toptext2.Rotation = -Math.PI / 2;
                                    }
                                    else if (view == "BOTTOMVIEW")
                                    {
                                        MText topviewText = new MText
                                        {
                                            Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y - maxdepth - 950, 0),
                                            Height = 55,
                                            TextHeight = 55,
                                            Contents = @"\LBOTTOM VIEW",  // "\L" applies underline
                                            Attachment = AttachmentPoint.MiddleCenter
                                        };
                                        blockTableRecord.AppendEntity(topviewText);
                                        transaction.AddNewlyCreatedDBObject(topviewText, true);

                                        BlockReference toptext1 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) - 450, descPoint.Y - maxdepth - 1000, 0), 1.0);
                                        BlockReference toptext2 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) + 450, descPoint.Y - maxdepth - 1000, 0), 1.0);
                                        toptext1.Rotation = -Math.PI / 2;
                                        toptext2.Rotation = -Math.PI / 2;
                                    }

                                    AlignedDimension dimrightsidebaseheight = new AlignedDimension(
                                                    new Point3d(shippingrigthX + sidechannel, descPoint.Y, 0),
                                                    new Point3d(shippingrigthX + sidechannel, descPoint.Y - bottomchannel, 0),
                                                    new Point3d(shippingrigthX + sidechannel + 90, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimrightsidebaseheight);
                                    transaction.AddNewlyCreatedDBObject(dimrightsidebaseheight, true);

                                    if (feederheights.Count > 0) // Ensure there is at least one list
                                    {
                                        List<double> lastList = feederheights[feederheights.Count - 1]; // Get the last list
                                        double previousY = descPoint.Y; // Start Y position

                                        foreach (double value in lastList)
                                        {
                                            // Create an aligned dimension moving upwards
                                            AlignedDimension dimrightsideheight = new AlignedDimension(
                                                new Point3d(shippingrigthX + sidechannel, previousY, 0),
                                                new Point3d(shippingrigthX + sidechannel, previousY + value, 0), // Increment Y-axis
                                                new Point3d(shippingrigthX + sidechannel + 90, previousY + (value / 2), 0), // Midpoint for dimension line
                                                "", // Auto-generated text
                                                db.Dimstyle // Use current dimension style
                                            );

                                            // Append to drawing
                                            blockTableRecord.AppendEntity(dimrightsideheight);
                                            transaction.AddNewlyCreatedDBObject(dimrightsideheight, true);

                                            // Update previous Y position for next dimension
                                            previousY += value;
                                        }
                                    }


                                }

                                startY = descPoint.Y;
                                startX += width;
                                width = 0.0;
                                vbbfound = false;
                                //feederfound = false;
                            }


                            transaction.Commit();

                            //StringBuilder message = new StringBuilder();
                            //for (int i = 0; i < feederheights.Count; i++)
                            //{
                            //    message.AppendLine($"List {i + 1}: {string.Join(", ", feederheights[i])}");
                            //}

                            //// Show message box with all lists and values
                            //MessageBox.Show(message.ToString(), "List of Lists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }


                    //acadDoc.SendStringToExecute("._ZOOM _EXTENTS ", true, false, false);

                    if (error)
                    {
                        MessageBox.Show(errorText);
                    }

                }

                catch (Exception ex)
                {
                    MessageBox.Show("\nError: " + ex.Message);
                }
                finally
                {
                    if (excelApp != null)
                    {
                        Marshal.ReleaseComObject(excelApp);
                    }
                }
            }
            else
            {
                try
                {
                    if (selectedRange == null || selectedRange.Cells.Count < 2)
                    {
                        MessageBox.Show("\nPlease select at least two cells in Excel.");
                        return;
                    }

                    Application.MainWindow.WindowState = Autodesk.AutoCAD.Windows.Window.State.Maximized;
                    Application.MainWindow.Focus();
                    // AutoCAD document setup
                    Document acadDoc = Application.DocumentManager.MdiActiveDocument;
                    Database db = acadDoc.Database;

                    // Load blocks from external DWG file
                    string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string cadFilePath = Path.Combine(pluginDirectory, "blocks.dwg");

                    if (!File.Exists(cadFilePath))
                    {
                        MessageBox.Show("\nBlocks DWG file not found.");
                        return;
                    }

                    //ImportBlocksFromDWG(db, cadFilePath);

                    using (Database sourceDb = new Database(false, true))
                    {
                        sourceDb.ReadDwgFile(cadFilePath, FileOpenMode.OpenForReadAndReadShare, false, null);

                        using (Transaction transaction = db.TransactionManager.StartTransaction())
                        {
                            BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord blockTableRecord = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                            double startX = descPoint.X;
                            double startY = descPoint.Y;
                            int basecolor = 4;
                            List<string> mergeaddress = new List<string>();
                            List<List<double>> feederheights = new List<List<double>>();

                            double shippingleftX = 0.0;
                            double shippingrigthX = 0.0;
                            double shippingcolor = 0.0;
                            double shippingcount = 0.0;
                            double panelheight = 0.0;
                            double feedernumbercol = 1;
                            double maxdepth = 0;
                            bool sidedoor = false;


                            // Check if "GaMeR" dimension style exists
                            DimStyleTable dimStyleTable = transaction.GetObject(db.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;
                            string dimStyleName = "GaMeR";
                            ObjectId dimStyleId;

                            if (!dimStyleTable.Has(dimStyleName))
                            {
                                DimStyleTableRecord newDimStyle = new DimStyleTableRecord
                                {
                                    Name = dimStyleName,

                                    // Set dimension style properties here
                                    Dimclrd = Color.FromColorIndex(ColorMethod.ByColor, 6),
                                    Dimclrt = Color.FromColorIndex(ColorMethod.ByColor, 3),
                                    Dimclre = Color.FromColorIndex(ColorMethod.ByColor, 6),
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

                                // Open the dim style table for write before adding
                                dimStyleTable.UpgradeOpen();

                                // Add the new dimension style to the table and transaction
                                dimStyleId = dimStyleTable.Add(newDimStyle);
                                transaction.AddNewlyCreatedDBObject(newDimStyle, true);


                            }
                            else
                            {
                                // If it already exists, get its ObjectId
                                dimStyleId = dimStyleTable[dimStyleName];
                            }

                            // Set the new dimension style as the current one
                            db.Dimstyle = dimStyleId;

                            for (int col = 1; col <= selectedRange.Columns.Count; col++) // Left to right
                            {
                                double width = 0.0;
                                bool horizontallink = false;
                                double previouswidth = 0.0;
                                bool feederfound = false;
                                bool vbbfound = false;
                                bool cablechamberfound = false;
                                List<Point3d> feederaddress = new List<Point3d>();
                                feederheights.Add(new List<double>());
                                string whichside = "";
                                bool instrumentfound = false;
                                double checkpanelheight = 0.0;
                                double depth = 0;

                                for (int row = selectedRange.Rows.Count; row >= 1; row--) // Bottom to top
                                {

                                    Excel.Range cell = selectedRange.Cells[row, col];
                                    double height = 0.0;
                                    string[] splitValues = null;

                                    if (cell.Value2?.ToString().ToLower() == "rhs")
                                    {
                                        whichside = "rhs";
                                        continue;
                                    }
                                    else if (cell.Value2?.ToString().ToLower() == "lhs")
                                    {
                                        whichside = "lhs";
                                        continue;
                                    }
                                    else if (cell.Value2?.ToString().ToLower() == "-")
                                    {
                                        continue;
                                    }

                                    if (cell.Interior.Color != 16777215)
                                    {

                                        if (row != selectedRange.Rows.Count - 1)
                                        {
                                            MessageBox.Show($"\nInterior color are only allowed in bottom cells for vertical width.");
                                            return;
                                        }

                                        //MessageBox.Show(cell.Font.Color.ToString());

                                        width = double.Parse(cell.Value2.ToString());
                                        previouswidth = width;

                                        if (shippingcolor == cell.Interior.Color)
                                        {
                                            shippingrigthX += width;
                                        }
                                        else
                                        {
                                            shippingcolor = cell.Interior.Color;

                                            if (col != 1)
                                            {
                                                shippingcount++;
                                                if (baseheight > 0)
                                                {
                                                    Point3d basebottomLeft = new Point3d(shippingleftX, descPoint.Y - baseheight, 0);
                                                    Point3d basetopRight = new Point3d(shippingrigthX, descPoint.Y, 0);
                                                    Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                                    Hatch hatch = new Hatch();
                                                    hatch.SetDatabaseDefaults();

                                                    hatch.PatternScale = 4.0;
                                                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "GOST_GROUND");
                                                    hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                                                    // Add hatch to drawing
                                                    blockTableRecord.AppendEntity(hatch);
                                                    transaction.AddNewlyCreatedDBObject(hatch, true);

                                                    // Associate the hatch with the rectangle boundary
                                                    ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                    boundaryIds.Add(baserect.ObjectId);
                                                    hatch.Associative = true;
                                                    hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                    hatch.EvaluateHatch(true);

                                                }

                                                AlignedDimension dim2 = new AlignedDimension(
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight, 0),
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + 150, 0),
                                                    $"<>(SS-{shippingcount})", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                blockTableRecord.AppendEntity(dim2);
                                                transaction.AddNewlyCreatedDBObject(dim2, true);
                                                //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING_HOOK", new Point3d(shippingleftX + 78, descPoint.Y + panelheight, 0), 1.0);
                                                //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING_HOOK", new Point3d(shippingrigthX - 78, descPoint.Y + panelheight, 0), 1.0);
                                                //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOGO", new Point3d(shippingleftX + 212, descPoint.Y + panelheight + 35, 0), 1.0);
                                            }
                                            shippingleftX = startX;
                                            shippingrigthX = startX + width;
                                        }

                                        AlignedDimension dim = new AlignedDimension(
                                            new Point3d(startX, descPoint.Y - baseheight, 0), // First point
                                            new Point3d(startX + width, descPoint.Y - baseheight, 0), // Second point
                                            new Point3d(startX, descPoint.Y - baseheight - 100, 0), // Dimension line position (50mm down)
                                            "", // Dimension text (Auto-generated)
                                            db.Dimstyle // Use current dimension style
                                            );

                                        blockTableRecord.AppendEntity(dim);
                                        transaction.AddNewlyCreatedDBObject(dim, true);

                                        continue;
                                    }

                                    if (cell.MergeCells)
                                    {
                                        if (mergeaddress.Contains(cell.MergeArea.Cells[1, 1].Address))
                                        {
                                            if (cell.MergeArea.Columns.Count > 1)
                                            {
                                                if (cell.Column != cell.MergeArea.Cells[1, 1].Column)
                                                {
                                                    Excel.Range firstCellInMerge = cell.MergeArea.Cells[1, 1];

                                                    string cellValue = firstCellInMerge.Value2.ToString();

                                                    string[] splitValues2 = cellValue.Split('#');

                                                    if (splitValues2.Length >= 2)
                                                    {
                                                        double leftCellValue = double.Parse(splitValues2[1]);
                                                        startY += leftCellValue;
                                                        checkpanelheight += leftCellValue;
                                                        if (feederheights.Count > 0)
                                                        {
                                                            feederheights[feederheights.Count - 1].Add(leftCellValue);
                                                        }
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        transaction.Commit();
                                                        MessageBox.Show($"\nInvalid cell format in {firstCellInMerge.Address}. Expected format: FEEDER ID # WIDTH");
                                                        return;
                                                    }
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                continue;
                                            }


                                        }
                                    }

                                    if (cell.MergeCells)
                                    {
                                        if (cell.Column == cell.MergeArea.Cells[1, 1].Column)
                                        {
                                            if (cell.MergeArea.Columns.Count > 1)
                                            {
                                                // Loop through all columns in the merged range
                                                for (int mergedCol = col + 1; mergedCol <= col + cell.MergeArea.Columns.Count - 1; mergedCol++)
                                                {
                                                    if (mergedCol > selectedRange.Columns.Count) // Ensure within the selected range
                                                        break;

                                                    Excel.Range rightCellBottom = selectedRange.Cells[selectedRange.Rows.Count - 1, mergedCol];

                                                    if (rightCellBottom.Interior.Color != 16777215) // Check if it's valid
                                                    {
                                                        double rightCellWidth = double.Parse(rightCellBottom.Value2.ToString());
                                                        width += rightCellWidth;
                                                        horizontallink = true;
                                                        //MessageBox.Show($"\nWidth from column {mergedCol}: {rightCellWidth}, Total Width: {width}");
                                                    }
                                                    else
                                                    {
                                                        transaction.Commit();
                                                        MessageBox.Show($"\nInvalid or missing width value in cell: {rightCellBottom.Address}.");
                                                        return;
                                                    }
                                                }
                                            }
                                            else
                                            {

                                            }

                                        }
                                    }

                                    if (cell.MergeCells)
                                    {
                                        Excel.Range cell2 = cell.MergeArea.Cells[1, 1];
                                        // Now check the value of the cell (either merged or not)
                                        if (cell2.Value2 != null)
                                        {
                                            mergeaddress.Add(cell2.Address);

                                            string cellValue = cell2.Value2.ToString(); // Get cell value as string

                                            // Split the string by '#' and check if it has at least 3 parts
                                            splitValues = cellValue.Split('#');

                                            if (splitValues.Length >= 2)
                                            {
                                                height = double.Parse(splitValues[1]);
                                                if (col == 1)
                                                {
                                                    panelheight += double.Parse(splitValues[1]);
                                                }
                                                checkpanelheight += double.Parse(splitValues[1]);

                                                if (feederheights.Count > 0)
                                                {
                                                    feederheights[feederheights.Count - 1].Add(height);
                                                }

                                                if (row == 2)
                                                {
                                                    if (checkpanelheight != panelheight)
                                                    {
                                                        transaction.Commit();
                                                        MessageBox.Show($"\nInvalid height value in column: {cell2.Address}. Expected height: {panelheight}");
                                                        return;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                transaction.Commit();
                                                MessageBox.Show($"\nInvalid cell format in {cell2.Address}. Expected format: FEEDER ID # WIDTH");
                                                return;
                                            }



                                        }
                                    }
                                    else if (cell.Value2 != null)
                                    {
                                        mergeaddress.Add(cell.Address);
                                        string cellValue = cell.Value2.ToString(); // Get cell value as string

                                        // Split the string by '#' and check if it has at least 3 parts
                                        splitValues = cellValue.Split('#');

                                        if (splitValues.Length >= 2)
                                        {
                                            height = double.Parse(splitValues[1]);
                                            if (col == 1)
                                            {
                                                panelheight += double.Parse(splitValues[1]);
                                            }
                                            checkpanelheight += double.Parse(splitValues[1]);

                                            if (feederheights.Count > 0)
                                            {
                                                feederheights[feederheights.Count - 1].Add(height);
                                            }

                                            if (row == 2)
                                            {
                                                if (checkpanelheight != panelheight)
                                                {
                                                    transaction.Commit();
                                                    MessageBox.Show($"\nInvalid height value in column: {cell.Address}. Expected height: {panelheight}");
                                                    return;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (row == 1)
                                            {
                                                if (checkpanelheight != panelheight)
                                                {
                                                    transaction.Commit();
                                                    Application.ShowAlertDialog($"\nInvalid height value in {col}th Vertical. Expected height: {panelheight}");
                                                    return;
                                                }
                                            }

                                            if (double.TryParse(cellValue, out double tempdepth))
                                            {
                                                if (row == 1)
                                                {
                                                    // FOR TOP AND BOTTOM VIEW
                                                    if (maxdepth < tempdepth)
                                                    {
                                                        maxdepth = tempdepth;
                                                    }
                                                    depth = tempdepth;

                                                    if (view == "TOPVIEW")
                                                    {
                                                        Point3d Bottomleft = new Point3d(startX, descPoint.Y + panelheight + 800, 0);
                                                        Point3d Topright = new Point3d(startX + width, descPoint.Y + panelheight + 800 + depth, 0);

                                                        if (col == 1)
                                                        {
                                                            AlignedDimension dimtopheight = new AlignedDimension(
                                                            new Point3d(Bottomleft.X, Bottomleft.Y, 0),
                                                            new Point3d(Bottomleft.X, Topright.Y, 0),
                                                            new Point3d(Bottomleft.X - 90, Bottomleft.Y, 0),
                                                            "", // Dimension text (Auto-generated)
                                                            db.Dimstyle // Use current dimension style
                                                            );

                                                            blockTableRecord.AppendEntity(dimtopheight);
                                                            transaction.AddNewlyCreatedDBObject(dimtopheight, true);
                                                        }

                                                        //Addrectangle(transaction, blockTableRecord, new Point3d(startX, panelheight + 600, 0), new Point3d(startX + width, panelheight + 600 + depth, 0));

                                                        Addrectangle(transaction, blockTableRecord, Bottomleft, Topright);
                                                        Polyline door = Addrectangle(transaction, blockTableRecord, new Point3d(Bottomleft.X + 30, Bottomleft.Y - 20, 0), new Point3d(Topright.X - 30, Bottomleft.Y, 0), 10);
                                                        if (whichside == "rhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(15 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Topright.X - 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);
                                                            Point3d startPoint = Bottomleft;
                                                            Point3d endPoint = door.GetPoint3dAt(3);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), -0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), -0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        else if (whichside == "lhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(345 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Bottomleft.X + 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);
                                                            Point3d startPoint = new Point3d(Topright.X, Bottomleft.Y, 0);
                                                            Point3d endPoint = door.GetPoint3dAt(2);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), 0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        Polyline doorline = new Polyline();
                                                        doorline.AddVertexAt(0, new Point2d(Bottomleft.X + 17, Topright.Y), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(Bottomleft.X + 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(Topright.X - 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(Topright.X - 17, Topright.Y), 0, 0, 0);

                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }

                                                        if (feederfound)
                                                        {
                                                            DBText widthText = new DBText
                                                            {
                                                                Position = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0),
                                                                Height = 40,
                                                                TextString = $"{feedernumbercol}F",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0)
                                                            };
                                                            blockTableRecord.AppendEntity(widthText);
                                                            transaction.AddNewlyCreatedDBObject(widthText, true);
                                                            DrawCircle(transaction, blockTableRecord, new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0), 45, 10);
                                                        }

                                                        if (cablechamberfound || (feederfound && rearcabling))
                                                        {
                                                            // FOR GLAND PLATE
                                                            Point3d glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - 350, 0);
                                                            Point3d glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);

                                                            if (maxdepth < 430)
                                                            {
                                                                glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - maxdepth + 100, 0);
                                                                glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);
                                                            }
                                                            // FOR GLAND PLATE
                                                            Polyline gland = new Polyline(4);
                                                            gland.AddVertexAt(0, new Point2d(glandTopright.X, glandTopright.Y), 0, 0, 0);
                                                            gland.AddVertexAt(1, new Point2d(glandTopright.X, glandBottomleft.Y), 0, 0, 0);
                                                            gland.AddVertexAt(2, new Point2d(((glandBottomleft.X + glandTopright.X) / 2) + 40, ((glandBottomleft.Y + glandTopright.Y) / 2) + 60), 0, 0, 0);
                                                            gland.AddVertexAt(3, new Point2d(glandBottomleft.X, glandTopright.Y), 0, 0, 0);
                                                            gland.Closed = true;
                                                            //gland.ColorIndex = 10;
                                                            blockTableRecord.AppendEntity(gland);
                                                            transaction.AddNewlyCreatedDBObject(gland, true);

                                                            Polyline glandrec = Addrectangle(transaction, blockTableRecord, glandBottomleft, glandTopright);

                                                            double textX = (glandBottomleft.X + glandTopright.X) / 2;
                                                            double textY = (glandBottomleft.Y + glandTopright.Y) / 2;

                                                            MText glandText = new MText
                                                            {
                                                                Location = new Point3d(textX - 5, textY - 20, 0), // Corrected midpoint calculation
                                                                Height = 30,
                                                                TextHeight = 30,
                                                                Width = 200,
                                                                Contents = "GLAND PLATE",
                                                                Attachment = AttachmentPoint.MiddleCenter // Better alignment
                                                            };

                                                            blockTableRecord.AppendEntity(glandText);
                                                            transaction.AddNewlyCreatedDBObject(glandText, true);

                                                            Hatch hatch = new Hatch();
                                                            hatch.SetDatabaseDefaults();

                                                            hatch.PatternScale = 2.5;
                                                            hatch.SetHatchPattern(HatchPatternType.PreDefined, "DASH");
                                                            hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(hatch);
                                                            transaction.AddNewlyCreatedDBObject(hatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                            boundaryIds.Add(gland.ObjectId);
                                                            hatch.Associative = true;
                                                            hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                            hatch.EvaluateHatch(false);


                                                        }
                                                        else if (vbbfound)
                                                        {
                                                            DBText feederText = new DBText
                                                            {
                                                                Position = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Height = 35,
                                                                TextString = "V.B.C",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                            };
                                                            blockTableRecord.AppendEntity(feederText);
                                                            transaction.AddNewlyCreatedDBObject(feederText, true);
                                                        }
                                                    }
                                                    else if (view == "BOTTOMVIEW")
                                                    {
                                                        Point3d Bottomleft = new Point3d(startX, descPoint.Y - 700 - depth, 0);
                                                        Point3d Topright = new Point3d(startX + width, descPoint.Y - 700, 0);

                                                        if (col == 1)
                                                        {
                                                            AlignedDimension dimtopheight = new AlignedDimension(
                                                            new Point3d(Bottomleft.X, Bottomleft.Y, 0),
                                                            new Point3d(Bottomleft.X, Topright.Y, 0),
                                                            new Point3d(Bottomleft.X - 90, Bottomleft.Y, 0),
                                                            "", // Dimension text (Auto-generated)
                                                            db.Dimstyle // Use current dimension style
                                                            );

                                                            blockTableRecord.AppendEntity(dimtopheight);
                                                            transaction.AddNewlyCreatedDBObject(dimtopheight, true);
                                                        }

                                                        Addrectangle(transaction, blockTableRecord, Bottomleft, Topright);
                                                        Polyline door = Addrectangle(transaction, blockTableRecord, new Point3d(Bottomleft.X + 30, Bottomleft.Y - 20, 0), new Point3d(Topright.X - 30, Bottomleft.Y, 0), 10);
                                                        if (whichside == "rhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(15 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Topright.X - 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);

                                                            Point3d startPoint = Bottomleft;
                                                            Point3d endPoint = door.GetPoint3dAt(3);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), -0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), -0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        else if (whichside == "lhs")
                                                        {
                                                            Matrix3d rotationMatrix = Matrix3d.Rotation(345 * (Math.PI / 180), Vector3d.ZAxis, new Point3d(Bottomleft.X + 30, Bottomleft.Y, 0));
                                                            door.TransformBy(rotationMatrix);

                                                            Point3d startPoint = new Point3d(Topright.X, Bottomleft.Y, 0);
                                                            Point3d endPoint = door.GetPoint3dAt(2);
                                                            Polyline bendPolyline = new Polyline();
                                                            bendPolyline.AddVertexAt(0, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0); // Start point
                                                            bendPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0.2, 0, 0); // Midpoint with bulge
                                                            bendPolyline.AddVertexAt(2, new Point2d(startPoint.X, startPoint.Y), 0.2, 0, 0); // End point
                                                            bendPolyline.ColorIndex = 2;

                                                            blockTableRecord.AppendEntity(bendPolyline);
                                                            transaction.AddNewlyCreatedDBObject(bendPolyline, true);
                                                        }
                                                        Polyline doorline = new Polyline();
                                                        doorline.AddVertexAt(0, new Point2d(Bottomleft.X + 17, Topright.Y), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(Bottomleft.X + 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(Topright.X - 17, Topright.Y + 20), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(Topright.X - 17, Topright.Y), 0, 0, 0);

                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }

                                                        if (feederfound)
                                                        {
                                                            DBText widthText = new DBText
                                                            {
                                                                Position = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0),
                                                                Height = 40,
                                                                TextString = $"{feedernumbercol}F",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0)
                                                            };
                                                            blockTableRecord.AppendEntity(widthText);
                                                            transaction.AddNewlyCreatedDBObject(widthText, true);
                                                            DrawCircle(transaction, blockTableRecord, new Point3d((Bottomleft.X + Topright.X) / 2, Bottomleft.Y + 90, 0), 45, 10);


                                                        }

                                                        if (cablechamberfound || (feederfound && rearcabling))
                                                        {
                                                            Point3d glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - 350, 0);
                                                            Point3d glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);

                                                            if (maxdepth < 430)
                                                            {
                                                                glandBottomleft = new Point3d(Bottomleft.X + 50, Topright.Y - maxdepth + 100, 0);
                                                                glandTopright = new Point3d(Topright.X - 50, Topright.Y - 50, 0);
                                                            }

                                                            Polyline gland = new Polyline(4);
                                                            gland.AddVertexAt(0, new Point2d(glandTopright.X, glandTopright.Y), 0, 0, 0);
                                                            gland.AddVertexAt(1, new Point2d(glandTopright.X, glandBottomleft.Y), 0, 0, 0);
                                                            gland.AddVertexAt(2, new Point2d(((glandBottomleft.X + glandTopright.X) / 2) + 40, ((glandBottomleft.Y + glandTopright.Y) / 2) + 60), 0, 0, 0);
                                                            gland.AddVertexAt(3, new Point2d(glandBottomleft.X, glandTopright.Y), 0, 0, 0);
                                                            gland.Closed = true;
                                                            //gland.ColorIndex = 10;
                                                            blockTableRecord.AppendEntity(gland);
                                                            transaction.AddNewlyCreatedDBObject(gland, true);

                                                            Polyline glandrec = Addrectangle(transaction, blockTableRecord, glandBottomleft, glandTopright);

                                                            double textX = (glandBottomleft.X + glandTopright.X) / 2;
                                                            double textY = (glandBottomleft.Y + glandTopright.Y) / 2;

                                                            MText glandText = new MText
                                                            {
                                                                Location = new Point3d(textX - 5, textY - 20, 0), // Corrected midpoint calculation
                                                                Height = 30,
                                                                TextHeight = 25,
                                                                Width = 200,
                                                                Contents = "GLAND PLATE",
                                                                Attachment = AttachmentPoint.MiddleCenter // Better alignment
                                                            };

                                                            blockTableRecord.AppendEntity(glandText);
                                                            transaction.AddNewlyCreatedDBObject(glandText, true);

                                                            Hatch hatch = new Hatch();
                                                            hatch.SetDatabaseDefaults();

                                                            hatch.PatternScale = 2.5;
                                                            hatch.SetHatchPattern(HatchPatternType.PreDefined, "DASH");
                                                            hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);

                                                            // Add hatch to drawing
                                                            blockTableRecord.AppendEntity(hatch);
                                                            transaction.AddNewlyCreatedDBObject(hatch, true);

                                                            // Associate the hatch with the rectangle boundary
                                                            ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                                            boundaryIds.Add(gland.ObjectId);
                                                            hatch.Associative = true;
                                                            hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                                            hatch.EvaluateHatch(false);

                                                        }
                                                        else if (vbbfound)
                                                        {
                                                            DBText feederText = new DBText
                                                            {
                                                                Position = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Height = 35,
                                                                TextString = "V.B.C",
                                                                ColorIndex = 4,
                                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                                AlignmentPoint = new Point3d(((Bottomleft.X + Topright.X) / 2), (Bottomleft.Y + Topright.Y) / 2, 0),
                                                                Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                            };
                                                            blockTableRecord.AppendEntity(feederText);
                                                            transaction.AddNewlyCreatedDBObject(feederText, true);
                                                        }


                                                    }


                                                    continue;
                                                }
                                                else
                                                {
                                                    transaction.Commit();
                                                    MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                transaction.Commit();
                                                MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                                return;
                                            }

                                        }

                                    }

                                    if (height == 0)
                                    {
                                        transaction.Commit();
                                        MessageBox.Show($"\nInvalid or missing height value in cell: {cell.Address}.");
                                        return;
                                    }

                                    if (width == 0)
                                    {
                                        transaction.Commit();
                                        MessageBox.Show($"\nInvalid or missing width value in cell: {cell.Address}.");
                                        return;
                                    }

                                    // Draw rectangle
                                    Point3d bottomLeft = new Point3d(startX, startY, 0);
                                    Point3d topRight = new Point3d(startX + width, startY + height, 0);

                                    Polyline rectangle = new Polyline(4);
                                    rectangle.AddVertexAt(0, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(topRight.X, bottomLeft.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(bottomLeft.X, topRight.Y), 0, 0, 0);
                                    rectangle.Closed = true;
                                    //rectangle.ColorIndex = 10;

                                    blockTableRecord.AppendEntity(rectangle);
                                    transaction.AddNewlyCreatedDBObject(rectangle, true);

                                    double offsetDistance = -10; // Negative value for inside offset
                                    DBObjectCollection offsetCurves = rectangle.GetOffsetCurves(offsetDistance);

                                    foreach (Entity offsetEntity in offsetCurves)
                                    {
                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                        if (offsetPolyline != null)
                                        {
                                            blockTableRecord.AppendEntity(offsetPolyline);
                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                        }
                                    }

                                    if (splitValues.Length >= 2)
                                    {
                                        string feederid = splitValues[0];
                                        string feederidlow = feederid.Replace(" ", "").ToLower();
                                        string feedername = "";

                                        for (int row2 = 1; row2 <= descRange.Rows.Count; row2++)
                                        {
                                            Excel.Range feederidcell = descRange.Cells[row2, 1];
                                            if (feederidcell.Value2 != null)
                                            {
                                                string feederidcellvalue = feederidcell.Value2.ToString();
                                                if (feederidlow.Contains(feederidcellvalue.Replace(" ", "").ToLower()))
                                                {
                                                    feedername = descRange.Cells[row2, 2].Value2.ToString();
                                                    feedername = feedername.ToLower();
                                                    break;
                                                }
                                            }
                                        }

                                        if (feederidlow == "cc")
                                        {
                                            cablechamberfound = true;
                                            if (height > 500)
                                            {
                                                feedername = "CABLE CHAMBER";
                                            }
                                            else
                                            {
                                                feedername = "cc";
                                            }
                                        }
                                        else if (feederidlow == "hbb" || feederidlow == "bb" || feederidlow == "vbb")
                                        {
                                            feedername = "BUSBAR CHAMBER";
                                            if (feederidlow == "vbb")
                                            {
                                                vbbfound = true;
                                            }


                                        }
                                        else if (feederidlow == "v1")
                                        {
                                            feedername = "V1";
                                        }
                                        else if (feederidlow.Contains("met"))
                                        {
                                            feedername = "METERING CHAMBER";
                                        }
                                        else if (feederidlow.Contains("inst"))
                                        {
                                            feedername = "INSTRUMENT CHAMBER";
                                            instrumentfound = true;
                                        }
                                        else if (feederidlow == "d" || feederidlow == "vacant")
                                        {
                                            feedername = "VACANT";
                                        }

                                        if (string.IsNullOrEmpty(feedername))
                                        {
                                            error = true;
                                            errorText += $"\nFeeder ID {feederid} not found in description range.";
                                        }

                                        string feedertext = "";
                                        if (feedername.Contains("METERING CHAMBER"))
                                        {
                                            feedertext = "METERING CHAMBER";
                                        }
                                        else if (feedername.Contains("INSTRUMENT CHAMBER"))
                                        {
                                            feedertext = "INSTRUMENT CHAMBER";
                                        }
                                        else if (feedername.Contains("VACANT"))
                                        {
                                            feedertext = "VACANT";
                                        }
                                        else if (feedername == "cc")
                                        {
                                            feedertext = "CABLE CHAMBER";
                                        }
                                        else if (feedername.Contains("mpcb"))
                                        {
                                            string[] mpcb = feedername.Split('a');
                                            if (mpcb.Length > 1)
                                            {
                                                feedertext += " " + $"{mpcb[0]}A MPCB";
                                            }
                                            else
                                            {
                                                feedertext += " " + "MPCB";
                                            }

                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "mccb", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                        }
                                        else if (feedername.Contains("mccb"))
                                        {
                                            if (feedername.Contains("dol") || feedername.Contains("sds"))
                                            {
                                                string[] mpcb = feedername.Split('a');
                                                if (mpcb.Length > 1)
                                                {
                                                    feedertext += " " + $"{mpcb[0]}A";
                                                }
                                                else
                                                {
                                                    Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                                    if (ampsMatch.Success)
                                                    {
                                                        feedertext = ampsMatch.Value.ToUpper();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                                if (ampsMatch.Success)
                                                {
                                                    feedertext = ampsMatch.Value.ToUpper();
                                                }
                                            }


                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "MCCB";

                                            if (feedername.Contains("630") || feedername.Contains("400") || feedername.Contains("320"))
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "DN3_MCCB", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }
                                            else
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "mccb", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }

                                        }
                                        else if (feedername.Contains("acb"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "ACB";
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "ACB_GA", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2) + 10, 0), 1.0);
                                        }
                                        else if (feedername.Contains("rccb") || feedername.Contains("rcbo"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "RCBO";

                                            if (poleTypeMatch.Value.ToUpper() == "FP" || poleTypeMatch.Value.ToUpper() == "4P")
                                            {
                                                InsertBlock(db, sourceDb, transaction, blockTableRecord, "FP_RCCB", new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                            }
                                        }
                                        else if (feedername.Contains("mcb"))
                                        {
                                            Match ampsMatch = Regex.Match(feedername, @"\d+ ?A", RegexOptions.IgnoreCase);
                                            if (ampsMatch.Success)
                                            {
                                                feedertext = ampsMatch.Value.ToUpper();
                                            }

                                            Match poleTypeMatch = Regex.Match(feedername, @"[SDTF1234]P[N]?", RegexOptions.IgnoreCase);
                                            if (poleTypeMatch.Success)
                                            {
                                                if (poleTypeMatch.Value.ToUpper() == "TPN")
                                                {
                                                    feedertext += " TP+N";
                                                }
                                                else if (poleTypeMatch.Value.ToUpper() == "4P")
                                                {
                                                    feedertext += " FP";
                                                }
                                                else
                                                {
                                                    feedertext += " " + poleTypeMatch.Value.ToUpper();
                                                }
                                            }

                                            feedertext += " " + "MCB";

                                            if (poleTypeMatch.Value.ToUpper() == "SP" || poleTypeMatch.Value.ToUpper() == "1P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 22.5;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "SP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "DP" || poleTypeMatch.Value.ToUpper() == "2P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 40;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "DP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "TP" || poleTypeMatch.Value.ToUpper() == "3P")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 57.5;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "TP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                            else if (poleTypeMatch.Value.ToUpper() == "4P" || poleTypeMatch.Value.ToUpper() == "FP")
                                            {
                                                int multiplier = 1; // Default to 1 if no multiplier is found
                                                bool rotateToVertical = false; // Flag to track if rotation is needed

                                                // Single regex to extract multiplier (xN) and detect 'v' at the end
                                                Match match = Regex.Match(feederidlow, @"x(\d+)(v)?", RegexOptions.IgnoreCase);

                                                if (match.Success)
                                                {
                                                    multiplier = int.Parse(match.Groups[1].Value); // Extract multiplier
                                                    rotateToVertical = match.Groups[2].Success;   // 'v' exists if Group[2] is matched
                                                }

                                                // Base position (center)
                                                Point3d basePosition = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0);

                                                // Spacing between blocks
                                                double spacing = 75;

                                                // List to store inserted block references for later rotation
                                                List<BlockReference> insertedBlocks = new List<BlockReference>();

                                                for (int i = 0; i < multiplier; i++)
                                                {
                                                    // Calculate horizontal position for centering
                                                    double offset = (multiplier == 1) ? 0 : ((i - (multiplier - 1) / 2.0) * spacing);

                                                    Point3d position = new Point3d(basePosition.X + offset, basePosition.Y, 0);

                                                    // Insert the "SP_MCB" block
                                                    BlockReference blockRef = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FP_MCB", position, 1.0);

                                                    if (blockRef != null)
                                                    {
                                                        insertedBlocks.Add(blockRef);
                                                    }
                                                }

                                                // If 'v' is found, rotate the entire group around its center
                                                if (rotateToVertical && insertedBlocks.Count > 0)
                                                {
                                                    // Compute the center of all blocks
                                                    double minX = insertedBlocks.Min(b => b.Position.X);
                                                    double maxX = insertedBlocks.Max(b => b.Position.X);
                                                    Point3d rotationCenter = new Point3d((minX + maxX) / 2, basePosition.Y, 0);

                                                    foreach (BlockReference block in insertedBlocks)
                                                    {
                                                        // Rotate each block around the group's center
                                                        Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, rotationCenter);
                                                        block.TransformBy(rotationMatrix);
                                                    }
                                                }
                                            }
                                        }

                                        if (feedername.Contains("eb"))
                                        {
                                            feedertext += " " + "EB";
                                        }
                                        else if (feedername.Contains("dg"))
                                        {
                                            feedertext += " " + "DG";
                                        }

                                        if (feedername.Contains("incomer") || feedername.Contains("incoming"))
                                        {
                                            feedertext += " " + "INCOMER";
                                        }
                                        else if (feedername.Contains("outgoing") || feedername.Contains("out"))
                                        {
                                            feedertext += " " + "O/G";
                                        }

                                        if (whichside == "rhs")
                                        {
                                            if (!feedername.Contains("CABLE CHAMBER") && !feedername.Contains("BUSBAR CHAMBER") && !feedername.Contains("V1") && !feedername.Contains("VACANT"))
                                            {
                                                feederfound = true;
                                                feederaddress.Add(new Point3d(topRight.X - 55, bottomLeft.Y + 35, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(topRight.X - 55, bottomLeft.Y + 70, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(topRight.X - 55, bottomLeft.Y + 70, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                MText feederText = new MText
                                                {
                                                    Location = new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 20, 0),
                                                    Height = 23,
                                                    TextHeight = 25,
                                                    Width = width - ((height <= 240) ? 167 : 200),
                                                    Contents = feedertext,
                                                    Attachment = AttachmentPoint.BottomCenter
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);


                                                if (!feederidlow.Contains("`"))
                                                {

                                                    Point3d meterpos = new Point3d(topRight.X - 75, topRight.Y - 80, 0);
                                                    Point3d lamppos = new Point3d(topRight.X - 75, topRight.Y - 42, 0);

                                                    if (feedername.Contains("mfm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "MFM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("elr"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "ELR", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("vm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "VM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("am"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "AM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("ryb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RYB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("rg"))
                                                    {
                                                        if (feedername.Contains("rga"))
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGA", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }
                                                        else
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RG", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }

                                                    }
                                                    if (feedername.Contains("rgpb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGPB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("ss"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "SS", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                }

                                                //Addrectangle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 20, topRight.Y - 50, 0), new Point3d(bottomLeft.X + 130, topRight.Y - 20, 0));
                                                //Addrectangle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 12, bottomLeft.Y + 12, 0), new Point3d(bottomLeft.X + 132, bottomLeft.Y + 29, 0), 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 17, bottomLeft.Y + 21, 0), 3.5, 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + 127, bottomLeft.Y + 21, 0), 3.5, 10);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }

                                            }

                                            if (feedername.Contains("CABLE CHAMBER"))
                                            {
                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }


                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 35,
                                                    TextString = feedername,
                                                    ColorIndex = 4,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);
                                            }
                                            else if (feedername == "VACANT")
                                            {
                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 25,
                                                    TextString = "VACANT",
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                feederfound = true;

                                                feederaddress.Add(new Point3d(topRight.X - 55, bottomLeft.Y + 35, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(topRight.X - 55, bottomLeft.Y + 70, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(topRight.X - 55, bottomLeft.Y + 70, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, bottomLeft.Y + height - 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(bottomLeft.X + 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }

                                            }
                                        }
                                        else
                                        {
                                            if (!feedername.Contains("CABLE CHAMBER") && !feedername.Contains("BUSBAR CHAMBER") && !feedername.Contains("V1") && !feedername.Contains("VACANT"))
                                            {
                                                feederfound = true;
                                                feederaddress.Add(new Point3d(bottomLeft.X + 55, bottomLeft.Y + 35, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(bottomLeft.X + 55, bottomLeft.Y + 70, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(bottomLeft.X + 55, bottomLeft.Y + 70, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                MText feederText = new MText
                                                {
                                                    Location = new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 20, 0),
                                                    Height = 23,
                                                    TextHeight = 25,
                                                    Width = width - ((height <= 240) ? 167 : 200),
                                                    Contents = feedertext,
                                                    Attachment = AttachmentPoint.BottomCenter
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                if (!feederidlow.Contains("`"))
                                                {


                                                    Point3d meterpos = new Point3d(bottomLeft.X + 75, topRight.Y - 80, 0);
                                                    Point3d lamppos = new Point3d(bottomLeft.X + 75, topRight.Y - 42, 0);
                                                    if (feedername.Contains("mfm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "MFM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("elr"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "ELR", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("vm"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "VM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("am"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "AM", meterpos, 1.0);
                                                        meterpos = new Point3d(meterpos.X, meterpos.Y - 110, 0);
                                                        if (lamppos.X == meterpos.X)
                                                        {
                                                            lamppos = new Point3d(topRight.X - (width / 2), lamppos.Y, 0);
                                                        }
                                                    }
                                                    if (feedername.Contains("ryb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RYB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("rg"))
                                                    {
                                                        if (feedername.Contains("rga"))
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGA", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }
                                                        else
                                                        {
                                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "RG", lamppos, 1.0);
                                                            lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                        }

                                                    }
                                                    if (feedername.Contains("rgpb"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "RGPB", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                    if (feedername.Contains("ss"))
                                                    {
                                                        InsertBlock(db, sourceDb, transaction, blockTableRecord, "SS", lamppos, 1.0);
                                                        lamppos = new Point3d(lamppos.X, lamppos.Y - 62, 0);
                                                    }
                                                }



                                                //Addrectangle(transaction, blockTableRecord, new Point3d(topRight.X - 120, topRight.Y - 50, 0), new Point3d(topRight.X - 20, topRight.Y - 20, 0));
                                                //Addrectangle(transaction, blockTableRecord, new Point3d(topRight.X - 132, topRight.Y - height + 12, 0), new Point3d(topRight.X - 12, topRight.Y - height + 29, 0), 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + width - 17, bottomLeft.Y + 21, 0), 3.5, 10);
                                                //DrawCircle(transaction, blockTableRecord, new Point3d(bottomLeft.X + width - 127, bottomLeft.Y + 21, 0), 3.5, 10);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }
                                            }
                                            if (feedername.Contains("CABLE CHAMBER"))
                                            {
                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }


                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 35,
                                                    TextString = feedername,
                                                    ColorIndex = 4,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Rotation = Math.PI / 2 // Rotate 90 degrees (upwards)
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);
                                            }
                                            else if (feedername == "VACANT")
                                            {
                                                DBText feederText = new DBText
                                                {
                                                    Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                    Height = 25,
                                                    TextString = "VACANT",
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                };
                                                blockTableRecord.AppendEntity(feederText);
                                                transaction.AddNewlyCreatedDBObject(feederText, true);

                                                feederfound = true;
                                                feederaddress.Add(new Point3d(bottomLeft.X + 55, bottomLeft.Y + 35, 0));

                                                DBText heightText = new DBText
                                                {
                                                    Position = new Point3d(bottomLeft.X + 55, bottomLeft.Y + 70, 0),
                                                    Height = 23,
                                                    TextString = $"M{(height / 100).ToString("0.0")}",
                                                    ColorIndex = 3,
                                                    HorizontalMode = TextHorizontalMode.TextCenter,
                                                    VerticalMode = TextVerticalMode.TextVerticalMid,
                                                    AlignmentPoint = new Point3d(bottomLeft.X + 55, bottomLeft.Y + 70, 0)
                                                };
                                                blockTableRecord.AppendEntity(heightText);
                                                transaction.AddNewlyCreatedDBObject(heightText, true);

                                                if (height > 600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else if (height > 250)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - 60, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, topRight.Y - height + 60, 0), 1.0);
                                                }
                                                else
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "CAM_LOCK", new Point3d(topRight.X - 35, (bottomLeft.Y + topRight.Y) / 2, 0), 1.0);
                                                }

                                            }
                                        }

                                        if (feederfound)
                                        {
                                            if (!sidedoor)
                                            {
                                                if (feedernumbercol == 1)
                                                {
                                                    string linetypeName = "HIDDEN";
                                                    using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                    {
                                                        if (!linetypeTable.Has(linetypeName))
                                                        {
                                                            db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                        }
                                                    }
                                                    double depth1 = 0;
                                                    if (maxdepth < 1)
                                                    {
                                                        if (rearcabling)
                                                        {
                                                            depth1 = 600;
                                                        }
                                                        else
                                                        {
                                                            depth1 = 440;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        depth1 = maxdepth;
                                                    }
                                                    Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                    partitionline.ColorIndex = 150;
                                                    partitionline.Linetype = linetypeName;
                                                    blockTableRecord.AppendEntity(partitionline);
                                                    transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                    Polyline doorline = new Polyline(4);
                                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                    //doorline.ColorIndex = 10;
                                                    blockTableRecord.AppendEntity(doorline);
                                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                                    double offsetDistance2 = -2; // Negative value for inside offset
                                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                    foreach (Entity offsetEntity in offsetCurves2)
                                                    {
                                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                                        if (offsetPolyline != null)
                                                        {
                                                            blockTableRecord.AppendEntity(offsetPolyline);
                                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                        }
                                                    }

                                                    AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X, bottomLeft.Y, 0),
                                                    new Point3d(descPoint.X, topRight.Y, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                    blockTableRecord.AppendEntity(dimbase);
                                                    transaction.AddNewlyCreatedDBObject(dimbase, true);



                                                }

                                            }
                                        }

                                        if (feedername.Contains("BUSBAR CHAMBER"))
                                        {
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 30, bottomLeft.Y + 45, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 30, bottomLeft.Y + 45, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 30, topRight.Y - 45, 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 30, topRight.Y - 45, 0), 1.0);

                                            if (height <= 500)
                                            {
                                                if (width <= 500)
                                                {
                                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(bottomLeft.X + 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(topRight.X - 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                }
                                                else if (width <= 1800)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(bottomLeft.X + 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(topRight.X - 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                }
                                                else
                                                {
                                                    double spacing = width / 4; // Dynamic spacing based on height

                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(bottomLeft.X + 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d(bottomLeft.X + spacing, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d(topRight.X - spacing, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d(topRight.X - 130, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                }

                                                if (!sidedoor)
                                                {
                                                    if (feedernumbercol == 1)
                                                    {
                                                        string linetypeName = "HIDDEN";
                                                        using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                        {
                                                            if (!linetypeTable.Has(linetypeName))
                                                            {
                                                                db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                            }
                                                        }
                                                        double depth1 = 0;
                                                        if (maxdepth < 1)
                                                        {
                                                            if (rearcabling)
                                                            {
                                                                depth1 = 600;
                                                            }
                                                            else
                                                            {
                                                                depth1 = 440;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            depth1 = maxdepth;
                                                        }
                                                        Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                        partitionline.ColorIndex = 150;
                                                        partitionline.Linetype = linetypeName;
                                                        blockTableRecord.AppendEntity(partitionline);
                                                        transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                        Polyline doorline = new Polyline(4);
                                                        doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                        doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                        doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                        //doorline.ColorIndex = 10;
                                                        blockTableRecord.AppendEntity(doorline);
                                                        transaction.AddNewlyCreatedDBObject(doorline, true);
                                                        double offsetDistance2 = -2; // Negative value for inside offset
                                                        DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                        foreach (Entity offsetEntity in offsetCurves2)
                                                        {
                                                            Polyline offsetPolyline = offsetEntity as Polyline;
                                                            if (offsetPolyline != null)
                                                            {
                                                                blockTableRecord.AppendEntity(offsetPolyline);
                                                                transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                            }
                                                        }
                                                        AlignedDimension dimbase = new AlignedDimension(
                                                        new Point3d(descPoint.X, bottomLeft.Y, 0),
                                                        new Point3d(descPoint.X, topRight.Y, 0),
                                                        new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                        "", // Dimension text (Auto-generated)
                                                        db.Dimstyle // Use current dimension style
                                                        );

                                                        blockTableRecord.AppendEntity(dimbase);
                                                        transaction.AddNewlyCreatedDBObject(dimbase, true);
                                                    }

                                                }

                                            }
                                            else
                                            {
                                                if (height <= 1600)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 100, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y - 100, 0), 1.0);
                                                }
                                                else
                                                {
                                                    double spacing = height / 4; // Dynamic spacing based on height

                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + 100, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d((bottomLeft.X + topRight.X) / 2, bottomLeft.Y + spacing, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, ((bottomLeft.Y + topRight.Y) / 2), 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "DANGER", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y - spacing, 0), 1.0);
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOUVER", new Point3d((bottomLeft.X + topRight.X) / 2, topRight.Y - 100, 0), 1.0);
                                                }
                                            }

                                        }

                                        if (feedername == "V1")
                                        {
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(bottomLeft.X + 30, bottomLeft.Y + (height / 2), 0), 1.0);
                                            InsertBlock(db, sourceDb, transaction, blockTableRecord, "BOLT", new Point3d(topRight.X - 30, bottomLeft.Y + (height / 2), 0), 1.0);


                                            DBText feederText = new DBText
                                            {
                                                Position = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                                Height = 25,
                                                TextString = "V1",
                                                HorizontalMode = TextHorizontalMode.TextCenter,
                                                VerticalMode = TextVerticalMode.TextVerticalMid,
                                                AlignmentPoint = new Point3d((bottomLeft.X + topRight.X) / 2, (bottomLeft.Y + topRight.Y) / 2, 0),
                                            };
                                            blockTableRecord.AppendEntity(feederText);
                                            transaction.AddNewlyCreatedDBObject(feederText, true);

                                            if (!sidedoor)
                                            {
                                                if (feedernumbercol == 1)
                                                {
                                                    string linetypeName = "HIDDEN";
                                                    using (LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead))
                                                    {
                                                        if (!linetypeTable.Has(linetypeName))
                                                        {
                                                            db.LoadLineTypeFile(linetypeName, "acad.lin"); // Load from AutoCAD's linetype file
                                                        }
                                                    }
                                                    double depth1 = 0;
                                                    if (maxdepth < 1)
                                                    {
                                                        if (rearcabling)
                                                        {
                                                            depth1 = 600;
                                                        }
                                                        else
                                                        {
                                                            depth1 = 440;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        depth1 = maxdepth;
                                                    }
                                                    Line partitionline = new Line(new Point3d(descPoint.X - 800, bottomLeft.Y, 0), new Point3d(descPoint.X - 800 - depth1, bottomLeft.Y, 0));
                                                    partitionline.ColorIndex = 150;
                                                    partitionline.Linetype = linetypeName;
                                                    blockTableRecord.AppendEntity(partitionline);
                                                    transaction.AddNewlyCreatedDBObject(partitionline, true);

                                                    Polyline doorline = new Polyline(4);
                                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 + 20, bottomLeft.Y + 10), 0, 0, 0);
                                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 + 20, topRight.Y - 10), 0, 0, 0);
                                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800, topRight.Y - 10), 0, 0, 0);
                                                    //doorline.ColorIndex = 10;
                                                    blockTableRecord.AppendEntity(doorline);
                                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                                    double offsetDistance2 = -2; // Negative value for inside offset
                                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                                    foreach (Entity offsetEntity in offsetCurves2)
                                                    {
                                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                                        if (offsetPolyline != null)
                                                        {
                                                            blockTableRecord.AppendEntity(offsetPolyline);
                                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                                        }
                                                    }
                                                    AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X, bottomLeft.Y, 0),
                                                    new Point3d(descPoint.X, topRight.Y, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                                    blockTableRecord.AppendEntity(dimbase);
                                                    transaction.AddNewlyCreatedDBObject(dimbase, true);
                                                }

                                            }

                                        }


                                        if (instrumentfound)
                                        {

                                            if (feedername.Contains("INSTRUMENT CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("inst", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }

                                                List<string> metersToPlace = new List<string>();
                                                Dictionary<string, string> blocks = new Dictionary<string, string>
                                            {
                                                { "mfm", "MFM" },
                                                { "elr", "ELR" },
                                                { "vm", "VM" },
                                                { "am", "AM" }
                                            };

                                                // Identify meters present in feedername
                                                foreach (var kvp in blocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        metersToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int meterCount = metersToPlace.Count;
                                                if (meterCount == 0) return;  // No meters found, exit

                                                // Calculate Center Position
                                                Point3d centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 110, 0);

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (meterCount == 1)
                                                {
                                                    meterPositions.Add(centerPos);
                                                }
                                                else if (meterCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X - 90, centerPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 90, centerPos.Y, 0)); // Right
                                                }
                                                else if (meterCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));       // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));  // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));  // Right
                                                }
                                                else if (meterCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - 110, 0));  // Below Center
                                                }
                                                else
                                                {
                                                    // First three in row, rest stack below center
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right

                                                    int yOffset = 110;
                                                    for (int i = 3; i < meterCount; i++)
                                                    {
                                                        meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - yOffset, 0));
                                                        yOffset += 110;
                                                    }
                                                }

                                                // Insert meters at calculated positions
                                                for (int i = 0; i < metersToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, metersToPlace[i], meterPositions[i], 1.0);
                                                }
                                            }
                                            if (feedername.Contains("METERING CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("met", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }
                                                List<string> lampsToPlace = new List<string>();
                                                Dictionary<string, string> lampBlocks = new Dictionary<string, string>
                                            {
                                                { "ryb", "RYB" },
                                                { "rga", "RGA" },
                                                { "rgpb", "RGPB" },
                                                { "ss", "SS" }
                                            };

                                                // Identify other lamps in feedername
                                                foreach (var kvp in lampBlocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        lampsToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int lampCount = lampsToPlace.Count;
                                                if (lampCount == 0) return;  // No lamps found, exit

                                                // Starting Position for Lamp Placement
                                                Point3d lampPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 90, 0);

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (lampCount == 1)
                                                {
                                                    meterPositions.Add(lampPos);
                                                }
                                                else if (lampCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                }
                                                else if (lampCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    meterPositions.Add(new Point3d(lampPos.X, lampPos.Y - 60, 0));  // Right
                                                }
                                                else if (lampCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    meterPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y - 60, 0));  // Right
                                                    meterPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y - 60, 0));  // Right
                                                }

                                                for (int i = 0; i < lampsToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, lampsToPlace[i], meterPositions[i], 1.0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (feedername.Contains("METERING CHAMBER"))
                                            {
                                                if (double.TryParse(feederidlow.Replace("met", ""), out double rownum))
                                                {
                                                    feedername = descRange.Cells[rownum, 2].Value2?.ToString().ToLower();
                                                }
                                                List<string> lampsToPlace = new List<string>();
                                                Dictionary<string, string> lampBlocks = new Dictionary<string, string>
                                            {
                                                { "ryb", "RYB" },
                                                { "rga", "RGA" },
                                                { "rgpb", "RGPB" },
                                                { "ss", "SS" }
                                            };

                                                // Identify other lamps in feedername
                                                foreach (var kvp in lampBlocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        lampsToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int lampCount = lampsToPlace.Count;

                                                // Starting Position for Lamp Placement
                                                Point3d lampPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 90, 0);

                                                // Positioning Logic
                                                List<Point3d> lampPositions = new List<Point3d>();

                                                if (lampCount == 1)
                                                {
                                                    lampPositions.Add(lampPos);
                                                }
                                                else if (lampCount == 2)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                }
                                                else if (lampCount == 3)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    lampPositions.Add(new Point3d(lampPos.X, lampPos.Y - 60, 0));  // Right
                                                }
                                                else if (lampCount == 4)
                                                {
                                                    lampPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y, 0)); // Left
                                                    lampPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y, 0)); // Right
                                                    lampPositions.Add(new Point3d(lampPos.X - 75, lampPos.Y - 60, 0));  // Right
                                                    lampPositions.Add(new Point3d(lampPos.X + 75, lampPos.Y - 60, 0));  // Right
                                                }

                                                for (int i = 0; i < lampsToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, lampsToPlace[i], lampPositions[i], 1.0);
                                                }

                                                List<string> metersToPlace = new List<string>();
                                                Dictionary<string, string> blocks = new Dictionary<string, string>
                                            {
                                                { "mfm", "MFM" },
                                                { "elr", "ELR" },
                                                { "vm", "VM" },
                                                { "am", "AM" }
                                            };

                                                // Identify meters present in feedername
                                                foreach (var kvp in blocks)
                                                {
                                                    if (feedername.Contains(kvp.Key))
                                                    {
                                                        metersToPlace.Add(kvp.Value);
                                                    }
                                                }

                                                int meterCount = metersToPlace.Count;

                                                Point3d centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 100, 0);

                                                if (lampCount > 2)
                                                {
                                                    centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 225, 0);
                                                }
                                                else if (lampCount >= 1)
                                                {
                                                    centerPos = new Point3d(bottomLeft.X + (width / 2), topRight.Y - 170, 0);
                                                }

                                                // Positioning Logic
                                                List<Point3d> meterPositions = new List<Point3d>();

                                                if (meterCount == 1)
                                                {
                                                    meterPositions.Add(centerPos);
                                                }
                                                else if (meterCount == 2)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X - 90, centerPos.Y, 0)); // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 90, centerPos.Y, 0)); // Right
                                                }
                                                else if (meterCount == 3)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));       // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));  // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));  // Right
                                                }
                                                else if (meterCount == 4)
                                                {
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - 110, 0));  // Below Center
                                                }
                                                else
                                                {
                                                    // First three in row, rest stack below center
                                                    meterPositions.Add(new Point3d(centerPos.X, centerPos.Y, 0));        // Center
                                                    meterPositions.Add(new Point3d(centerPos.X - 110, centerPos.Y, 0));   // Left
                                                    meterPositions.Add(new Point3d(centerPos.X + 110, centerPos.Y, 0));   // Right

                                                    int yOffset = 110;
                                                    for (int i = 3; i < meterCount; i++)
                                                    {
                                                        meterPositions.Add(new Point3d(centerPos.X, centerPos.Y - yOffset, 0));
                                                        yOffset += 110;
                                                    }
                                                }

                                                // Insert meters at calculated positions
                                                for (int i = 0; i < metersToPlace.Count; i++)
                                                {
                                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, metersToPlace[i], meterPositions[i], 1.0);
                                                }

                                            }
                                        }


                                    }
                                    else
                                    {
                                        MessageBox.Show($"\nInvalid cell format in {cell.Address}. Expected format: FEEDER ID # WIDTH");
                                        return;
                                    }

                                    // Update startY for stacking rectangles
                                    startY += height;
                                    if (horizontallink)
                                    {
                                        width = previouswidth;
                                        horizontallink = false;
                                    }
                                }

                                if (feederfound)
                                {
                                    sidedoor = true;
                                    int feedernumberrow = 1;
                                    for (int i = feederaddress.Count - 1; i >= 0; i--)
                                    {
                                        Point3d point = feederaddress[i];
                                        DBText widthText = new DBText
                                        {
                                            Position = new Point3d(point.X, point.Y, 0),
                                            Height = 23,
                                            TextString = $"{feedernumbercol}F{feedernumberrow}",
                                            ColorIndex = 3,
                                            HorizontalMode = TextHorizontalMode.TextCenter,
                                            VerticalMode = TextVerticalMode.TextVerticalMid,
                                            AlignmentPoint = new Point3d(point.X, point.Y, 0)
                                        };
                                        blockTableRecord.AppendEntity(widthText);
                                        transaction.AddNewlyCreatedDBObject(widthText, true);
                                        feedernumberrow++;
                                    }
                                    feedernumbercol++;
                                }
                                else
                                {
                                    if (feederheights.Count > 0)
                                    {
                                        feederheights.RemoveAt(feederheights.Count - 1);
                                    }
                                }

                                if (col == 1)
                                {

                                }
                                else if (col == selectedRange.Columns.Count)
                                {
                                    shippingcount++;
                                    if (baseheight > 0)
                                    {
                                        Point3d basebottomLeft = new Point3d(shippingleftX, descPoint.Y - baseheight, 0);
                                        Point3d basetopRight = new Point3d(shippingrigthX, descPoint.Y, 0);
                                        Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                        Hatch hatch = new Hatch();
                                        hatch.SetDatabaseDefaults();

                                        hatch.PatternScale = 4.0;
                                        hatch.SetHatchPattern(HatchPatternType.PreDefined, "GOST_GROUND");
                                        hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                                        // Add hatch to drawing
                                        blockTableRecord.AppendEntity(hatch);
                                        transaction.AddNewlyCreatedDBObject(hatch, true);

                                        // Associate the hatch with the rectangle boundary
                                        ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                        boundaryIds.Add(baserect.ObjectId);
                                        hatch.Associative = true;
                                        hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                        hatch.EvaluateHatch(true);

                                        AlignedDimension dimbase = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y, 0),
                                                    new Point3d(descPoint.X, descPoint.Y - baseheight, 0),
                                                    new Point3d(descPoint.X - 120, descPoint.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                        blockTableRecord.AppendEntity(dimbase);
                                        transaction.AddNewlyCreatedDBObject(dimbase, true);

                                    }
                                    AlignedDimension dim2 = new AlignedDimension(
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight, 0),
                                                    new Point3d(shippingleftX, descPoint.Y + panelheight + 150, 0),
                                                    $"<>(SS-{shippingcount})", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dim2);
                                    transaction.AddNewlyCreatedDBObject(dim2, true);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING_HOOK", new Point3d(shippingleftX + 78, descPoint.Y + panelheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "LIFTING_HOOK", new Point3d(shippingrigthX - 78, descPoint.Y + panelheight, 0), 1.0);
                                    //InsertBlock(db, sourceDb, transaction, blockTableRecord, "LOGO", new Point3d(shippingleftX + 212, descPoint.Y + panelheight + 35, 0), 1.0);

                                    AlignedDimension dim = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight, 0),
                                                    new Point3d(shippingrigthX, descPoint.Y + panelheight, 0),
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight + 300, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dim);
                                    transaction.AddNewlyCreatedDBObject(dim, true);

                                    AlignedDimension dimheight = new AlignedDimension(
                                                    new Point3d(descPoint.X, descPoint.Y, 0),
                                                    new Point3d(descPoint.X, descPoint.Y + panelheight, 0),
                                                    new Point3d(descPoint.X - 220, descPoint.Y + panelheight, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimheight);
                                    transaction.AddNewlyCreatedDBObject(dimheight, true);

                                    Point3d sidebottomleft = new Point3d(descPoint.X - 800 - maxdepth, descPoint.Y, 0);
                                    Point3d sidetopright = new Point3d(descPoint.X - 800, descPoint.Y + panelheight, 0);
                                    Addrectangle(transaction, blockTableRecord, sidebottomleft, sidetopright);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "SIDE_HOOK", new Point3d(sidebottomleft.X + 45, descPoint.Y + panelheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "SIDE_HOOK", new Point3d(sidetopright.X - 45, descPoint.Y + panelheight, 0), 1.0);
                                    if (baseheight > 0)
                                    {
                                        Point3d basebottomLeft = new Point3d(sidebottomleft.X, descPoint.Y - baseheight, 0);
                                        Point3d basetopRight = new Point3d(sidetopright.X, descPoint.Y, 0);
                                        Polyline baserect = Addrectangle(transaction, blockTableRecord, basebottomLeft, basetopRight, basecolor);

                                        Hatch hatch = new Hatch();
                                        hatch.SetDatabaseDefaults();

                                        hatch.PatternScale = 4.0;
                                        hatch.SetHatchPattern(HatchPatternType.PreDefined, "GOST_GROUND");
                                        hatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                                        // Add hatch to drawing
                                        blockTableRecord.AppendEntity(hatch);
                                        transaction.AddNewlyCreatedDBObject(hatch, true);

                                        // Associate the hatch with the rectangle boundary
                                        ObjectIdCollection boundaryIds = new ObjectIdCollection();
                                        boundaryIds.Add(baserect.ObjectId);
                                        hatch.Associative = true;
                                        hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                        hatch.EvaluateHatch(true);
                                    }

                                    Polyline doorline = new Polyline();
                                    doorline.AddVertexAt(0, new Point2d(descPoint.X - 800 - maxdepth, descPoint.Y + 10), 0, 0, 0);
                                    doorline.AddVertexAt(1, new Point2d(descPoint.X - 800 - 20 - maxdepth, descPoint.Y + 10), 0, 0, 0);
                                    doorline.AddVertexAt(2, new Point2d(descPoint.X - 800 - 20 - maxdepth, descPoint.Y + panelheight - 10), 0, 0, 0);
                                    doorline.AddVertexAt(3, new Point2d(descPoint.X - 800 - maxdepth, descPoint.Y + panelheight - 10), 0, 0, 0);

                                    blockTableRecord.AppendEntity(doorline);
                                    transaction.AddNewlyCreatedDBObject(doorline, true);
                                    double offsetDistance2 = -2; // Negative value for inside offset
                                    DBObjectCollection offsetCurves2 = doorline.GetOffsetCurves(offsetDistance2);

                                    foreach (Entity offsetEntity in offsetCurves2)
                                    {
                                        Polyline offsetPolyline = offsetEntity as Polyline;
                                        if (offsetPolyline != null)
                                        {
                                            blockTableRecord.AppendEntity(offsetPolyline);
                                            transaction.AddNewlyCreatedDBObject(offsetPolyline, true);
                                        }
                                    }
                                    AlignedDimension dimsideheight = new AlignedDimension(
                                                    new Point3d(sidebottomleft.X, sidebottomleft.Y - baseheight, 0),
                                                    new Point3d(sidebottomleft.X, sidetopright.Y, 0),
                                                    new Point3d(sidebottomleft.X - 150, sidetopright.Y, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimsideheight);
                                    transaction.AddNewlyCreatedDBObject(dimsideheight, true);

                                    AlignedDimension dimdepth = new AlignedDimension(
                                                    new Point3d(sidebottomleft.X, sidetopright.Y, 0),
                                                    new Point3d(sidetopright.X, sidetopright.Y, 0),
                                                    new Point3d(sidebottomleft.X, sidetopright.Y + 150, 0),
                                                    "", // Dimension text (Auto-generated)
                                                    db.Dimstyle // Use current dimension style
                                                    );

                                    blockTableRecord.AppendEntity(dimdepth);
                                    transaction.AddNewlyCreatedDBObject(dimdepth, true);

                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(descPoint.X - 1150 - maxdepth, descPoint.Y - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(descPoint.X - 450, descPoint.Y - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FLOOR", new Point3d(shippingrigthX + 450, descPoint.Y - baseheight, 0), 1.0);
                                    InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(descPoint.X - 500, descPoint.Y + (panelheight / 2), 0), 1.0);

                                    MText frontviewText = new MText
                                    {
                                        Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y - 270, 0),
                                        Height = 55,
                                        TextHeight = 55,
                                        Contents = @"\LFRONT VIEW",  // "\L" applies underline
                                        Attachment = AttachmentPoint.MiddleCenter
                                    };
                                    blockTableRecord.AppendEntity(frontviewText);
                                    transaction.AddNewlyCreatedDBObject(frontviewText, true);

                                    MText sideviewText = new MText
                                    {
                                        Location = new Point3d(descPoint.X - 800 - (maxdepth / 2), descPoint.Y - 230, 0),
                                        Height = 55,
                                        TextHeight = 55,
                                        Contents = @"\LSIDE VIEW",  // "\L" applies underline
                                        Attachment = AttachmentPoint.MiddleCenter
                                    };
                                    blockTableRecord.AppendEntity(sideviewText);
                                    transaction.AddNewlyCreatedDBObject(sideviewText, true);

                                    if (view == "TOPVIEW")
                                    {
                                        MText topviewText = new MText
                                        {
                                            Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y + panelheight + 600, 0),
                                            Height = 55,
                                            TextHeight = 55,
                                            Contents = @"\LTOP VIEW",  // "\L" applies underline
                                            Attachment = AttachmentPoint.MiddleCenter
                                        };
                                        blockTableRecord.AppendEntity(topviewText);
                                        transaction.AddNewlyCreatedDBObject(topviewText, true);

                                        BlockReference toptext1 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) - 450, descPoint.Y + panelheight + 550, 0), 1.0);
                                        BlockReference toptext2 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) + 450, descPoint.Y + panelheight + 550, 0), 1.0);
                                        toptext1.Rotation = -Math.PI / 2;
                                        toptext2.Rotation = -Math.PI / 2;
                                    }
                                    else if (view == "BOTTOMVIEW")
                                    {
                                        MText topviewText = new MText
                                        {
                                            Location = new Point3d((descPoint.X + shippingrigthX) / 2, descPoint.Y - maxdepth - 950, 0),
                                            Height = 55,
                                            TextHeight = 55,
                                            Contents = @"\LBOTTOM VIEW",  // "\L" applies underline
                                            Attachment = AttachmentPoint.MiddleCenter
                                        };
                                        blockTableRecord.AppendEntity(topviewText);
                                        transaction.AddNewlyCreatedDBObject(topviewText, true);

                                        BlockReference toptext1 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) - 450, descPoint.Y - maxdepth - 1000, 0), 1.0);
                                        BlockReference toptext2 = InsertBlock(db, sourceDb, transaction, blockTableRecord, "FRONT", new Point3d(((descPoint.X + shippingrigthX) / 2) + 450, descPoint.Y - maxdepth - 1000, 0), 1.0);
                                        toptext1.Rotation = -Math.PI / 2;
                                        toptext2.Rotation = -Math.PI / 2;
                                    }

                                    if (feederheights.Count > 0) // Ensure there is at least one list
                                    {
                                        List<double> lastList = feederheights[feederheights.Count - 1]; // Get the last list
                                        double previousY = descPoint.Y; // Start Y position

                                        foreach (double value in lastList)
                                        {
                                            // Create an aligned dimension moving upwards
                                            AlignedDimension dimrightsideheight = new AlignedDimension(
                                                new Point3d(shippingrigthX, previousY, 0),
                                                new Point3d(shippingrigthX, previousY + value, 0), // Increment Y-axis
                                                new Point3d(shippingrigthX + 90, previousY + (value / 2), 0), // Midpoint for dimension line
                                                "", // Auto-generated text
                                                db.Dimstyle // Use current dimension style
                                            );

                                            // Append to drawing
                                            blockTableRecord.AppendEntity(dimrightsideheight);
                                            transaction.AddNewlyCreatedDBObject(dimrightsideheight, true);

                                            // Update previous Y position for next dimension
                                            previousY += value;
                                        }
                                    }



                                }

                                startY = descPoint.Y;
                                startX += width;
                                width = 0.0;
                                vbbfound = false;
                                //feederfound = false;
                            }


                            transaction.Commit();
                        }
                    }

                    //acadDoc.SendStringToExecute("._ZOOM _EXTENTS ", true, false, false);

                    if (error)
                    {
                        MessageBox.Show(errorText);
                    }

                }

                catch (Exception ex)
                {
                    ed.WriteMessage("\nError: " + ex.Message);
                }
                finally
                {
                    if (excelApp != null)
                    {
                        Marshal.ReleaseComObject(excelApp);
                    }
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
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) throw new InvalidOperationException("Active document is null.");

            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions options = new PromptEntityOptions("\nSelect a rectangle: ");
            options.SetRejectMessage("\nOnly rectangles (closed polylines) are allowed.");
            options.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult result = ed.GetEntity(options);

            if (result.Status != PromptStatus.OK)
                return;
            ObjectId rectId = result.ObjectId;

            double userscale = 0.8;
            double linescale = 0.03;
            bool plotWithLineWeight = true;
            bool bommerge = true;
            string paneltype = "";

            using (ynotform panelform = new ynotform())
            {
                if (panelform.ShowDialog() == DialogResult.OK)
                {

                    double.TryParse(panelform.a4scale, out userscale);
                    double.TryParse(panelform.ltscale, out linescale);
                    plotWithLineWeight = panelform.lineweight;
                    bommerge = panelform.mergebom;
                    paneltype = panelform.panelselection;
                    
                }
            }

            if( paneltype == "SINGLE_PANEL")
            {
                try
                {
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

                    string fileNameOnly = Path.GetFileName(filePath).ToLower().Replace(" ", "");

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

                        if (!IsFileWritable(filePath))
                        {
                            MessageBox.Show($"Error: The file '{filePath}' is open in another application. Close it and try again.", "File In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return; // Exit without plotting
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
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Total Sheet Progress");
                                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                    progressDialog.LowerPlotProgressRange = 0;
                                    progressDialog.UpperPlotProgressRange = allRectangles.Count;
                                    progressDialog.LowerSheetProgressRange = 0;
                                    progressDialog.UpperSheetProgressRange = allRectangles.Count;

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
                                            progressDialog.SheetProgressPos = pageNumber;
                                            progressDialog.PlotProgressPos = pageNumber;
                                            plotEngine.BeginPage(plotPageInfo, plotInfo, pageNumber == allRectangles.Count, null);
                                            plotEngine.BeginGenerateGraphics(null);
                                            plotEngine.EndGenerateGraphics(null);
                                            plotEngine.EndPage(null);
                                            progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, $"Processing page {pageNumber} of {allRectangles.Count}");
                                            pageNumber++;
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

                    if (bommerge)
                    {
                        // Check if Excel is already running
                        Excel.Application excelApp = null;
                        try
                        {
                            excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");

                        }
                        catch (COMException)
                        {
                            // Excel is not running, show a message and return
                            MessageBox.Show("Pdf Generated \n But Excel was not running for Bom Merge");
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
                            MessageBox.Show("Pdf Generated \n But NO sheet opened for Bom Merge");
                            return;
                        }

                        // Check all workbooks and sheets for a match
                        foreach (Excel.Workbook wb in excelApp.Workbooks)
                        {
                            foreach (Excel.Worksheet ws in wb.Sheets)
                            {
                                if (fileNameOnly.Contains(ws.Name.ToLower().Replace(" ", "")))
                                {
                                    matchCount++;
                                    matchedWorksheet = ws;
                                    matchedWorkbook = wb;

                                    if (matchCount > 1)
                                    {
                                        MessageBox.Show("PDF Generated \n But Error: Multiple sheets match the filename. Please check and rename.");
                                        return;
                                    }
                                }
                            }
                        }

                        // If exactly one match is found, proceed
                        if (matchCount == 1)
                        {
                            workbook = matchedWorkbook;
                            worksheet = matchedWorksheet;
                        }
                        else if (matchCount == 0)
                        {
                            MessageBox.Show("PDF Generated \n But No matching sheet found in any open workbook for merging bom.");
                            return;
                        }

                        // Define the save path (same as CAD plotting save path)
                        string cadSaveDirectory = Path.GetDirectoryName(filePath); // Extract folder from CAD save path
                        string excelPdfSavePath = Path.Combine(cadSaveDirectory, "BOM.pdf");

                        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape; // Set to Landscape
                        worksheet.PageSetup.Zoom = false;  // Disable Zoom
                        worksheet.PageSetup.FitToPagesWide = 1;  // Fit all columns in one page
                        worksheet.PageSetup.FitToPagesTall = false;  // Don't force rows to fit (let them flow)

                        worksheet.PageSetup.LeftMargin = excelApp.InchesToPoints(0.45);
                        worksheet.PageSetup.RightMargin = excelApp.InchesToPoints(0.45);
                        worksheet.PageSetup.TopMargin = excelApp.InchesToPoints(0.55);
                        worksheet.PageSetup.BottomMargin = excelApp.InchesToPoints(0.55);
                        worksheet.PageSetup.HeaderMargin = excelApp.InchesToPoints(0.4);
                        worksheet.PageSetup.FooterMargin = excelApp.InchesToPoints(0.4);

                        worksheet.PageSetup.CenterHorizontally = true;

                        try
                        {

                            worksheet.ExportAsFixedFormat(
                                Excel.XlFixedFormatType.xlTypePDF,
                                excelPdfSavePath,
                                Excel.XlFixedFormatQuality.xlQualityStandard,
                                true,  // Include Open Worksheets
                                false,  // Fit to page
                                1,     // From page
                                Type.Missing,
                                false  // Do not open after publish
                            );

                            string mergedPdfPath = Path.Combine(cadSaveDirectory, "Merged_Output.pdf"); // Final merged PDF

                            // Check if both PDFs exist
                            if (!File.Exists(filePath) || !File.Exists(excelPdfSavePath))
                            {
                                MessageBox.Show("Error: One or both PDFs are missing.");
                                return;
                            }

                            // Create an empty output document
                            PdfDocument outputDocument = new PdfDocument();

                            PdfDocument inputPdf1 = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                            foreach (PdfPage page in inputPdf1.Pages)
                            {
                                if (page.Height > page.Width)  // If portrait, rotate it
                                {
                                    page.Rotate = 270;  // Rotate by 90 degrees to make landscape
                                }
                                outputDocument.AddPage(page);
                            }

                            // Merge the second PDF (BOM)
                            PdfDocument inputPdf2 = PdfReader.Open(excelPdfSavePath, PdfDocumentOpenMode.Import);
                            foreach (PdfPage page in inputPdf2.Pages)
                            {
                                if (page.Height > page.Width)  // If portrait, rotate it
                                {
                                    page.Rotate = 270;  // Rotate by 90 degrees to make landscape
                                }
                                outputDocument.AddPage(page);
                            }

                            // Save the merged PDF
                            outputDocument.Save(mergedPdfPath);

                            // Cleanup
                            //inputPdf1.Close();
                            //inputPdf2.Close();
                            //outputDocument.Close();

                            // Delete individual PDFs after merging (optional)
                            File.Delete(filePath);
                            File.Delete(excelPdfSavePath);
                            File.Move(mergedPdfPath, filePath);

                            MessageBox.Show($"Pdf Generated \nBOM sheet successfully plotted and Merged");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error while saving BOM sheet as PDF: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Pdf Generated \nAutomation by GaMeR");
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
            else if( paneltype == "MULTIPLE_PANEL")
            {
                try
                {

                    string lastUsedFolder = null;
                    string folderPath = null;
                    StringBuilder errorLog = new StringBuilder();

                    using (var dialog = new CommonOpenFileDialog
                    {
                        Title = "Select a folder",
                        IsFolderPicker = true, // Enables folder selection
                        RestoreDirectory = true // Restores the selected directory for future use
                    })
                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            folderPath = dialog.FileName;
                            lastUsedFolder = folderPath;
                        }


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
                    int pagecount = 1;
                    int maxpagecount = 0;

                    foreach (var polyExtents3 in allRectanglesfull)
                    {
                        using (Transaction acTrans = doc.TransactionManager.StartTransaction())
                        {
                            List<Extents3d> allRectanglescount = new List<Extents3d>();
                            //List<Extents3d> outerRectangles = new List<Extents3d>();
                            Point2d minPointcount = new Point2d(0, 0);
                            Point2d maxPointcount = new Point2d(0, 0);
                            Extents3d selectedExtents = polyExtents3;
                            minPointcount = new Point2d(selectedExtents.MinPoint.X, selectedExtents.MinPoint.Y);
                            maxPointcount = new Point2d(selectedExtents.MaxPoint.X, selectedExtents.MaxPoint.Y);


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
                                                        maxpagecount++;
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
                                            maxpagecount++;
                                        }
                                    }
                                }



                                tr.Commit();


                            }

                            acTrans.Commit();
                        }
                    }

                    foreach (var polyExtents2 in allRectanglesfull)
                    {
                        List<Extents3d> allRectangles = new List<Extents3d>();
                        //List<Extents3d> outerRectangles = new List<Extents3d>();
                        Point2d minPoint = new Point2d(0, 0);
                        Point2d maxPoint = new Point2d(0, 0);

                        string pdfName = "";
                        string pdfPath = "";
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
                                                string timehour = DateTime.Now.ToString("HH");
                                                string timemin = DateTime.Now.ToString("mm");
                                                pdfName = $"{dbText.TextString}-GA-{todayDate}.pdf";
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

                                if (!IsFileWritable(Path.Combine(folderPath, pdfName)))
                                {
                                    errorLog.AppendLine($"Error: The file '{Path.Combine(folderPath, pdfName)}' is open in another application. Close it and try again.");
                                    pdfnumber++;
                                    System.Threading.Thread.Sleep(2000);
                                    continue;
                                }



                                using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
                                {
                                    using (PlotProgressDialog progressDialog = new PlotProgressDialog(false, allRectangles.Count, true))
                                    {
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plotting to PDF");
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Total Sheet Progress");
                                        progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                        progressDialog.LowerPlotProgressRange = 0;
                                        progressDialog.UpperPlotProgressRange = maxpagecount;
                                        progressDialog.LowerSheetProgressRange = 0;
                                        progressDialog.UpperSheetProgressRange = allRectangles.Count;

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

                                                if (pdfName != null || pdfName != "")
                                                {
                                                    fileName = pdfName;
                                                }
                                                else
                                                {
                                                    fileName = $"{pdfnumber}.pdf";
                                                }

                                                pdfPath = Path.Combine(folderPath, fileName);
                                                //MessageBox.Show(pdfPath);

                                                if (pageNumber == 1)
                                                {
                                                    plotEngine.BeginDocument(plotInfo, doc.Name, null, 1, true, pdfPath);
                                                }

                                                PlotPageInfo plotPageInfo = new PlotPageInfo();
                                                progressDialog.SheetProgressPos = pageNumber;
                                                progressDialog.PlotProgressPos = pagecount;
                                                plotEngine.BeginPage(plotPageInfo, plotInfo, pageNumber == allRectangles.Count, null);
                                                plotEngine.BeginGenerateGraphics(null);
                                                plotEngine.EndGenerateGraphics(null);
                                                plotEngine.EndPage(null);
                                                progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, $"Processing page {pageNumber} of {allRectangles.Count}");
                                                progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, $"Processing Total page {pagecount} of {maxpagecount}");
                                                pageNumber++;
                                                pagecount++;

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

                        if (bommerge && pdfPath != "" && pdfName != "")
                        {
                            // Check if Excel is already running
                            Excel.Application excelApp = null;
                            try
                            {
                                excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");

                            }
                            catch (COMException)
                            {
                                // Excel is not running, show a message and return
                                errorLog.AppendLine($"Pdf Generated for {pdfName}, but Excel was not running for BOM Merge.");
                                pdfnumber++;
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }

                            Excel.Workbook workbook = null;
                            Excel.Worksheet worksheet = null;
                            int matchCount = 0;
                            Excel.Worksheet matchedWorksheet = null;
                            Excel.Workbook matchedWorkbook = null;

                            // Check if any workbooks are open
                            if (excelApp.Workbooks.Count == 0)
                            {
                                errorLog.AppendLine($"Pdf Generated for {pdfName}, but NO Excel sheet was opened for BOM Merge.");
                                pdfnumber++;
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }

                            // Check all workbooks and sheets for a match
                            foreach (Excel.Workbook wb in excelApp.Workbooks)
                            {
                                foreach (Excel.Worksheet ws in wb.Sheets)
                                {
                                    if (pdfName.ToLower().Replace(" ", "").Contains(ws.Name.ToLower().Replace(" ", "")))
                                    {
                                        matchCount++;
                                        matchedWorksheet = ws;
                                        matchedWorkbook = wb;

                                        if (matchCount > 1)
                                        {
                                            errorLog.AppendLine($"Pdf Generated for {pdfName}, but Error: Multiple sheets match the filename.");
                                            pdfnumber++;
                                            System.Threading.Thread.Sleep(2000);
                                            continue;
                                        }
                                    }
                                }
                            }

                            // If exactly one match is found, proceed
                            if (matchCount == 1)
                            {
                                workbook = matchedWorkbook;
                                worksheet = matchedWorksheet;
                            }
                            else if (matchCount == 0)
                            {
                                errorLog.AppendLine($"Pdf Generated for {pdfName}, but No matching sheet found in any open workbook.");
                                pdfnumber++;
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }

                            //MessageBox.Show(pdfPath);
                            string cadSaveDirectory = folderPath; // Extract folder from CAD save path
                            string excelPdfSavePath = Path.Combine(cadSaveDirectory, "BOM.pdf");

                            worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape; // Set to Landscape
                            worksheet.PageSetup.Zoom = false;  // Disable Zoom
                            worksheet.PageSetup.FitToPagesWide = 1;  // Fit all columns in one page
                            worksheet.PageSetup.FitToPagesTall = false;  // Don't force rows to fit (let them flow)

                            worksheet.PageSetup.LeftMargin = excelApp.InchesToPoints(0.45);
                            worksheet.PageSetup.RightMargin = excelApp.InchesToPoints(0.45);
                            worksheet.PageSetup.TopMargin = excelApp.InchesToPoints(0.55);
                            worksheet.PageSetup.BottomMargin = excelApp.InchesToPoints(0.55);
                            worksheet.PageSetup.HeaderMargin = excelApp.InchesToPoints(0.4);
                            worksheet.PageSetup.FooterMargin = excelApp.InchesToPoints(0.4);
                            worksheet.PageSetup.CenterHorizontally = true;

                            try
                            {

                                worksheet.ExportAsFixedFormat(
                                    Excel.XlFixedFormatType.xlTypePDF,
                                    excelPdfSavePath,
                                    Excel.XlFixedFormatQuality.xlQualityStandard,
                                    true,  // Include Open Worksheets
                                    false,  // Fit to page
                                    1,     // From page
                                    Type.Missing,
                                    false  // Do not open after publish
                                );

                                string mergedPdfPath = Path.Combine(cadSaveDirectory, "Merged_Output.pdf"); // Final merged PDF

                                // Check if both PDFs exist
                                if (!File.Exists(excelPdfSavePath))
                                {
                                    errorLog.AppendLine($"Error: BOM PDF was not generated for {pdfName}.");
                                    pdfnumber++;
                                    System.Threading.Thread.Sleep(2000);
                                    continue;
                                }

                                // Create an empty output document
                                PdfDocument outputDocument = new PdfDocument();

                                PdfDocument inputPdf1 = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
                                foreach (PdfPage page in inputPdf1.Pages)
                                {
                                    if (page.Height > page.Width)  // If portrait, rotate it
                                    {
                                        page.Rotate = 270;  // Rotate by 90 degrees to make landscape
                                    }
                                    outputDocument.AddPage(page);
                                }

                                // Merge the second PDF (BOM)
                                PdfDocument inputPdf2 = PdfReader.Open(excelPdfSavePath, PdfDocumentOpenMode.Import);
                                foreach (PdfPage page in inputPdf2.Pages)
                                {
                                    if (page.Height > page.Width)  // If portrait, rotate it
                                    {
                                        page.Rotate = 270;  // Rotate by 90 degrees to make landscape
                                    }
                                    outputDocument.AddPage(page);
                                }

                                // Save the merged PDF
                                outputDocument.Save(mergedPdfPath);

                                // Delete individual PDFs after merging (optional)
                                File.Delete(pdfPath);
                                File.Delete(excelPdfSavePath);
                                File.Move(mergedPdfPath, pdfPath);


                            }
                            catch (Exception ex)
                            {
                                errorLog.AppendLine($"Error while saving BOM sheet as PDF for {pdfName}: {ex.Message}");
                                pdfnumber++;
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }
                        }

                        pdfnumber++;
                        System.Threading.Thread.Sleep(2000);
                    }

                    db.Ltscale = oldlinescale;
                    if (errorLog.Length > 0)
                    {
                        MessageBox.Show(errorLog.ToString(), "Process Completed with Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("All PDFs generated and merged successfully! \nAutomation by GaMeR", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                catch (Exception ex)
                {

                    Application.ShowAlertDialog($"Error exporting PDF: {ex.Message}");
                }
            }

            
            
        }

        private bool IsFileWritable(string filePath)
        {
            try
            {
                // If the file does not exist, create it first to ensure CAD can write
                if (!File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        // File created successfully
                    }
                }
                else
                {
                    // If file exists, check if it is writable
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        // File is writable
                    }
                }
                return true; // No issues, file is writable
            }
            catch (IOException)
            {
                return false; // File is in use by another process
            }
        }

        [CommandMethod("DESCRIPTION")]
        public void ENTERDESCRPTION()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
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
                String DESFINAL = $"DESCRIPTION - {descrption}";
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

        [CommandMethod("DOOR")]
        public static void HelloAutoCAD()
        {
            // Initialize AutoCAD application
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }
            try
            {
                var acadApp = Application.AcadApplication as dynamic;
                if (acadApp == null)
                {
                    MessageBox.Show("AutoCAD is not running.");
                    return;
                }
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor editor = doc.Editor;

                
                string blockName = null;
                double doorthick = 0;
                double folding = 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)db.CurrentSpaceId.GetObject(OpenMode.ForWrite);

                    PromptEntityOptions promptOptions = new PromptEntityOptions("\nSelect a block: ");
                    promptOptions.SetRejectMessage("\nYou can only select a block.");
                    promptOptions.AddAllowedClass(typeof(BlockReference), true); // Restrict to BlockReference

                    // Get the user input
                    PromptEntityResult result = editor.GetEntity(promptOptions);

                    // Check if the user selected a valid entity
                    if (result.Status == PromptStatus.OK)
                    {
                        // Open the selected object and check if it's a BlockReference
                        ObjectId objectId = result.ObjectId;
                        BlockReference block = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
                        blockName = block.Name;
   
                    }
                    else
                    {
                        MessageBox.Show("Selection was not successful or the user canceled.");
                    }

                    PromptDoubleOptions thick = new PromptDoubleOptions("\nEnter Door Thickness: ")
                    {
                        AllowNegative = false, // Prevent negative values
                        AllowZero = false    
                    };

                    PromptDoubleResult thickResult = editor.GetDouble(thick);

                    if (thickResult.Status == PromptStatus.OK)
                    {
                        doorthick = thickResult.Value;
                    }
                    else
                    {
                        MessageBox.Show("Door Thickness is required.");
                        return;
                    }

                    PromptDoubleOptions whatthick = new PromptDoubleOptions("\nEnter Folding Length: ")
                    {
                        AllowNegative = false, // Prevent negative values
                        AllowZero = false
                    };

                    PromptDoubleResult foldingResult = editor.GetDouble(whatthick);

                    if (foldingResult.Status == PromptStatus.OK)
                    {
                        folding = foldingResult.Value;
                    }
                    else
                    {
                        MessageBox.Show("Folding Length input canceled or invalid.");
                        return;
                    }

                    PromptKeywordOptions lineweightOptions = new PromptKeywordOptions("\nSelect INCHES TYPE: ")
                    {
                        AllowNone = false // Prevent pressing Enter without selecting
                    };

                    // Add keyword options
                    lineweightOptions.Keywords.Add("WELDED");
                    lineweightOptions.Keywords.Add("STEPinches");

                    // Optionally highlight default behavior
                    lineweightOptions.Keywords.Default = "WELDED";

                    PromptResult lineweightResult = editor.GetKeywords(lineweightOptions);

                    if (lineweightResult.Status != PromptStatus.OK)
                    {
                        MessageBox.Show("Atleast select one INCHES type.");
                        return;
                    }

                    string inches = lineweightResult.StringResult;

                    // Ask the user for a point
                    PromptPointOptions pointOptions = new PromptPointOptions("Specify a point: ");
                    PromptPointResult pointResult = editor.GetPoint(pointOptions);

                    if (pointResult.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    Point3d descPoint = pointResult.Value;

                    Processdoor(blockName, descPoint,inches,folding,doorthick);


                    tr.Commit();
                }
                editor.Command("DESCRIPTION");

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            

        }

        [CommandMethod("MECHANICAL_GA")]
        public static void HelloAutoCAD2()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            PromptPointOptions pointOptions = new PromptPointOptions("Specify a point: ");
            PromptPointResult pointResult = editor.GetPoint(pointOptions);

            if (pointResult.Status != PromptStatus.OK)
            {
                return;
            }
            Point3d descPoint = pointResult.Value;

            Form1 myForm = new Form1(descPoint);
            myForm.ShowDialog();


        }

        [CommandMethod("TIPARTS")]
        public static void HelloAutoCAD3()
        {
            if (!isEnabled)
            {
                MessageBox.Show("GaMeR Add-in is Disabled");
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            PromptPointOptions pointOptions = new PromptPointOptions("Specify a point: ");
            PromptPointResult pointResult = editor.GetPoint(pointOptions);

            if (pointResult.Status != PromptStatus.OK)
            {
                return;
            }
            Point3d descPoint = pointResult.Value;

            TIPARTS myForm = new TIPARTS(descPoint);
            myForm.ShowDialog();


        }

        [CommandMethod("ABOUT_ME")]
        public void ShowAboutInfo()
        {
            MessageBox.Show(
                "Welcome to the Add-In!\n\n" +
                "Thank you for choosing my AutoCAD add-in to enhance your productivity and streamline your workflows. \n\n" +
                "My Mission is to simplify your tasks and unlock new possibilities within AutoCAD, helping you turn challenges into opportunities.\n\n" +
                ">>Nothing is impossible<<\n\n" +
                "Developed by --- GaMeR " +
                "",
                "About GaMeR Add-In",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        public static void Processdoor(string blockname , Point3d placepoint, string inchestype , double folding,double doorthick)
        {
            // Get the current AutoCAD document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            var config = new System.Collections.Specialized.NameValueCollection();
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

            // Read specific configuration values
            string blockName = blockname;
            string inches = inchestype;
            double foldlength = folding;
            double thick = doorthick;
            double inchx = Convert.ToDouble(config["step_inches_size_x"]);
            double inchy = Convert.ToDouble(config["step_inches_size_y"]);
            double inchclear = Convert.ToDouble(config["step_inches_clearence_y"]) - (inchy /2);
            double dclearx = Convert.ToDouble(config["door&cover_clearence_x"]);
            double dcleary = Convert.ToDouble(config["door&cover_clearence_y"]);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);

                // Get the block definition using the block name
                if (blockTable.Has(blockName))
                {
                    BlockTableRecord block = (BlockTableRecord)blockTable[blockName].GetObject(OpenMode.ForRead);
                    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    List<double> lineLengths = new List<double>();
                    double maxXLength = 0;
                    double maxYLength = 0;

                    foreach (ObjectId entityId in block)
                    {
                        Entity entity = (Entity)entityId.GetObject(OpenMode.ForRead);

                        // Check if the entity is a line
                        if (entity is Line line)
                        {
                            // Add the length of the line to the list
                            lineLengths.Add(line.Length);

                            // Calculate the projection lengths along the X and Y axes
                            double xLength = Math.Abs(line.StartPoint.X - line.EndPoint.X);
                            double yLength = Math.Abs(line.StartPoint.Y - line.EndPoint.Y);

                            // Update maximum X and Y lengths
                            if (xLength > maxXLength)
                                maxXLength = xLength;

                            if (yLength > maxYLength)
                                maxYLength = yLength;
                        }
                    }

                    // Assuming there are at least two lines in the block, otherwise, handle the exception
                    if (lineLengths.Count >= 2)
                    {
                        // User inputs (example, you would retrieve these from your config or user input)
                        double length = maxXLength;
                        double width = maxYLength;

                        //int length = (int)lineLengths[0];
                        //int width = (int)lineLengths[1];
                        //MessageBox.Show("Length: " + length + " Width: " + width);

                        double c = placepoint.Y;
                        double lx = placepoint.X;
                        double ly = placepoint.Y;

                        double fold = foldlength;
                        double fold1 = fold - thick;
                        //MessageBox.Show("Fold: " + fold + " Fold1: " + fold1);
                        double off = thick * 2;
                        double len = length + fold1 + lx;
                        double wid = width + fold1;

                        Point3d p1 = new Point3d(lx + fold1, ly, 0);
                        Point3d p2 = new Point3d(len - off, ly, 0);
                        Point3d p3 = new Point3d(p2.X, fold1 + ly, 0);
                        Point3d p4 = new Point3d(p2.X + fold1, p3.Y, 0);
                        Point3d p5 = new Point3d(p4.X, wid - off + ly, 0);
                        Point3d p6 = new Point3d(p2.X, p5.Y, 0);
                        Point3d p7 = new Point3d(p2.X, p5.Y + fold1, 0);
                        Point3d p8 = new Point3d(p1.X, p7.Y, 0);
                        Point3d p9 = new Point3d(p1.X, p5.Y, 0);
                        Point3d p10 = new Point3d(lx, p5.Y, 0);
                        Point3d p11 = new Point3d(lx, p3.Y, 0);
                        Point3d p12 = new Point3d(p1.X, p3.Y, 0);

                        //MessageBox.Show(inches);
                        if (inches == "STEPinches")
                        {
                            //MessageBox.Show("Inches: " + inches);
                            if (width >= 650)
                            {

                                // For inches 
                                Point3d p13 = new Point3d(lx, p3.Y + inchclear - thick, 0);
                                Point3d p14 = new Point3d(lx + fold1 + inchx - thick, p13.Y, 0);
                                Point3d p15 = new Point3d(p14.X, p13.Y + inchy, 0); 
                                Point3d p16 = new Point3d(lx, p15.Y, 0);
                                Point3d p17 = new Point3d(lx, (width / 2) - (inchy / 2) + ly + fold1, 0); 
                                Point3d p18 = new Point3d(lx, p17.Y + inchy, 0); 
                                Point3d p19 = new Point3d(lx, width - thick - inchclear - inchy + ly + fold1, 0); 
                                Point3d p20 = new Point3d(lx, p19.Y + inchy, 0);

                                Point3d p21 = new Point3d(p1.X, p13.Y, 0);
                                Point3d p22 = new Point3d(p1.X, p15.Y, 0);
                                Point3d p23 = new Point3d(p1.X, p17.Y, 0);
                                Point3d p24 = new Point3d(p1.X, p18.Y, 0);
                                Point3d p25 = new Point3d(p1.X, p19.Y, 0);
                                Point3d p26 = new Point3d(p1.X, p20.Y, 0);

                                // Drawing rectangle
                                Line line1 = new Line(p1, p2);
                                Line line2 = new Line(p2, p3);
                                Line line3 = new Line(p3, p4);
                                Line line4 = new Line(p4, p5);
                                Line line5 = new Line(p5, p6);
                                Line line6 = new Line(p6, p7);
                                Line line7 = new Line(p7, p8);
                                Line line8 = new Line(p8, p9);
                                Line line9 = new Line(p9, p10);
                                Line line11 = new Line(p11, p12);
                                Line line12 = new Line(p12, p1);
                                Line line17 = new Line(p11, p13);
                                Line line18 = new Line(p13, p14);
                                Line line19 = new Line(p14, p15);
                                Line line20 = new Line(p15, p16);
                                Line line21 = new Line(p16, p17);

                                Line line22 = new Line(p13, p14);
                                modelSpace.AppendEntity(line22);
                                tr.AddNewlyCreatedDBObject(line22, true);
                                Line line23 = new Line(p14, p15);
                                modelSpace.AppendEntity(line23);
                                tr.AddNewlyCreatedDBObject(line23, true);
                                Line line24 = new Line(p15, p16);
                                modelSpace.AppendEntity(line24);
                                tr.AddNewlyCreatedDBObject(line24, true);
                                Vector3d moveVector = new Vector3d(0,p17.Y - p13.Y, 0);
                                line22.TransformBy(Matrix3d.Displacement(moveVector));
                                line23.TransformBy(Matrix3d.Displacement(moveVector));
                                line24.TransformBy(Matrix3d.Displacement(moveVector));
                                Line line25 = new Line(p18, p19);
                                modelSpace.AppendEntity(line25);
                                tr.AddNewlyCreatedDBObject(line25, true);
                                Line line26 = new Line(p13, p14);
                                modelSpace.AppendEntity(line26);
                                tr.AddNewlyCreatedDBObject(line26, true);
                                Line line27 = new Line(p14, p15);
                                modelSpace.AppendEntity(line27);
                                tr.AddNewlyCreatedDBObject(line27, true);
                                Line line28 = new Line(p15, p16);
                                modelSpace.AppendEntity(line28);
                                tr.AddNewlyCreatedDBObject(line28, true);
                                Vector3d moveVector2 = new Vector3d(0, p19.Y - p13.Y, 0);
                                line26.TransformBy(Matrix3d.Displacement(moveVector2));
                                line27.TransformBy(Matrix3d.Displacement(moveVector2));
                                line28.TransformBy(Matrix3d.Displacement(moveVector2));
                                Line line29 = new Line(p20, p10);
                                modelSpace.AppendEntity(line29);
                                tr.AddNewlyCreatedDBObject(line29, true);
                                Line line13 = new Line(p12, p3);
                                modelSpace.AppendEntity(line13);
                                tr.AddNewlyCreatedDBObject(line13, true);
                                line13.ColorIndex = 12;
                                Line line14 = new Line(p3, p6);
                                modelSpace.AppendEntity(line14);
                                tr.AddNewlyCreatedDBObject(line14, true);
                                line14.ColorIndex = 12;
                                Line line15 = new Line(p6, p9);
                                modelSpace.AppendEntity(line15);
                                tr.AddNewlyCreatedDBObject(line15, true);
                                line15.ColorIndex = 12;
                                Line line30 = new Line(p12, p21);
                                modelSpace.AppendEntity(line30);
                                tr.AddNewlyCreatedDBObject(line30, true);
                                line30.ColorIndex = 12;
                                Line line31 = new Line(p22, p23);
                                modelSpace.AppendEntity(line31);
                                tr.AddNewlyCreatedDBObject(line31, true);
                                line31.ColorIndex = 12;
                                Line line32 = new Line(p24, p25);
                                modelSpace.AppendEntity(line32);
                                tr.AddNewlyCreatedDBObject(line32, true);
                                line32.ColorIndex = 12;
                                Line line33 = new Line(p26, p9);
                                modelSpace.AppendEntity(line33);
                                tr.AddNewlyCreatedDBObject(line33, true);
                                line33.ColorIndex = 12;
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
                                modelSpace.AppendEntity(line11);
                                modelSpace.AppendEntity(line12);
                                modelSpace.AppendEntity(line17);
                                modelSpace.AppendEntity(line18);
                                modelSpace.AppendEntity(line19);
                                modelSpace.AppendEntity(line20);
                                modelSpace.AppendEntity(line21);

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
                                tr.AddNewlyCreatedDBObject(line11, true);
                                tr.AddNewlyCreatedDBObject(line12, true);
                                tr.AddNewlyCreatedDBObject(line17, true);
                                tr.AddNewlyCreatedDBObject(line18, true);
                                tr.AddNewlyCreatedDBObject(line19, true);
                                tr.AddNewlyCreatedDBObject(line20, true);
                                tr.AddNewlyCreatedDBObject(line21, true);
                            }
                            else if (width >=225)
                            {
                                Point3d p13 = new Point3d(lx, p3.Y + inchclear - thick, 0);
                                Point3d p14 = new Point3d(lx + fold1 + inchx - thick, p13.Y, 0);
                                Point3d p15 = new Point3d(p14.X, p13.Y + inchy, 0);
                                Point3d p16 = new Point3d(lx, p15.Y, 0);
                                Point3d p17 = new Point3d(lx, (width / 2) - (inchy / 2) + ly + fold1, 0);
                                Point3d p18 = new Point3d(lx, p17.Y + inchy, 0);
                                Point3d p19 = new Point3d(lx, width - thick - inchclear - inchy + ly + fold1, 0);
                                Point3d p20 = new Point3d(lx, p19.Y + inchy, 0);

                                Point3d p21 = new Point3d(p1.X, p13.Y, 0);
                                Point3d p22 = new Point3d(p1.X, p15.Y, 0);
                                Point3d p23 = new Point3d(p1.X, p17.Y, 0);
                                Point3d p24 = new Point3d(p1.X, p18.Y, 0);
                                Point3d p25 = new Point3d(p1.X, p19.Y, 0);
                                Point3d p26 = new Point3d(p1.X, p20.Y, 0);

                                // Drawing rectangle
                                Line line1 = new Line(p1, p2);
                                Line line2 = new Line(p2, p3);
                                Line line3 = new Line(p3, p4);
                                Line line4 = new Line(p4, p5);
                                Line line5 = new Line(p5, p6);
                                Line line6 = new Line(p6, p7);
                                Line line7 = new Line(p7, p8);
                                Line line8 = new Line(p8, p9);
                                Line line9 = new Line(p9, p10);
                                Line line11 = new Line(p11, p12);
                                Line line12 = new Line(p12, p1);
                                Line line17 = new Line(p11, p13);
                                Line line18 = new Line(p13, p14);
                                Line line19 = new Line(p14, p15);
                                Line line20 = new Line(p15, p16);
                                Line line21 = new Line(p16, p19);

                                //Line line22 = new Line(p13, p14);
                                //modelSpace.AppendEntity(line22);
                                //tr.AddNewlyCreatedDBObject(line22, true);
                                //Line line23 = new Line(p14, p15);
                                //modelSpace.AppendEntity(line23);
                                //tr.AddNewlyCreatedDBObject(line23, true);
                                //Line line24 = new Line(p15, p16);
                                //modelSpace.AppendEntity(line24);
                                //tr.AddNewlyCreatedDBObject(line24, true);
                                //Vector3d moveVector = new Vector3d(0, p17.Y - p13.Y, 0);
                                //line22.TransformBy(Matrix3d.Displacement(moveVector));
                                //line23.TransformBy(Matrix3d.Displacement(moveVector));
                                //line24.TransformBy(Matrix3d.Displacement(moveVector));
                                //Line line25 = new Line(p18, p19);
                                //modelSpace.AppendEntity(line25);
                                //tr.AddNewlyCreatedDBObject(line25, true);
                                Line line26 = new Line(p13, p14);
                                modelSpace.AppendEntity(line26);
                                tr.AddNewlyCreatedDBObject(line26, true);
                                Line line27 = new Line(p14, p15);
                                modelSpace.AppendEntity(line27);
                                tr.AddNewlyCreatedDBObject(line27, true);
                                Line line28 = new Line(p15, p16);
                                modelSpace.AppendEntity(line28);
                                tr.AddNewlyCreatedDBObject(line28, true);
                                Vector3d moveVector2 = new Vector3d(0, p19.Y - p13.Y, 0);
                                line26.TransformBy(Matrix3d.Displacement(moveVector2));
                                line27.TransformBy(Matrix3d.Displacement(moveVector2));
                                line28.TransformBy(Matrix3d.Displacement(moveVector2));
                                Line line29 = new Line(p20, p10);
                                modelSpace.AppendEntity(line29);
                                tr.AddNewlyCreatedDBObject(line29, true);
                                Line line13 = new Line(p12, p3);
                                modelSpace.AppendEntity(line13);
                                tr.AddNewlyCreatedDBObject(line13, true);
                                line13.ColorIndex = 12;
                                Line line14 = new Line(p3, p6);
                                modelSpace.AppendEntity(line14);
                                tr.AddNewlyCreatedDBObject(line14, true);
                                line14.ColorIndex = 12;
                                Line line15 = new Line(p6, p9);
                                modelSpace.AppendEntity(line15);
                                tr.AddNewlyCreatedDBObject(line15, true);
                                line15.ColorIndex = 12;
                                Line line30 = new Line(p12, p21);
                                modelSpace.AppendEntity(line30);
                                tr.AddNewlyCreatedDBObject(line30, true);
                                line30.ColorIndex = 12;
                                Line line31 = new Line(p22, p25);
                                modelSpace.AppendEntity(line31);
                                tr.AddNewlyCreatedDBObject(line31, true);
                                line31.ColorIndex = 12;
                                //Line line32 = new Line(p24, p25);
                                //modelSpace.AppendEntity(line32);
                                //tr.AddNewlyCreatedDBObject(line32, true);
                                //line32.ColorIndex = 12;
                                Line line33 = new Line(p26, p9);
                                modelSpace.AppendEntity(line33);
                                tr.AddNewlyCreatedDBObject(line33, true);
                                line33.ColorIndex = 12;
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
                                modelSpace.AppendEntity(line11);
                                modelSpace.AppendEntity(line12);
                                modelSpace.AppendEntity(line17);
                                modelSpace.AppendEntity(line18);
                                modelSpace.AppendEntity(line19);
                                modelSpace.AppendEntity(line20);
                                modelSpace.AppendEntity(line21);

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
                                tr.AddNewlyCreatedDBObject(line11, true);
                                tr.AddNewlyCreatedDBObject(line12, true);
                                tr.AddNewlyCreatedDBObject(line17, true);
                                tr.AddNewlyCreatedDBObject(line18, true);
                                tr.AddNewlyCreatedDBObject(line19, true);
                                tr.AddNewlyCreatedDBObject(line20, true);
                                tr.AddNewlyCreatedDBObject(line21, true);
                            }
                            else
                            {
                                double inchclear2 = inchclear - 5;
                                Point3d p13 = new Point3d(lx, p3.Y + inchclear2 - thick, 0);
                                Point3d p14 = new Point3d(lx + fold1 + inchx - thick, p13.Y, 0);
                                Point3d p15 = new Point3d(p14.X, p13.Y + inchy, 0);
                                Point3d p16 = new Point3d(lx, p15.Y, 0);
                                Point3d p17 = new Point3d(lx, (width / 2) - (inchy / 2) + ly + fold1, 0);
                                Point3d p18 = new Point3d(lx, p17.Y + inchy, 0);
                                Point3d p19 = new Point3d(lx, width - thick - inchclear2 - inchy + ly + fold1, 0);
                                Point3d p20 = new Point3d(lx, p19.Y + inchy, 0);

                                Point3d p21 = new Point3d(p1.X, p13.Y, 0);
                                Point3d p22 = new Point3d(p1.X, p15.Y, 0);
                                Point3d p23 = new Point3d(p1.X, p17.Y, 0);
                                Point3d p24 = new Point3d(p1.X, p18.Y, 0);
                                Point3d p25 = new Point3d(p1.X, p19.Y, 0);
                                Point3d p26 = new Point3d(p1.X, p20.Y, 0);

                                // Drawing rectangle
                                Line line1 = new Line(p1, p2);
                                Line line2 = new Line(p2, p3);
                                Line line3 = new Line(p3, p4);
                                Line line4 = new Line(p4, p5);
                                Line line5 = new Line(p5, p6);
                                Line line6 = new Line(p6, p7);
                                Line line7 = new Line(p7, p8);
                                Line line8 = new Line(p8, p9);
                                Line line9 = new Line(p9, p10);
                                Line line11 = new Line(p11, p12);
                                Line line12 = new Line(p12, p1);
                                Line line17 = new Line(p11, p13);
                                Line line18 = new Line(p13, p14);
                                Line line19 = new Line(p14, p15);
                                Line line20 = new Line(p15, p16);
                                Line line21 = new Line(p16, p19);

                                //Line line22 = new Line(p13, p14);
                                //modelSpace.AppendEntity(line22);
                                //tr.AddNewlyCreatedDBObject(line22, true);
                                //Line line23 = new Line(p14, p15);
                                //modelSpace.AppendEntity(line23);
                                //tr.AddNewlyCreatedDBObject(line23, true);
                                //Line line24 = new Line(p15, p16);
                                //modelSpace.AppendEntity(line24);
                                //tr.AddNewlyCreatedDBObject(line24, true);
                                //Vector3d moveVector = new Vector3d(0, p17.Y - p13.Y, 0);
                                //line22.TransformBy(Matrix3d.Displacement(moveVector));
                                //line23.TransformBy(Matrix3d.Displacement(moveVector));
                                //line24.TransformBy(Matrix3d.Displacement(moveVector));
                                //Line line25 = new Line(p18, p19);
                                //modelSpace.AppendEntity(line25);
                                //tr.AddNewlyCreatedDBObject(line25, true);
                                Line line26 = new Line(p13, p14);
                                modelSpace.AppendEntity(line26);
                                tr.AddNewlyCreatedDBObject(line26, true);
                                Line line27 = new Line(p14, p15);
                                modelSpace.AppendEntity(line27);
                                tr.AddNewlyCreatedDBObject(line27, true);
                                Line line28 = new Line(p15, p16);
                                modelSpace.AppendEntity(line28);
                                tr.AddNewlyCreatedDBObject(line28, true);
                                Vector3d moveVector2 = new Vector3d(0, p19.Y - p13.Y, 0);
                                line26.TransformBy(Matrix3d.Displacement(moveVector2));
                                line27.TransformBy(Matrix3d.Displacement(moveVector2));
                                line28.TransformBy(Matrix3d.Displacement(moveVector2));
                                Line line29 = new Line(p20, p10);
                                modelSpace.AppendEntity(line29);
                                tr.AddNewlyCreatedDBObject(line29, true);
                                Line line13 = new Line(p12, p3);
                                modelSpace.AppendEntity(line13);
                                tr.AddNewlyCreatedDBObject(line13, true);
                                line13.ColorIndex = 12;
                                Line line14 = new Line(p3, p6);
                                modelSpace.AppendEntity(line14);
                                tr.AddNewlyCreatedDBObject(line14, true);
                                line14.ColorIndex = 12;
                                Line line15 = new Line(p6, p9);
                                modelSpace.AppendEntity(line15);
                                tr.AddNewlyCreatedDBObject(line15, true);
                                line15.ColorIndex = 12;
                                Line line30 = new Line(p12, p21);
                                modelSpace.AppendEntity(line30);
                                tr.AddNewlyCreatedDBObject(line30, true);
                                line30.ColorIndex = 12;
                                Line line31 = new Line(p22, p25);
                                modelSpace.AppendEntity(line31);
                                tr.AddNewlyCreatedDBObject(line31, true);
                                line31.ColorIndex = 12;
                                //Line line32 = new Line(p24, p25);
                                //modelSpace.AppendEntity(line32);
                                //tr.AddNewlyCreatedDBObject(line32, true);
                                //line32.ColorIndex = 12;
                                Line line33 = new Line(p26, p9);
                                modelSpace.AppendEntity(line33);
                                tr.AddNewlyCreatedDBObject(line33, true);
                                line33.ColorIndex = 12;
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
                                modelSpace.AppendEntity(line11);
                                modelSpace.AppendEntity(line12);
                                modelSpace.AppendEntity(line17);
                                modelSpace.AppendEntity(line18);
                                modelSpace.AppendEntity(line19);
                                modelSpace.AppendEntity(line20);
                                modelSpace.AppendEntity(line21);

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
                                tr.AddNewlyCreatedDBObject(line11, true);
                                tr.AddNewlyCreatedDBObject(line12, true);
                                tr.AddNewlyCreatedDBObject(line17, true);
                                tr.AddNewlyCreatedDBObject(line18, true);
                                tr.AddNewlyCreatedDBObject(line19, true);
                                tr.AddNewlyCreatedDBObject(line20, true);
                                tr.AddNewlyCreatedDBObject(line21, true);
                            }
                        }
                        else
                        {
                            // Drawing rectangle
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
                            

                            Line line13 = new Line(p12, p3);
                            modelSpace.AppendEntity(line13);
                            tr.AddNewlyCreatedDBObject(line13, true);
                            line13.ColorIndex = 12;
                            Line line14 = new Line(p3, p6);
                            modelSpace.AppendEntity(line14);
                            tr.AddNewlyCreatedDBObject(line14, true);
                            line14.ColorIndex = 12;
                            Line line15 = new Line(p6, p9);
                            modelSpace.AppendEntity(line15);
                            tr.AddNewlyCreatedDBObject(line15, true);
                            line15.ColorIndex = 12;
                            Line line16 = new Line(p9, p12);
                            modelSpace.AppendEntity(line16);
                            tr.AddNewlyCreatedDBObject(line16, true);
                            line16.ColorIndex = 12;
                            
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

                        Point3d dd1 = new Point3d(p4.X + 40, 0, 0);
                        AlignedDimension dim1 = new AlignedDimension(p4, p5, dd1, "", ObjectId.Null);
                        modelSpace.AppendEntity(dim1);
                        tr.AddNewlyCreatedDBObject(dim1, true);

                        Point3d dd2 = new Point3d(p2.X + fold1 + 65, 0, 0);
                        AlignedDimension dim2 = new AlignedDimension(p2, p7, dd2, "", ObjectId.Null);
                        modelSpace.AppendEntity(dim2);
                        tr.AddNewlyCreatedDBObject(dim2, true);

                        Point3d dd3 = new Point3d(0,p7.Y + 30, 0);
                        AlignedDimension dim3 = new AlignedDimension(p7, p8, dd3, "", ObjectId.Null);
                        modelSpace.AppendEntity(dim3);
                        tr.AddNewlyCreatedDBObject(dim3, true);

                        Point3d dd4 = new Point3d(0,p10.Y + fold1 + 55, 0);
                        AlignedDimension dim4 = new AlignedDimension(p5, p10, dd4, "", ObjectId.Null);
                        modelSpace.AppendEntity(dim4);
                        tr.AddNewlyCreatedDBObject(dim4, true);

                        AlignedDimension dim5 = new AlignedDimension(p5, p6, dd3, "", ObjectId.Null);
                        modelSpace.AppendEntity(dim5);
                        tr.AddNewlyCreatedDBObject(dim5, true);

                        Point3d insertionPoint = p12;
                        using (BlockReference blockRef = new BlockReference(insertionPoint, block.ObjectId))
                        {
                            // Add the block reference to the model space
                            BlockTableRecord models = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            models.AppendEntity(blockRef);
                            tr.AddNewlyCreatedDBObject(blockRef, true);

                            DBObjectCollection explodedObjects = new DBObjectCollection();
                            blockRef.Explode(explodedObjects);

                            
                            Point3d p99 = Point3d.Origin;
                            bool foundLine = false;

                            foreach (DBObject obj in explodedObjects)
                            {
                                if (obj is Line line)
                                {
                                    p99 = line.StartPoint;
                                    //MessageBox.Show($"{p99.X.ToString()}-{p99.Y.ToString()}");
                                    foundLine = true;
                                    break;
                                }
                            }

                            if (!foundLine)
                            {
                                doc.Editor.WriteMessage("\nNo lines found in the block.");
                                return;
                            }

                            // Now use p99 as the insertion point for the exploded entities
                            foreach (DBObject obj in explodedObjects)
                            {
                                if (obj is Entity entity)
                                {
                                    Vector3d offset2 = new Vector3d(insertionPoint.X - p99.X - thick, insertionPoint.Y - p99.Y - thick , 0);
                                    entity.TransformBy(Matrix3d.Displacement(offset2));
                                    
                                    if (entity.ColorIndex != 50)
                                    {
                                        modelSpace.AppendEntity(entity);
                                        tr.AddNewlyCreatedDBObject(entity, true);
                                    }
                                    
                                }
                            }

                            blockRef.Erase();

                            Point3d s1 = new Point3d(p1.X - thick, p8.Y + 200, 0);
                            Point3d s2 = new Point3d(p2.X + thick, p8.Y + 200, 0);
                            Point3d s3 = new Point3d(p2.X + thick, p8.Y + 200 + foldlength, 0);
                            Point3d s4 = new Point3d(p1.X - thick, p8.Y + 200 + foldlength, 0);

                            Point3d s5 = new Point3d(p4.X + 200, p4.Y - thick, 0);
                            Point3d s6 = new Point3d(p4.X + 200, p5.Y + thick, 0);
                            Point3d s7 = new Point3d(p4.X + 200 + foldlength, p5.Y + thick, 0);
                            Point3d s8 = new Point3d(p4.X + 200 + foldlength, p4.Y - thick, 0);

                            Line lines1 = new Line(s1, s2);
                            modelSpace.AppendEntity(lines1);
                            tr.AddNewlyCreatedDBObject(lines1, true);
                            Line lines2 = new Line(s2, s3);
                            modelSpace.AppendEntity(lines2);
                            tr.AddNewlyCreatedDBObject(lines2, true);
                            Line lines3 = new Line(s1, s4);
                            modelSpace.AppendEntity(lines3);
                            tr.AddNewlyCreatedDBObject(lines3, true);
                            Line lines4 = new Line(s5, s6);
                            modelSpace.AppendEntity(lines4);
                            tr.AddNewlyCreatedDBObject(lines4, true);
                            Line lines5 = new Line(s7, s6);
                            modelSpace.AppendEntity(lines5);
                            tr.AddNewlyCreatedDBObject(lines5, true);
                            Line lines6 = new Line(s5, s8);
                            modelSpace.AppendEntity(lines6);
                            tr.AddNewlyCreatedDBObject(lines6, true);

                            Point3d dd6 = new Point3d(0, s1.Y - 40, 0);
                            AlignedDimension dim6 = new AlignedDimension(s1, s2, dd6, "", ObjectId.Null);
                            modelSpace.AppendEntity(dim6);
                            tr.AddNewlyCreatedDBObject(dim6, true);

                            Point3d dd7 = new Point3d(s2.X + 40, 0, 0);
                            AlignedDimension dim7 = new AlignedDimension(s2, s3, dd7, "", ObjectId.Null);
                            modelSpace.AppendEntity(dim7);
                            tr.AddNewlyCreatedDBObject(dim7, true);

                            Point3d dd8 = new Point3d(s5.X - 40, 0, 0);
                            AlignedDimension dim8 = new AlignedDimension(s5, s6, dd8, "", ObjectId.Null);
                            modelSpace.AppendEntity(dim8);
                            tr.AddNewlyCreatedDBObject(dim8, true);

                            Point3d dd9 = new Point3d(0, s5.Y - 40, 0);
                            AlignedDimension dim9 = new AlignedDimension(s5, s8, dd9, "", ObjectId.Null);
                            modelSpace.AppendEntity(dim9);
                            tr.AddNewlyCreatedDBObject(dim9, true);


                        }

                    }
                    else
                    {
                        editor.WriteMessage("\nBlock does not contain enough lines.");
                    }
                }
                else
                {
                    editor.WriteMessage("\nBlock not found.");
                }

                tr.Commit();
            }
           
        }
        private bool IsRectangleWithin(Extents3d outer, Extents3d inner)
        {
            return outer.MinPoint.X <= inner.MinPoint.X && outer.MinPoint.Y <= inner.MinPoint.Y &&
                   outer.MaxPoint.X >= inner.MaxPoint.X && outer.MaxPoint.Y >= inner.MaxPoint.Y;
        }
        static void Zoom(Point3d pMin, Point3d pMax, Point3d pCenter, double dFactor)
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            int nCurVport = System.Convert.ToInt32(Application.GetSystemVariable("CVPORT"));

            // Get the extents of the current space when no points 
            // or only a center point is provided
            // Check to see if Model space is current
            if (acCurDb.TileMode == true)
            {
                if (pMin.Equals(new Point3d()) == true &&
                    pMax.Equals(new Point3d()) == true)
                {
                    pMin = acCurDb.Extmin;
                    pMax = acCurDb.Extmax;
                }
            }
            else
            {
                // Check to see if Paper space is current
                if (nCurVport == 1)
                {
                    // Get the extents of Paper space
                    if (pMin.Equals(new Point3d()) == true &&
                        pMax.Equals(new Point3d()) == true)
                    {
                        pMin = acCurDb.Pextmin;
                        pMax = acCurDb.Pextmax;
                    }
                }
                else
                {
                    // Get the extents of Model space
                    if (pMin.Equals(new Point3d()) == true &&
                        pMax.Equals(new Point3d()) == true)
                    {
                        pMin = acCurDb.Extmin;
                        pMax = acCurDb.Extmax;
                    }
                }
            }

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Get the current view
                using (ViewTableRecord acView = acDoc.Editor.GetCurrentView())
                {
                    Extents3d eExtents;

                    // Translate WCS coordinates to DCS
                    Matrix3d matWCS2DCS;
                    matWCS2DCS = Matrix3d.PlaneToWorld(acView.ViewDirection);
                    matWCS2DCS = Matrix3d.Displacement(acView.Target - Point3d.Origin) * matWCS2DCS;
                    matWCS2DCS = Matrix3d.Rotation(-acView.ViewTwist,
                                                    acView.ViewDirection,
                                                    acView.Target) * matWCS2DCS;

                    // If a center point is specified, define the min and max 
                    // point of the extents
                    // for Center and Scale modes
                    if (pCenter.DistanceTo(Point3d.Origin) != 0)
                    {
                        pMin = new Point3d(pCenter.X - (acView.Width / 2),
                                            pCenter.Y - (acView.Height / 2), 0);

                        pMax = new Point3d((acView.Width / 2) + pCenter.X,
                                            (acView.Height / 2) + pCenter.Y, 0);
                    }

                    // Create an extents object using a line
                    using (Line acLine = new Line(pMin, pMax))
                    {
                        eExtents = new Extents3d(acLine.Bounds.Value.MinPoint,
                                                    acLine.Bounds.Value.MaxPoint);
                    }

                    // Calculate the ratio between the width and height of the current view
                    double dViewRatio;
                    dViewRatio = (acView.Width / acView.Height);

                    // Tranform the extents of the view
                    matWCS2DCS = matWCS2DCS.Inverse();
                    eExtents.TransformBy(matWCS2DCS);

                    double dWidth;
                    double dHeight;
                    Point2d pNewCentPt;

                    // Check to see if a center point was provided (Center and Scale modes)
                    if (pCenter.DistanceTo(Point3d.Origin) != 0)
                    {
                        dWidth = acView.Width;
                        dHeight = acView.Height;

                        if (dFactor == 0)
                        {
                            pCenter = pCenter.TransformBy(matWCS2DCS);
                        }

                        pNewCentPt = new Point2d(pCenter.X, pCenter.Y);
                    }
                    else // Working in Window, Extents and Limits mode
                    {
                        // Calculate the new width and height of the current view
                        dWidth = eExtents.MaxPoint.X - eExtents.MinPoint.X;
                        dHeight = eExtents.MaxPoint.Y - eExtents.MinPoint.Y;

                        // Get the center of the view
                        pNewCentPt = new Point2d(((eExtents.MaxPoint.X + eExtents.MinPoint.X) * 0.5),
                                                    ((eExtents.MaxPoint.Y + eExtents.MinPoint.Y) * 0.5));
                    }

                    // Check to see if the new width fits in current window
                    if (dWidth > (dHeight * dViewRatio)) dHeight = dWidth / dViewRatio;

                    // Resize and scale the view
                    if (dFactor != 0)
                    {
                        acView.Height = dHeight * dFactor;
                        acView.Width = dWidth * dFactor;
                    }

                    // Set the center of the view
                    acView.CenterPoint = pNewCentPt;

                    // Set the current view
                    acDoc.Editor.SetCurrentView(acView);
                }

                // Commit the changes
                acTrans.Commit();
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
        public static Polyline Addrectangle(Transaction trans,BlockTableRecord btr, Point3d bottomLeft, Point3d topRight, int? color = null)
        {
            Polyline rectangle = new Polyline(4);
            rectangle.AddVertexAt(0, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
            rectangle.AddVertexAt(1, new Point2d(topRight.X, bottomLeft.Y), 0, 0, 0);
            rectangle.AddVertexAt(2, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
            rectangle.AddVertexAt(3, new Point2d(bottomLeft.X, topRight.Y), 0, 0, 0);
            rectangle.Closed = true;
            if (color.HasValue)
            {
                rectangle.ColorIndex = color.Value;
            }
            btr.AppendEntity(rectangle);
            trans.AddNewlyCreatedDBObject(rectangle, true);

            return rectangle;
        }
        private Circle DrawCircle(Transaction trans, BlockTableRecord block, Point3d center, double radius, int? color = null)
        {
            // Create a new Circle object
            Circle circle = new Circle
            {
                Center = center, // Set the center point
                Radius = radius  // Set the radius
            };

            // Apply the color if provided
            if (color.HasValue)
            {
                circle.ColorIndex = color.Value;
            }

            // Add the circle to the block table record and the transaction
            block.AppendEntity(circle);
            trans.AddNewlyCreatedDBObject(circle, true);

            // Return the created circle
            return circle;
        }

    }
    }
