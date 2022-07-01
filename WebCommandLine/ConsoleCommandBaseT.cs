using System;
using System.Threading.Tasks;

namespace WebCommandLine
{
    public abstract class ConsoleCommandBase<TArgs> : ConsoleCommandBase where TArgs : new()
    {
        public override async Task<ConsoleResult> RunAsync(string[] args)
        {
            if (args[0] == "?" || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                return await Task.FromResult(Help());
            }

            var result = Parse(args);
            if (result.HasErrors == false)
            {
                try
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    return await RunAsyncCore(args: result.Object);
#pragma warning restore CS8604 // Possible null reference argument.
                }
                catch
                {
                    return ConsoleResult.CreateError("Unexpected error occured while executing cli request");
                }
            }
            else
            {
#pragma warning disable CS8604 // Possible null reference argument.
                return ConsoleResult.CreateError(result.ErrorText);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }

        protected override Task<ConsoleResult> RunAsyncCore(string[] args)
        {
            return Task.FromResult(ConsoleResult.CreateError("Not Implemented"));
        }

        protected abstract CommandLineParserResult<TArgs> Parse(string[] args);
        protected abstract Task<ConsoleResult> RunAsyncCore(TArgs args);
    }
}