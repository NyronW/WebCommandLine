using System;
using System.Threading.Tasks;

namespace WebCommandLine
{
    public abstract class ConsoleCommandBase<TArgs> : ConsoleCommandBase where TArgs : new()
    {
        public override async Task<ConsoleResult> RunAsync(string[] args)
        {
            if (args.Length != 0 && (args[0] == "?" || args[0].Equals("help", StringComparison.OrdinalIgnoreCase)))
            {
                return await Task.FromResult(Help());
            }

            var result = Parse(args);
            if (result.HasErrors == false)
            {
                try
                {
                    return await RunAsyncCore(args: result.Object);
                }
                catch
                {
                    return ConsoleResult.CreateError("Unexpected error occured while executing cli request");
                }
            }
            else
            {
                return ConsoleResult.CreateError(result.ErrorText!);
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