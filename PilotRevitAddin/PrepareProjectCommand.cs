using System.IO;
using System.IO.Pipes;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Newtonsoft.Json;
using Ascon.Pilot.SharedProject;

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

            File.WriteAllText(saveFileDialog.FileName, string.Empty);

            using (var namedPipeServer = new NamedPipeServerStream("PilotRevitAddinPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message))
            {
                namedPipeServer.WaitForConnection();
                var messageBuilder = new StringBuilder();
                var messageBuffer = new byte[983];
                do
                {
                    namedPipeServer.Read(messageBuffer, 0, messageBuffer.Length);
                    var messageChunk = Encoding.UTF8.GetString(messageBuffer).TrimEnd((char)0);
                    messageBuilder.Append(messageChunk);
                    messageBuffer = new byte[messageBuffer.Length];
                } while (!namedPipeServer.IsMessageComplete);

                var revitProject = JsonConvert.DeserializeObject<RevitProject>(messageBuilder.ToString());

                var shareFilePathDirectory = Path.GetDirectoryName(revitProject.CentralModelPath);
                if (shareFilePathDirectory == null)
                    return Result.Failed;

                Directory.CreateDirectory(shareFilePathDirectory);
                var iniPath = Path.Combine(shareFilePathDirectory, saveFileDialog.SafeFileName + ".ini");
                File.WriteAllText(iniPath, revitProject.PilotObjectId);
                var savingSettings = new SaveAsOptions() {OverwriteExistingFile = true};
                savingSettings.SetWorksharingOptions(new WorksharingSaveAsOptions() { SaveAsCentral = true});

                UpdateProjectSettingsCommand.UpdateProjectInfo(doc, revitProject);

                doc.SaveAs(Path.Combine(revitProject.CentralModelPath), savingSettings);
            }
              
            return Result.Succeeded;
        }
    }
}
