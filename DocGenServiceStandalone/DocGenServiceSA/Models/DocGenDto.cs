using econsys.DocGenServiceSTA.Constants;
using Syncfusion.DocIO.DLS;

namespace econsys.DocGenServiceSTA.Models
{
    public class DocGenDto
    {
        public DocGenDto() {
            VariableData = new Dictionary<string, object>();
            SimpleVariables = new Dictionary<string, object>();
            RepeatedVariables = new Dictionary<string, List<Dictionary<string, object>>>();
        }
        public WordDocument? Document { get; set; }
        public bool HasStationery { get; set; }
        public WordDocument? Stationery { get; set; }

        public Dictionary<string, object> VariableData { get; set; }
        public Dictionary<string, object> SimpleVariables { get; set; } // Simple replacements
        public Dictionary<string, List<Dictionary<string, object>>> RepeatedVariables { get; set; } // Repeating data
    }
}
