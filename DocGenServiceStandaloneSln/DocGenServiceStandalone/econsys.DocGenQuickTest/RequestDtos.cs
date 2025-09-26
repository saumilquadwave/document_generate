using Microsoft.AspNetCore.Mvc;
using Syncfusion.DocIO.DLS;

namespace econsys.DocGenQuickTest
{
    public class RequestDto
    {
        public Guid RequestId { get; set; }
        public required RequestDocumentDto DocumentDetails { get; set; }
        public bool HasStationery { get; set; }
        public RequestStationeryDto? StationeryDetails { get; set; } // If HasStationary, then fill this
        public string? strVariableJSONData { get; set; }        
        public RequestForDto RequestFor { get; set; }

        public RequestDto() { RequestId = Guid.NewGuid(); }
    }


    public class RequestDocumentDto
    {
        public EnumDocumentContentType ContentType { get; set; } //InlineContent / File
        public string? InlineContent { get; set; } //SFDT Content (Ex: OA1, OA2, OA3 etc)

        public byte[]? FileBytes { get; set; }
        public string? FilePath { get; set; }
    }

    //Stationery is always ContentType = File
    public class RequestStationeryDto
    {

        public byte[]? FileBytes { get; set; }
        public string? FilePath { get; set; }
    }

    public class RequestForDto
    {
        public bool IsPreview { get; set; }
        public ResponseFormatType ResponseFormatType { get; set; }
        public int TenantId { get; set; }
        public required string UserName { get; set; }
    }

    
}
