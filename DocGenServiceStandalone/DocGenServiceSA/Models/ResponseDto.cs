using Microsoft.AspNetCore.Mvc;

namespace econsys.DocGenServiceSTA.Models
{
    public class RequestValidateDto
    {
        public bool IsValid { get; set; }       
        public string DisplayMessage { get; set; }
        public string ErrorMessage { get; set; }

    }

    public class ResponseDto
    {
        public bool IsSuccess { get; set; }
        public string pdfBase64Data { get; set; }
        public FileContentResult GeneratedFile { get; set; }

        public string DisplayMessage { get; set; }
        public string ErrorMessage { get; set; }

    }
}
