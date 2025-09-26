using econsys.DocGenServiceSTA.Constants;
using econsys.DocGenServiceSTA.Models;
using econsys.DocGenServiceSTA.Services.Interfaces;
using econsys.DocGenServiceSTA.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System.Text;

namespace econsys.DocGenServiceSTA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentGeneratorController : ControllerBase
    {
        private readonly ILogger<DocumentGeneratorController> _logger;
        private readonly IDocGenInitializerService _docGenInitService;
        private readonly IDocGeneratorService _docGenService;

        public DocumentGeneratorController(
            ILogger<DocumentGeneratorController> logger,
            IDocGenInitializerService docGenInitService,
            IDocGeneratorService docGenService)
        {
            _logger = logger;
            _docGenInitService = docGenInitService;
            _docGenService = docGenService;
        }
     
        [HttpPost("Preview")]
        public async Task<ResponseDto> PreviewDocument([FromBody] RequestDto requestDto)
        {
            try
            {
                requestDto.RequestFor.IsPreview = true;
                requestDto.RequestFor.ResponseFormatType = ResponseFormatType.Pdf;

                //Log start
                _logger.LogInformation(DocGenUtilities.PrepareDocGenRequestLogSummary("Preivew", requestDto));


                var response = await GetPreviewOrGenerateDocument(requestDto);
                if (!response.IsSuccess)
                {
                    return response;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"PreivewDocument error. RequestId: {requestDto.RequestId}");
                return new ResponseDto { DisplayMessage = "Failed to prepare document", ErrorMessage = ex.Message };
            }
        }

        [HttpPost("Generate")]
        public async Task<ResponseDto> GenerateDocument([FromBody] RequestDto requestDto)
        {
            try
            {
                //Log start
                _logger.LogInformation(DocGenUtilities.PrepareDocGenRequestLogSummary("Generate", requestDto));

                var response = await GetPreviewOrGenerateDocument(requestDto);
                if (!response.IsSuccess)
                {
                    return response;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Generate error. RequestId: {requestDto.RequestId}");
                return new ResponseDto { DisplayMessage = "Failed to prepare document", ErrorMessage = ex.Message };
            }
        }


        #region Methods

        /// <summary>
        /// Common Method for Preview / Generate
        /// </summary>
        /// <param name="requestDto"></param>
        /// <returns></returns>
        private async Task<ResponseDto> GetPreviewOrGenerateDocument(RequestDto requestDto)
        {
            //Validate
            var validateReq = await DocGenUtilities.ValidateRequest(requestDto);
            if (!validateReq.IsValid)
            {
                return new ResponseDto { DisplayMessage = validateReq.DisplayMessage, ErrorMessage = validateReq.ErrorMessage };
            }

            //Initialize - prepare DocGenDto
            var docGenDto = await _docGenInitService.PrepareDocGenDtoFromInputDto(requestDto);

            //Process DocGenDto and Generate Doc
            var outputDocument = await _docGenService.Generate(docGenDto);

            //Convert to bytes
            byte[] outputDocumentBytes = DocGenUtilities.ConvertDocumentToBytes(requestDto,outputDocument);

            var response = new ResponseDto
            {
                IsSuccess = true
            };

            if (requestDto.RequestFor.IsPreview)
            {
                //For PDF Preview mode, its pdf viewer
                response.pdfBase64Data = "data:application/pdf;base64," + Convert.ToBase64String(outputDocumentBytes);
            }
            else
            {
                // Determine file extension and content type
                string extension = requestDto.RequestFor.ResponseFormatType == ResponseFormatType.Pdf ? "pdf" : "docx";
                string contentType = requestDto.RequestFor.ResponseFormatType == ResponseFormatType.Pdf
                    ? "application/pdf"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                // Generate filename
                string fileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

                response.GeneratedFile = File(outputDocumentBytes, contentType, fileName);
            }

            return response;

        }


       
        #endregion
    }
}
