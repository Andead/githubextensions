using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tools.Github.Extensions;
using Microsoft.Extensions.Logging;

namespace Tools.Github.Autocomplete.Api
{
    public sealed class GitHubClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public GitHubClient(string server, string accessToken, ILogger logger = null)
        {
            _logger = logger;
            _client = GetClient(server, accessToken);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public async Task<PullRequest> GetPullRequest(string owner, string repo, int number)
        {
            try
            {
                string requestUri = $"repos/{owner}/{repo}/pulls/{number}";
                _logger?.LogDebug($"Sending GET to '{requestUri}'...");

                HttpResponseMessage result = await _client.GetAsync(requestUri);
                if (!result.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        $"GitHub replied with status '{result.StatusCode}'. Reason: '{result.ReasonPhrase}'.");
                    return null;
                }

                PullRequest pullRequest = await result.Content.AsSnakeCaseJsonAsync<PullRequest>();
                return pullRequest;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Failed to get pull request #{number}.");
            }

            return null;
        }

        public async Task<bool> MergePullRequest(PullRequest pullRequest, MergeMethod mergeMethod = MergeMethod.Merge)
        {
            try
            {
                HttpResponseMessage response = await _client.PutAsync(
                    $"repos/{pullRequest.Base.Repo.Owner.Login}/{pullRequest.Base.Repo.Name}/pulls/{pullRequest.Number}/merge",
                    new
                    {
                        pullRequest.Head.Sha,
                        mergeMethod
                    }.AsSnakeCaseJsonContent());
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully merged pull request #{pullRequest.Number}.");
                    return true;
                }

                _logger.LogWarning($"Failed to merge pull request #{pullRequest.Number}. Reason: {response.ReasonPhrase}.");
                return false;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Failed to merge pull request #{pullRequest.Number}.");
                return false;
            }
        }

        public async Task<bool> UpdatePullRequestHead(PullRequest pullRequest)
        {
            try
            {
                // Merging base to head
                string head = pullRequest.Base.Ref;
                string @base = pullRequest.Head.Ref;

                HttpResponseMessage response = await _client.PostAsync(
                    $"repos/{pullRequest.Base.Repo.Owner.Login}/{pullRequest.Base.Repo.Name}/merges",
                    new
                    {
                        @base,
                        head,
                        сommitMessage = $"Merge branch '{head}' into {@base}"
                    }.AsSnakeCaseJsonContent());
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully updated branch for pull request #{pullRequest.Number}.");
                    return true;
                }

                _logger.LogWarning($"Failed to update branch for pull request #{pullRequest.Number}. Reason: {response.ReasonPhrase}.");
                return false;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Failed to update branch for pull request #{pullRequest.Number}.");
                return false;
            }
        }

        private HttpClient GetClient(string server, string accessToken)
        {
            Uri baseAddress = new UriBuilder
            {
                Scheme = "https",
                Host = server,
                Path = Path.Combine("api", "v3/")
            }.Uri;

            var client = new HttpClient
            {
                BaseAddress = baseAddress,
                DefaultRequestHeaders =
                {
                    Accept =
                    {
                        new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json")
                    },
                    Authorization = AuthenticationHeaderValue.Parse($"token {accessToken}")
                }
            };

            _logger?.LogDebug($"Created http client, base address: '{client.BaseAddress}'.");

            return client;
        }
    }
}