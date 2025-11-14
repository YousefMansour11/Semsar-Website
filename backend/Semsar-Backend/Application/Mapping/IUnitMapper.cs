using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public interface IUnitMapper
    {
        UnitCardDto ToCardDto(Domain.Entities.Unit u);
        UnitDetailsDto ToDetailsDto(dynamic u); // accept projection result
        List<UnitCardDto> ToCardDtoList(IEnumerable<dynamic> list);
    }
}