using System;

namespace Tools.Github.Extensions
{
    public class AutocompleteConfiguration
    {
        public TimeSpan PollingInterval { get; set; }

        public TimeSpan MinDelay { get; set; } = TimeSpan.FromSeconds(1);

        public bool MergeUnstable { get; set; } = true;
    }
}