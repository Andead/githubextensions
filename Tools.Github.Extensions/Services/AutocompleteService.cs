using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tools.Github.Extensions
{
    public class AutocompleteService : IAutocompleteService
    {
        private readonly ILogger _logger;
        private readonly AutocompleteConfiguration _configuration;
        private readonly IGitHubClient _client;
        private readonly IUserInterface _userInterface;

        public AutocompleteService(AutocompleteConfiguration configuration,
            IGitHubClient client, IUserInterface userInterface, ILogger logger)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _userInterface = userInterface;
        }

        public async Task<bool> Run(string repositoryOwner, string repositoryName, int number)
        {
            TimeSpan pollingInterval = _configuration.PollingInterval;
            if (pollingInterval < _configuration.MinDelay){
                pollingInterval = _configuration.MinDelay;
            }
            ConfiguredTaskAwaitable delay() => Task.Delay(pollingInterval).ConfigureAwait(true);
            DateTime timeAfterDelay() => DateTime.Now + pollingInterval;
            ConfiguredTaskAwaitable oneSecondDelay() => Task.Delay(_configuration.MinDelay).ConfigureAwait(true);

            void info(string message) => _logger.LogInformation(message);
            void error(string message) => _logger.LogError(message);

            // Only prompt user once
            bool prompted = false;

            while (true)
            {
                // We check the pull request in a loop by looking it up in the repository and
                // checking its status. If the head branch is behind the base branch, we merge base into head
                // and continue the loop. If the branch is clean and PR can be merged, the PR is merged without
                // deleting the head branch.  
                PullRequest pullRequest = await _client.GetPullRequest(repositoryOwner, repositoryName, number);
                if (pullRequest == null)
                {
                    error($"Pull request #{number} was not found in {repositoryOwner}/{repositoryName}.");
                    return false;
                }

                if (pullRequest.Merged == null)
                {
                    error($"Pull request #{pullRequest.Number} has unknown merge status.");
                    return false;
                }

                if (pullRequest.Merged == true)
                {
                    info($"Pull request #{pullRequest.Number} has already been merged.");
                    return true;
                }

                if (pullRequest.MergeableState == "dirty")
                {
                    error($"Pull request #{pullRequest.Number} cannot be merged due to unresolved conflicts.");
                    return false;
                }

                // We ask user to confirm his intentions by entering the PR's title at the beginning
                if (!prompted)
                {
                    string input = _userInterface.Prompt($"Found pull request #{pullRequest.Number}.\n" +
                        $"Merging into '{pullRequest.Base.Ref}' from '{pullRequest.Head.Ref}'\n\n" +
                        $"To initiate auto-complete for pull request #{pullRequest.Number} enter its full title (case matters): ");

                    if (!string.Equals(input, pullRequest.Title, StringComparison.Ordinal))
                    {
                        error($"Pull request #{pullRequest.Number} title does not match the input.");
                        return false;
                    }

                    prompted = true;
                }

                if (pullRequest.MergeableState == "blocked")
                {
                    // This happens when a background status check is pending, i.e. a CI build 
                    info($"Pull request #{pullRequest.Number} merging is blocked. Will try again at {timeAfterDelay():T}.");
                    await delay();

                    continue;
                }

                if (pullRequest.MergeableState == "behind")
                {
                    info($"Pull request #{pullRequest.Number} branch has to be updated. Will try that now.");
                    bool updated = await _client.UpdatePullRequestHead(pullRequest);
                    if (!updated)
                    {
                        error($"Failed to merge {pullRequest.Base.Ref} into {pullRequest.Head.Ref}.");
                        return false;
                    }

                    info($"Successfully merged {pullRequest.Base.Ref} into {pullRequest.Head.Ref}.");
                    continue;
                }

                if (pullRequest.MergeableState != "clean")
                {
                    error($"Pull request #{pullRequest.Number} has an unsupported mergeable state: {pullRequest.MergeableState}.");
                    return false;
                }

                if (pullRequest.Mergeable == null)
                {
                    // This happens when GitHub has not finished computation
                    info($"Pull request #{pullRequest.Number} is being validated for mergeability. Will try again in a second.");
                    await oneSecondDelay();

                    continue;
                }

                if (pullRequest.Mergeable == false)
                {
                    // PR can still be not mergeable if something has changed since our last check
                    error($"Pull request #{pullRequest.Number} cannot be merged.");
                    return false;
                }

                bool merged = await _client.MergePullRequest(pullRequest);
                if (!merged)
                {
                    error($"Pull request #{pullRequest.Number} failed to merge.");
                    return false;
                }

                info($"Pull request #{pullRequest.Number} has been merged.");
                return true;
            }
        }
    }
}