using System.IO;
using System.IO.Pipes;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Newtonsoft.Json;
using Ascon.Pilot.SharedProject;
using System;

namespace PilotRevitAddin
{
    //96440E59-D333-4B44-AB75-0C58FE0432C6
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PrepareProjectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var doc = uiApp.ActiveUIDocument.Document;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Revit file (*.rvt)|*.rvt",
                FileName = doc.Title
            };

            if (saveFileDialog.ShowDialog() != true)
                return Result.Cancelled;

            RevitProject revitProject;
            using (var namedPipeClient = new NamedPipeClientStream(".", "PilotRevitAddinPipe"))
            {
                try
                {
                    namedPipeClient.Connect(10000);
                }
                catch (TimeoutException)
                {
                    return Result.Failed;
                }
                StreamString ss = new StreamString(namedPipeClient);
                ss.SendCommand(saveFileDialog.FileName);
                var answer = ss.ReadAnswer();
                revitProject = JsonConvert.DeserializeObject<RevitProject>(answer);
            }

            var shareFilePathDirectory = Path.GetDirectoryName(revitProject.CentralModelPath);
            if (shareFilePathDirectory == null)
                return Result.Failed;

            Directory.CreateDirectory(shareFilePathDirectory);
            var savingSettings = new SaveAsOptions() { OverwriteExistingFile = true };
            savingSettings.SetWorksharingOptions(new WorksharingSaveAsOptions() { SaveAsCentral = true });

            UpdateProjectSettingsCommand.UpdateProjectInfo(doc, revitProject);

            doc.SaveAs(revitProject.CentralModelPath, savingSettings);
            var pathSaveTo = Path.Combine(revitProject.CentralModelPath, Path.GetFileName(saveFileDialog.FileName));
            File.Copy(pathSaveTo, saveFileDialog.FileName);

            return Result.Succeeded;
        }
    }
}
