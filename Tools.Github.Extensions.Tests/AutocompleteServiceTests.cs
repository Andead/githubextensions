using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Tools.Github.Extensions;
using Xunit;

namespace Tools.Github.Extensions.Tests
{
    public class AutocompleteServiceTests
    {
        [Fact]
        public async Task OnPullRequestWithMergedTrue_LogsInfoAndReturnsTrue()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest { Merged = true };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object);

            bool result = await service.Run(owner, repo, number);

            logger.VerifyLogged(LogLevel.Information);
            Assert.True(result);
        }

        [Fact]
        public async Task OnPullRequestWithMergedNull_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest { Merged = null };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object);

            bool result = await service.Run(owner, repo, number);

            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task OnPullRequestWithMergeableStateDirty_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest { Merged = false, MergeableState = "dirty" };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object);

            bool result = await service.Run(owner, repo, number);

            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task OnNullPullRequest_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = null;

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object);

            bool result = await service.Run(owner, repo, number);

            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Theory]
        [InlineData("Bug 123", "bug 123")]
        public async Task IfTitlesDontMatch_LogsErrorAndReturnsFalse(string title, string input)
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "clean",
                Title = title,
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => input);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            bool result = await service.Run(owner, repo, number);

            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task LoopsForABlockedPullRequest()
        {
            // number of times until PR is returned with Merged = true to break the loop
            const int calledMax = 5;
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "blocked",
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            int called = 0;
            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns<string, string, int>((o, r, n) =>
                {
                    if (++called >= calledMax)
                    {
                        pullRequest.Merged = true;
                    }

                    return Task.FromResult(pullRequest);
                });
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            await service.Run(owner, repo, number);

            client.Verify(c => c.GetPullRequest(owner, repo, number), Times.Exactly(calledMax));
        }

        [Fact]
        public async Task IfTitlesMatch_PromptOnlyFirstTime()
        {
            // number of times until PR is returned with Merged = true to break the loop
            const int calledMax = 5;
            string owner = "owner", repo = "repo", title = "bug 123", input = "bug 123";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "blocked",
                Title = title,
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            int called = 0;
            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns<string, string, int>((o, r, n) =>
                {
                    if (++called > calledMax)
                    {
                        pullRequest.Merged = true;
                    }

                    return Task.FromResult(pullRequest);
                });
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => input);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
        }

        [Fact]
        public async Task OnAnOutdatedPullRequest_CallsUpdatePullRequestHead()
        {
            // number of times until PR is returned with Merged = true to break the loop
            const int calledMax = 5;
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "behind",
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            int called = 0;
            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns<string, string, int>((o, r, n) =>
                {
                    if (++called > calledMax)
                    {
                        pullRequest.Merged = true;
                    }

                    return Task.FromResult(pullRequest);
                });
            client.Setup(c => c.UpdatePullRequestHead(pullRequest))
                .Returns(Task.FromResult(true));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            client.Verify(c => c.UpdatePullRequestHead(pullRequest), Times.Exactly(calledMax));
        }

        [Fact]
        public async Task WhenUpdatePullRequestHeadFails_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "behind",
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            client.Setup(c => c.UpdatePullRequestHead(pullRequest))
                .Returns(Task.FromResult(false));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task OnUnknownMergeableState_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "something_unknown",
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task OnUnstableMergeableState_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "unstable",
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();
            var config = GetConfiguration(false);

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object,
                ui: ui.Object, configuration: config);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task LoopsForAStillBeingComputedForMergeabilityPullRequest()
        {
            // number of times until PR is returned with Merged = true to break the loop
            const int calledMax = 5;
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "clean",
                Mergeable = null,
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            int called = 0;
            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns<string, string, int>((o, r, n) =>
                {
                    if (++called > calledMax)
                    {
                        pullRequest.Merged = true;
                    }

                    return Task.FromResult(pullRequest);
                });
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            client.Verify(c => c.GetPullRequest(owner, repo, number), Times.Exactly(calledMax + 1));
        }

        [Fact]
        public async Task OnCleanButUnmergeablePR_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "clean",
                Mergeable = false,
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Fact]
        public async Task OnPRMergeFailure_LogsErrorAndReturnsFalse()
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = "clean",
                Mergeable = true,
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            client.Setup(c => c.MergePullRequest(pullRequest, It.IsAny<MergeMethod>()))
                .Returns(Task.FromResult(false));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(client: client.Object, logger: logger.Object, ui: ui.Object);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            client.Verify(c => c.MergePullRequest(pullRequest, It.IsAny<MergeMethod>()), Times.Once);
            logger.VerifyLogged(LogLevel.Error);
            Assert.False(result);
        }

        [Theory]
        [InlineData("clean")]
        [InlineData("unstable")]
        public async Task OnPRMergeSuccess_LogsInfoAndReturnsTrue(string mergeableState)
        {
            string owner = "owner", repo = "repo";
            int number = 1234;
            PullRequest pullRequest = new PullRequest
            {
                Merged = false,
                MergeableState = mergeableState,
                Mergeable = true,
                Title = "bug 123",
                Head = new Branch { },
                Base = new Branch { }
            };

            var client = new Mock<IGitHubClient>();
            var logger = new Mock<ILogger>();
            var ui = new Mock<IUserInterface>();
            var config = GetConfiguration(true);

            client.Setup(c => c.GetPullRequest(owner, repo, number))
                .Returns(Task.FromResult(pullRequest));
            client.Setup(c => c.MergePullRequest(pullRequest, It.IsAny<MergeMethod>()))
                .Returns(Task.FromResult(true));
            ui.Setup(u => u.Prompt(It.IsAny<string>()))
                .Returns(() => pullRequest.Title);

            IAutocompleteService service = GetService(config, client.Object, ui.Object, logger.Object);

            bool result = await service.Run(owner, repo, number);

            ui.Verify(u => u.Prompt(It.IsAny<string>()), Times.Exactly(1));
            client.Verify(c => c.MergePullRequest(pullRequest, It.IsAny<MergeMethod>()), Times.Once);
            logger.VerifyLogged(LogLevel.Information);
            Assert.True(result);
        }

        private static IAutocompleteService GetService(AutocompleteConfiguration configuration = null,
            IGitHubClient client = null, IUserInterface ui = null, ILogger logger = null)
        {
            return new AutocompleteService(configuration ?? GetConfiguration(), 
                client ?? Mock.Of<IGitHubClient>(),
                ui ?? Mock.Of<IUserInterface>(), 
                logger ?? Mock.Of<ILogger>());
        }

        private static AutocompleteConfiguration GetConfiguration(bool mergeUnstable = false)
        {
            return new AutocompleteConfiguration
            {
                // Speeding up the tests
                MinDelay = TimeSpan.Zero,
                PollingInterval = TimeSpan.Zero,
                MergeUnstable = mergeUnstable
            };
        }
    }
}