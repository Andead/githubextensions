using CommandLine;

namespace Tools.Github.Extensions.ConsoleApp
{
    internal class Options
    {
        [Value(0, MetaName = "Pull request id", Required = true, HelpText = "Pull request id")]
        public int Id { get; set; }

        [Option('s', "server", HelpText = "GitHub Enterprise URL")]
        public string Server { get; set; }

        [Option('o', "owner", HelpText = "Repository owner")]
        public string Owner { get; set; }

        [Option('r', "repo", HelpText = "Repository name")]
        public string Repo { get; set; }

        [Option('t', "token", HelpText = "OAuth access token")]
        public string Token { get; set; }

        [Option('i', "interval", Default = 30, HelpText = "Polling interval, seconds")]
        public int Interval { get; set; }

        [Option("merge-unstable", Default = true, HelpText = "Allow merging of pull requests with unstable branches (some checks not successful).")]
        public bool MergeUnstable { get; set; }
    }
}