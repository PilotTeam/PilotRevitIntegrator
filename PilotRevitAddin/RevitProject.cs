using System.Collections.Generic;

namespace PilotRevitAddin
{
    public class RevitProject
    {
        public string CentralModelPath { get; set; }
        public string PilotObjectId { get; set; }
        public Dictionary<string, string> ProjectInfoAttrsMap { get; set; }
    }
}
