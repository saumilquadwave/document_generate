using Microsoft.AspNetCore.Mvc;

namespace econsys.DocGenQuickTest
{
    public class ResponseDto
    {
        public bool IsSuccess { get; set; }
        public string pdfBase64Data { get; set; }
        public GeneratedFileDto GeneratedFile { get; set; }

        public string DisplayMessage { get; set; }
        public string ErrorMessage { get; set; }

    }

    public class GeneratedFileDto
    {
        public byte[] FileContents { get; set; }
        public string ContentType { get; set; }
        public string FileDownloadName { get; set; }
        public DateTime? LastModified { get; set; }
        public string EntityTag { get; set; }
        public bool EnableRangeProcessing { get; set; }
    }
}
