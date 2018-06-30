using System;

namespace Tools.Github.Extensions.ConsoleApp
{
    public class ConsoleUserInterface : IUserInterface
    {
        public string Prompt(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }
    }
}