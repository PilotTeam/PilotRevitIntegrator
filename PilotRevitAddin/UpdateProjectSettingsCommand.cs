using System;
using System.IO.Pipes;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace PilotRevitAddin
{
    //F490B081-2A87-4C03-8D07-9D8F4AF7D2AF
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UpdateProjectSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var doc = uiApp.ActiveUIDocument.Document;

            using (var namedPipeClient = new NamedPipeClientStream(".", "PilotRevitAddinUpdateSettingsPipe", PipeDirection.InOut))
            {
                try
                {
                    namedPipeClient.Connect(10000);
                }
                catch (TimeoutException)
                {
                    return Result.Failed;
                }

                namedPipeClient.ReadMode = PipeTransmissionMode.Message;

                var centralModelPath = doc.GetWorksharingCentralModelPath();
                var centralFilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralModelPath);
                if (string.IsNullOrEmpty(centralFilePath))
                    return Result.Failed;

                var messageBytes = Encoding.UTF8.GetBytes(centralFilePath);
                namedPipeClient.Write(messageBytes, 0, messageBytes.Length);

                var revitProject = GetAnswerFromPilot(namedPipeClient);

                UpdateProjectInfo(doc, revitProject);

                doc.Save();
            }

            return Result.Succeeded;
        }

        internal static void UpdateProjectInfo(Document doc, RevitProject revitProject)
        {
            var trans = new Transaction(doc);
            trans.Start("projectInfoUpdate");
            foreach (var attrMap in revitProject.ProjectInfoAttrsMap)
            {
                var param = doc.ProjectInformation.LookupParameter(attrMap.Key);
                param?.Set(attrMap.Value);
            }
            trans.Commit();
        }

        internal static RevitProject GetAnswerFromPilot(NamedPipeClientStream namedPipeClient)
        {
            var messageBuilder = new StringBuilder();
            var messageBuffer = new byte[853];
            do
            {
                namedPipeClient.Read(messageBuffer, 0, messageBuffer.Length);
                var messageChunk = Encoding.UTF8.GetString(messageBuffer).TrimEnd((char)0);
                messageBuilder.Append(messageChunk);
                messageBuffer = new byte[messageBuffer.Length];
            }
            while (!namedPipeClient.IsMessageComplete);

            var revitProject = JsonConvert.DeserializeObject<RevitProject>(messageBuilder.ToString());

            return revitProject;
        }
    }
}
