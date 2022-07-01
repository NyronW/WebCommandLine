using System;
using System.Threading.Tasks;

namespace WebCommandLine
{
    public abstract class ConsoleCommandBase : IConsoleCommand
    {
        public abstract ConsoleResult Help();
        public virtual async Task<ConsoleResult> RunAsync(string[] args)
        {
            if (args.Length != 0 && (args[0] == "?" || args[0].Equals("help", StringComparison.OrdinalIgnoreCase)))
            {
                return await Task.FromResult(Help());
            }

            try
            {
                return await RunAsyncCore(args);
            }
            catch
            {
                return ConsoleResult.CreateError("Unexpected error occured while executing cli request");
            }
        }

        protected abstract Task<ConsoleResult> RunAsyncCore(string[] args);
    }
}