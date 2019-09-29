using System;

namespace CosmosDbUploader.Adapters
{
    public class ConsoleReader : IConsoleReader
    {
        public string ReadLine() => Console.ReadLine();
    }
}
