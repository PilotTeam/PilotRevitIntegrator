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
using Microsoft.WindowsAPICodePack.Dialogs;

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

            var saveFileDialog = new CommonSaveFileDialog();
            saveFileDialog.DefaultFileName = doc.Title;
            saveFileDialog.DefaultExtension = "rvt";
            saveFileDialog.AlwaysAppendDefaultExtension = true;
            saveFileDialog.Filters.Add(new CommonFileDialogFilter("Revit file", "*.rvt"));

            if (saveFileDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return Result.Cancelled;

            RevitProject revitProject;
            using (var namedPipeClient = new NamedPipeClientStream(".", "PilotRevitAddinPrepareProjectPipe"))
            {
                try
                {
                    namedPipeClient.Connect(10000);
                }
                catch (TimeoutException)
                {
                    return Result.Failed;
                }
                var pipeStream = new StreamString(namedPipeClient);
                pipeStream.SendCommand(saveFileDialog.FileName);
                var answer = pipeStream.ReadAnswer();
                revitProject = JsonConvert.DeserializeObject<RevitProject>(answer);
            }

            var shareFilePathDirectory = Path.GetDirectoryName(revitProject.CentralModelPath);
            if (shareFilePathDirectory == null)
                return Result.Failed;

            if (!Directory.Exists(shareFilePathDirectory))
                Directory.CreateDirectory(shareFilePathDirectory);

            var savingSettings = new SaveAsOptions() { OverwriteExistingFile = true };
            savingSettings.SetWorksharingOptions(new WorksharingSaveAsOptions() { SaveAsCentral = true });

            UpdateProjectSettingsCommand.UpdateProjectInfo(doc, revitProject);

            doc.SaveAs(revitProject.CentralModelPath, savingSettings);
            File.Copy(revitProject.CentralModelPath, saveFileDialog.FileName, true);

            return Result.Succeeded;
        }
    }
}
