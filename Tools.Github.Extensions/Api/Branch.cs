namespace Tools.Github.Extensions
{
    public sealed class Branch
    {
        public Repository Repo { get; set; }

        public string Ref { get; set; }

        public string Sha { get; set; }
    }
}