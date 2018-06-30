using System.Threading.Tasks;

namespace Tools.Github.Extensions
{
    public class ExtensionsService
    {
        private readonly AutocompleteService _autocompleteService;

        public ExtensionsService(AutocompleteService autocompleteService)
        {
            _autocompleteService = autocompleteService;
        }

        public async Task Autocomplete(string repositoryOwner, string repositoryName, int pullRequestId)
        {
            await _autocompleteService.Run(repositoryOwner, repositoryName, pullRequestId);
        }
    }
}