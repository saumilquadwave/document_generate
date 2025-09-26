using econsys.DocGenServiceSTA.Constants;
using econsys.DocGenServiceSTA.Controllers;
using econsys.DocGenServiceSTA.Models;
using Newtonsoft.Json;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System.Text;

namespace econsys.DocGenServiceSTA.Utils
{
    public class DocGenUtilities
    {
        public static string PrepareDocGenRequestLogSummary(string action, RequestDto requestDto)
        {
            StringBuilder str = new StringBuilder();

            str.Append($"************* REQUEST SUMMARY START ******************");
            str.Append($"Request Id: {requestDto.RequestId}. Action: {action}");
            str.Append(Environment.NewLine);

            string requestForDetails = JsonConvert.SerializeObject(requestDto, Formatting.Indented);
            str.Append($"Request For: {requestForDetails}");
            str.Append(Environment.NewLine);

            str.Append($"************* REQUEST SUMMARY END ******************");
            str.Append(Environment.NewLine);

            return str.ToString();
        }

        public static async Task<RequestValidateDto> ValidateRequest(RequestDto request)
        {
            if (request == null)
            {
                return new RequestValidateDto
                {
                    DisplayMessage = "Invalid Request.",
                    ErrorMessage = "request is null"
                };
            }

            if (request.DocumentDetails == null)
            {
                return new RequestValidateDto
                {
                    DisplayMessage = "Invalid Request.",
                    ErrorMessage = "request.DocumentDetails is null"
                };
            }

            if (request.HasStationery && request.StationeryDetails == null)
            {
                return new RequestValidateDto
                {
                    DisplayMessage = "Invalid Request.",
                    ErrorMessage = "request.HasStationery is true , but request.StationeryDetails is null"
                };
            }

            //Do Other validations here..

            return new RequestValidateDto { IsValid = true };
        }
        public static byte[] ConvertDocumentToBytes(RequestDto requestDto, WordDocument document)
        {

            using MemoryStream memoryStream = new MemoryStream();
            if (requestDto.RequestFor.ResponseFormatType == ResponseFormatType.Pdf)
            {
                using (DocIORenderer renderer = new DocIORenderer())
                {
                    using (PdfDocument pdfDocument = renderer.ConvertToPDF(document))
                    {
                        pdfDocument.Save(memoryStream);
                        pdfDocument.Close(true);
                    }
                }
                document.Close();
            }
            else
            {
                document.Save(memoryStream, FormatType.Docx);
                document.Close();
            }
            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}
