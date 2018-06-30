using System;
using System.Threading.Tasks;

namespace Tools.Github.Extensions
{
    public interface IGitHubClient : IDisposable
    {
        Task<PullRequest> GetPullRequest(string owner, string repo, int number);

        Task<bool> MergePullRequest(PullRequest pullRequest, MergeMethod mergeMethod = MergeMethod.Merge);

        Task<bool> UpdatePullRequestHead(PullRequest pullRequest);
    }
}