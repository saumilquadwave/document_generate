using econsys.DocGenServiceSTA.Models;

namespace econsys.DocGenServiceSTA.Services.Interfaces
{
    public interface IDocGenInitializerService
    {
        Task<DocGenDto> PrepareDocGenDtoFromInputDto(RequestDto input);
    }
}
