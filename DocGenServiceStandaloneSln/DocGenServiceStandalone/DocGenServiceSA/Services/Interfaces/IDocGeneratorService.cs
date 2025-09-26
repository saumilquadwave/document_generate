using econsys.DocGenServiceSTA.Models;
using Syncfusion.DocIO.DLS;

namespace econsys.DocGenServiceSTA.Services.Interfaces
{
    public interface IDocGeneratorService
    {
        Task<WordDocument> Generate(DocGenDto docGenDto);
    }
}
