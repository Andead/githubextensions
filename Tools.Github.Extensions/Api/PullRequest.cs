namespace Tools.Github.Extensions
{
    public sealed class PullRequest
    {
        public bool? Mergeable { get; set; }

        public bool? Merged { get; set; }

        public string MergeableState { get; set; }

        public Branch Head { get; set; }

        public Branch Base { get; set; }

        public int Id { get; set; }

        public int Number { get; set; }

        public string Title { get; set; }
    }
}