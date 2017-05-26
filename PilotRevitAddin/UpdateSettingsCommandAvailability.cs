using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PilotRevitAddin
{
    public class UpdateSettingsCommandAvailability : IExternalCommandAvailability
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

            return !string.IsNullOrEmpty(centralFilePath);
        }
    }
}