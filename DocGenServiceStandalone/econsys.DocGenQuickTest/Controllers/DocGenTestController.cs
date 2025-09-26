using Microsoft.AspNetCore.Mvc;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace econsys.DocGenQuickTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocGenTestController : ControllerBase
    {

        /*
         * NOTES:
         * Replace: _testDataBasePath
         * Can use https://base64.guru/converter/decode/pdf to view the preview
         */
        private readonly IHttpClientFactory _httpClientFactory;
        string _apiBasePath = "";
        string _testDataBasePath = "";
        string _outputBasePath = Directory.GetCurrentDirectory();

        public DocGenTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

            _apiBasePath = "https://localhost:7160/api/DocumentGenerator";
            _testDataBasePath = Path.Combine("D:\\QWC_PROJECTS\\OFFLINE_DEMOPROJECTS\\v5_DocGenSA_TestData");
            _outputBasePath = Path.Combine(_testDataBasePath, "TestOutput");

            if(Directory.Exists(_outputBasePath))
            {
                Directory.CreateDirectory(_outputBasePath);
            }
        }


        // 1. document and stationary as file paths
        [HttpGet("TestFilePaths/{isPreview}")]
        public async Task<IActionResult> TestFilePaths(bool isPreview)
        {
            var client = _httpClientFactory.CreateClient();

            string fileNameWithoutExt = "Project Quotation";
            var requestDto = new RequestDto
            {
                HasStationery = false,
                DocumentDetails = new RequestDocumentDto
                {
                    ContentType = EnumDocumentContentType.File,
                    FilePath = Path.Combine(_testDataBasePath, $"Docs\\{fileNameWithoutExt}.docx")
                },
                StationeryDetails = null,
                RequestFor = GetSampleRequestForDto(isPreview),
                strVariableJSONData = await GetVariableJSONData($"{fileNameWithoutExt}.json")                
            };            

            var content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");

            string apiURL = isPreview ? $"{_apiBasePath}/Preview" : $"{_apiBasePath}/Generate";

            var response = await client.PostAsync(apiURL, content);

            var resultString = await response.Content.ReadAsStringAsync();

            // Deserialize to ResponseDto
            var responseDto = JsonSerializer.Deserialize<ResponseDto>(
                                 resultString,
                                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                             );

            if (!isPreview) // Save doc if not preview
            {
                if (responseDto != null && responseDto.GeneratedFile != null)
                {
                    // Save the file
                    var outputPath = Path.Combine(_outputBasePath, fileNameWithoutExt + "_" + responseDto.GeneratedFile.FileDownloadName ?? "output.bin");
                    await System.IO.File.WriteAllBytesAsync(outputPath, responseDto.GeneratedFile.FileContents);
                }
            }
            return Content(resultString, "application/json");
        }

        // 2. document as WordDocumentBytes (byte[])
        [HttpGet("TestFileBytes/{isPreview}")]
        public async Task<IActionResult> TestFileBytes(bool isPreview)
        {
            var client = _httpClientFactory.CreateClient();

            string fileNameWithoutExt = "Project Quotation";

            string docPath = Path.Combine(_testDataBasePath, $"Docs\\{fileNameWithoutExt}.docx");
            byte[] documentBytes = await System.IO.File.ReadAllBytesAsync(docPath);

            var requestDto = new RequestDto
            {
                HasStationery = false,
                DocumentDetails = new RequestDocumentDto
                {
                    ContentType = EnumDocumentContentType.File,
                    FileBytes = documentBytes,
                },
                StationeryDetails = null,
                RequestFor = GetSampleRequestForDto(isPreview),
                strVariableJSONData = await GetVariableJSONData($"{fileNameWithoutExt}.json")
            };

            var content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");

            string apiURL = isPreview ? $"{_apiBasePath}/Preview" : $"{_apiBasePath}/Generate";

            var response = await client.PostAsync(apiURL, content);

            var resultString = await response.Content.ReadAsStringAsync();

            // Deserialize to ResponseDto
            var responseDto = JsonSerializer.Deserialize<ResponseDto>(
                                 resultString,
                                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                             );

            if (!isPreview) // Save doc if not preview
            {
                if (responseDto != null && responseDto.GeneratedFile != null)
                {
                    // Save the file
                    var outputPath = Path.Combine(_outputBasePath, fileNameWithoutExt + "_" + responseDto.GeneratedFile.FileDownloadName ?? "output.bin");
                    await System.IO.File.WriteAllBytesAsync(outputPath, responseDto.GeneratedFile.FileContents);
                }
            }
            return Content(resultString, "application/json");
        }

        // 3. document as sfdt and stationary as WordDocument
        [HttpGet("TestSfdtAndStationaryFileBytes/{isPreview}")]
        public async Task<IActionResult> TestSfdtAndStationaryFileBytes(bool isPreview)
        {
            var client = _httpClientFactory.CreateClient();

            string fileNameWithoutExt = "OA1";

            string docPath = Path.Combine(_testDataBasePath, $"Docs\\{fileNameWithoutExt}.sfdt");
            string documentSFDTContent = await System.IO.File.ReadAllTextAsync(docPath);

            string stationaryPath = Path.Combine(_testDataBasePath, $"Stationery\\Commercial.docx");
            byte[] stationaryBytes = await System.IO.File.ReadAllBytesAsync(stationaryPath);

            var requestDto = new RequestDto
            {
                HasStationery = true,
                DocumentDetails = new RequestDocumentDto
                {
                    ContentType = EnumDocumentContentType.InlineContent,
                    InlineContent = documentSFDTContent,
                },
                StationeryDetails = new RequestStationeryDto { 
                    FileBytes = stationaryBytes
                },
                RequestFor = GetSampleRequestForDto(isPreview),
                strVariableJSONData = await GetVariableJSONData($"{fileNameWithoutExt}.json")
            };

            var content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");

            string apiURL = isPreview ? $"{_apiBasePath}/Preview" : $"{_apiBasePath}/Generate";

            var response = await client.PostAsync(apiURL, content);

            var resultString = await response.Content.ReadAsStringAsync();

            // Deserialize to ResponseDto
            var responseDto = JsonSerializer.Deserialize<ResponseDto>(
                                 resultString,
                                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                             );

            if (!isPreview) // Save doc if not preview
            {
                if (responseDto != null && responseDto.GeneratedFile != null)
                {
                    // Save the file
                    var outputPath = Path.Combine(_outputBasePath, fileNameWithoutExt + "_" + responseDto.GeneratedFile.FileDownloadName ?? "output.bin");
                    await System.IO.File.WriteAllBytesAsync(outputPath, responseDto.GeneratedFile.FileContents);
                }
            }
            return Content(resultString, "application/json");
        }


        #region Utility Methods
        private RequestForDto GetSampleRequestForDto(bool isPreview)
        {
            return new RequestForDto
            {
                TenantId = 1,
                UserName = "tenant1User1",
                IsPreview = isPreview,
                ResponseFormatType = isPreview ? ResponseFormatType.Pdf : ResponseFormatType.Docx
            };
        }

        private async Task<string> GetVariableJSONData(string fileName)
        {
            string variablePath = Path.Combine(_testDataBasePath, $"VariableData\\{fileName}");
            if (!Path.Exists(variablePath))
            {
                variablePath = Path.Combine(_testDataBasePath, $"VariableData\\default.json");
            }
            string strVariableData = await System.IO.File.ReadAllTextAsync(variablePath);

            return strVariableData;
        } 
        #endregion
    }
}