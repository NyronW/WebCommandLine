namespace WebCommandLine
{
    public class CommandLineParserResult<TArgs> where TArgs : new()
    {
        public TArgs Object { get; }

        public CommandLineParserResult(TArgs @object, string? errorText)
        {
            Object = @object;
            ErrorText = errorText;
        }

        public string? ErrorText { get; }

        public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorText);
    }
}

