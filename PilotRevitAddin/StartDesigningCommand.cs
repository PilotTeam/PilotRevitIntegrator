using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PilotRevitAddin
{
    //7D7B9E90-BA33-44CD-8218-EE06F6861E54
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class StartDesigningCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var doc = uiApp.ActiveUIDocument.Document;
            var myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(myDocsPath, doc.Title);
          
            doc.SaveAs(GetSafeFilePath(filePath));

            return Result.Succeeded;
        }

        private static string GetSafeFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;
            var validFileName = Path.GetFileNameWithoutExtension(filePath);
            var directory = Path.GetDirectoryName(filePath);
            var extension = Path.GetExtension(filePath);
            for (var i = 1; ; i++)
            {
                var nextFileName = $"{validFileName} ({i})";
                if (directory == null)
                    continue;
                var nextFilePath = Path.Combine(directory, nextFileName + extension);
                if (!File.Exists(nextFilePath))
                    return nextFilePath;
            }
        }
    }
}
