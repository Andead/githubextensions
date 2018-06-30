using System;
using System.Collections.Generic;
using CommandLine;

namespace Tools.Github.Extensions.ConsoleApp
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Parser.Default
                    .ParseArguments<Options>(args)
                    .WithParsed(App.RunWithOptions)
                    .WithNotParsed(HandleParseError);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            Console.WriteLine("Usage: github-autocomplete <pull_request_id> \n" +
                              "[-s <server>] [-t <token>]\n" +
                              "[-o <owner>] [-r <repo>]\n" +
                              "[-i <interval>]");
        }
    }
}