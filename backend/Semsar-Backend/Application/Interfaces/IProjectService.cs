using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IProjectService
    {
        Task<(int Id, string Name, string Location)> CreateAsync(ProjectDto dto);
        Task PatchAsync(int id, UpdateProjectDto dto);
        Task DeleteAsync(int id);
    }
}
