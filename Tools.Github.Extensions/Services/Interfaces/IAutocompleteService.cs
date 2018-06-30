using System.Threading.Tasks;

namespace Tools.Github.Extensions
{
    public interface IAutocompleteService
    {
        Task<bool> Run(string repositoryOwner, string repositoryName, int number);
    }
}