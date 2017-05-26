using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace PilotRevitAddin
{
    //40A992C2-CD30-4F75-8632-FEE0A4B8B44C
    [Transaction(TransactionMode.Automatic)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class PilotRibbon : IExternalApplication
    {
        private static readonly string AddInPath = typeof(PilotRibbon).Assembly.Location;
        private const string PilotIceTabName = "Pilot-ICE";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreatePilotTab(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ribbon Sample", ex.ToString());
                return Result.Failed;
            }
        }

        private void CreatePilotTab(UIControlledApplication application)
        {
            application.CreateRibbonTab(PilotIceTabName);
            var pilotRibbon = application.CreateRibbonPanel(PilotIceTabName, "Панель команд Pilot-ICE");
            var prepareButton = new PushButtonData("PrepareButton", "Подготовить проект", AddInPath,
                "PilotRevitAddin.PrepareProjectCommand")
            {
                ToolTip = "Подготовить проект для совместной работы",
                LargeImage = NewBitmapImage(Assembly.GetExecutingAssembly(), "prepareIcon.png"),
                AvailabilityClassName = "PilotRevitAddin.PrepareProjectCommandAvailability"
            };

            var startButton = new PushButtonData("StartButton", "Начать проектирование", AddInPath,
                "PilotRevitAddin.StartDesigningCommand")
            {
                ToolTip = "Начать проектирование в режиме совместной работы",
                LargeImage = NewBitmapImage(Assembly.GetExecutingAssembly(), "startDesigningIcon.png"),
                AvailabilityClassName = "PilotRevitAddin.StartDesigningCommandAvailability"
            };

            var updateProjectSettingsButton = new PushButtonData("UpdateProjectButton", "Обновить настройки проекта", AddInPath,
              "PilotRevitAddin.UpdateProjectSettingsCommand")
            {
                ToolTip = "Обновить настройки проекта",
                LargeImage = NewBitmapImage(Assembly.GetExecutingAssembly(), "updateSettingsIcon.png"),
                AvailabilityClassName = "PilotRevitAddin.UpdateSettingsCommandAvailability"
            };

            pilotRibbon.AddItem(prepareButton);
            pilotRibbon.AddItem(startButton);
            pilotRibbon.AddItem(updateProjectSettingsButton);
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static BitmapImage NewBitmapImage(Assembly a, string imageName)
        {
            var s = a.GetManifestResourceStream("PilotRevitAddin." + imageName);
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();
            return img;
        }
    }
}
