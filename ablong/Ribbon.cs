using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System.IO;
using Exception = System.Exception;
using System.Drawing;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Windows.Media;
using System.Linq;

namespace CAD_AUTOMATION
{
    public class RibbonClass
    {
        [CommandMethod("CreateRibbon")]
        public void CreateRibbon()
        {
            // Check if the ribbon is already created
            if (ComponentManager.Ribbon == null)
                return;

            // Check if the tab already exists (optional safety check)
            var existingTab = ComponentManager.Ribbon.Tabs.FirstOrDefault(t => t.Id == "GamerTab");
            if (existingTab != null)
                return;

            // Create the ribbon tab
            RibbonTab ribbonTab = new RibbonTab
            {
                Title = "GaMeR",
                Id = "GamerTab"
            };

            ComponentManager.Ribbon.Tabs.Add(ribbonTab);

            // Create the ribbon panel
            RibbonPanelSource panelSource = new RibbonPanelSource
            {
                Title = "Tools"
            };

            RibbonPanel ribbonPanel = new RibbonPanel
            {
                Source = panelSource
            };

            ribbonTab.Panels.Add(ribbonPanel);

            // Wrap the button inside a RibbonRowPanel
            RibbonRowPanel rowPanel = new RibbonRowPanel
            {
                IsTopJustified = true // ensures proper vertical layout
            };

            // Create the button
            RibbonButton rectButton = new RibbonButton
            {
                Text = "TI PARTS",
                ShowText = true,
                ShowImage = true,
                Orientation = System.Windows.Controls.Orientation.Vertical, // Image on top, text below
                Size = RibbonItemSize.Large,
                Image = LoadBitmap("automation"),       // Optional: for small icon
                LargeImage = LoadBitmap("automation"),  // Your 32x32 icon
                CommandHandler = new RibbonCommandHandler("TIPARTS")
            };

            RibbonButton secondButton = new RibbonButton
            {
                Text = "About ME",
                ShowText = true,
                ShowImage = true,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Size = RibbonItemSize.Large,
                LargeImage = LoadBitmap("about"),
                CommandHandler = new RibbonCommandHandler("ABOUT_ME")
            };

            rowPanel.Items.Add(rectButton);
            rowPanel.Items.Add(secondButton);


            // Add the row panel to the panel source
            panelSource.Items.Add(rowPanel);
        }

        private ImageSource LoadBitmap(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourcePath = $"CAD_AUTOMATION.Icons.{resourceName}.bmp";

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        // Load Bitmap from stream
                        using (Bitmap bitmap = new Bitmap(stream))
                        {
                            using (MemoryStream memory = new MemoryStream())
                            {
                                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                memory.Position = 0;

                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = memory;
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.EndInit();
                                return bitmapImage;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Bitmap resource not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bitmap: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }
    }

    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        private readonly string _commandName;

        public RibbonCommandHandler(string commandName)
        {
            _commandName = commandName;
        }

        public void Execute(object parameter)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute(_commandName + " ", true, false, false);
        }

        public bool CanExecute(object parameter) => true;

        public event System.EventHandler CanExecuteChanged;
    }
}
