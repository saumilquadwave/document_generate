using econsys.DocGenServiceSTA.Controllers;
using econsys.DocGenServiceSTA.Models;
using econsys.DocGenServiceSTA.Services.Interfaces;
using Newtonsoft.Json;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.Text.Json;

namespace econsys.DocGenServiceSTA.Services
{
    public class DocGenInitializerService : IDocGenInitializerService
    {        
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentGeneratorController> _logger;

        public DocGenInitializerService(
            ILogger<DocumentGeneratorController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<DocGenDto> PrepareDocGenDtoFromInputDto(RequestDto input)
        {
            var docGenDto = new DocGenDto();
            PrepareStationery(input, docGenDto);
            PrepareDocument(input, docGenDto);

            if (input.strVariableJSONData != null)
            {
                docGenDto.VariableData = JsonConvert.DeserializeObject<Dictionary<string, object>>(input.strVariableJSONData);
            }

            return Task.FromResult<DocGenDto>(docGenDto);
        }

        private void PrepareStationery(RequestDto input, DocGenDto docGenDto)
        {
            docGenDto.HasStationery = input.HasStationery;

            if (!input.HasStationery || input.StationeryDetails == null)
                return;

            if (input.StationeryDetails?.FileBytes != null)
            {
                using var stream = new MemoryStream(input.StationeryDetails?.FileBytes);
                docGenDto.Stationery = new WordDocument(stream, Syncfusion.DocIO.FormatType.Docx);
                return;
            }

            if (!string.IsNullOrWhiteSpace(input.StationeryDetails?.FilePath))
            {
                try
                {
                    using FileStream stationeryStream = new FileStream(input.StationeryDetails?.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using WordDocument stationeryDoc = new WordDocument(stationeryStream, FormatType.Docx);
                    docGenDto.Stationery = stationeryDoc;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error at PrepareStationery"); //Log more details - tenant , user, which doc etc.
                }                
                
                return;
            }

            return;

        }

        private void PrepareDocument(RequestDto input, DocGenDto docGenDto)
        {
            if (input.DocumentDetails == null) //No Document
                return;

            if (input.DocumentDetails.ContentType == Constants.EnumDocumentContentType.InlineContent)
            {
                //Prepare Doc from SFDT
                ConvertSfdtToDocx(input, docGenDto);
            }
            else
            {
                //Get from WordDocument object or Filepath
                if (input.DocumentDetails?.FileBytes != null)
                {
                    using var stream = new MemoryStream(input.DocumentDetails?.FileBytes);
                    docGenDto.Document = new WordDocument(stream, Syncfusion.DocIO.FormatType.Docx);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(input.DocumentDetails?.FilePath))
                {
                    using FileStream documentStream = new FileStream(input.DocumentDetails?.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    WordDocument document = new WordDocument(documentStream, FormatType.Docx);
                    docGenDto.Document = document;

                    return;
                }
            }

            return;
        }       

        private void ConvertSfdtToDocx(RequestDto input, DocGenDto docGenDto)
        {

            if (string.IsNullOrWhiteSpace(input.DocumentDetails.InlineContent))
                return;

            WordDocument wordDocument = null;
            try
            {
                wordDocument = Syncfusion.EJ2.DocumentEditor.WordDocument.Save(input.DocumentDetails.InlineContent);
                docGenDto.Document = wordDocument;
            }
            finally
            {
                //if (wordDocument != null)
                //{
                //    wordDocument.Dispose();
                //}
            }

        }

        //private string CreateOrGetTempFolder(RequestDto input)
        //{
        //  string _tempFolderBasePath = _configuration["FolderPaths:DocGen_Temp"] ?? Path.GetTempPath();

        //    string tenantFolder = input.RequestFor != null ? input.RequestFor.TenantId.ToString() : "0";
        //    string tempFolderPath = Path.Combine(_tempFolderBasePath, tenantFolder);

        //    DirectoryInfo tempDirectory = new DirectoryInfo(tempFolderPath);
        //    if(!tempDirectory.Exists) Directory.CreateDirectory(tempFolderPath);
        //    return tempFolderPath;
        //}
    }
}
