using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PilotRevitAddin
{
    public class StartDesigningCommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication a, CategorySet b)
        {
            if (a.ActiveUIDocument == null)
                return false;
            var doc = a.ActiveUIDocument.Document;
            if (!doc.IsWorkshared)
                return false;
            var centralModelPath = doc.GetWorksharingCentralModelPath();
            var centralFilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralModelPath);
            var myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return !string.IsNullOrEmpty(centralFilePath) && !doc.PathName.Contains(myDocsPath);
        }
    }
}