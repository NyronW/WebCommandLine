using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WebCommandLine
{
    public class ConsoleResult
    {
        public string output { get; set; } = "";     //Holds the success or error output
        public bool isError { get; set; } = false;  //Is the output a text string or an HTML string?
        public bool isHTML { get; set; } = false;  //True if output is an error message

        public ConsoleResult() { }
        public ConsoleResult(string output)
        {
            this.output = output;
        }

        public ConsoleResult(string output, bool isHtml)
        {
            this.output = output;
            this.isHTML = isHtml;
        }

        public static ConsoleResult CreateError(string errorMessage)
        {
            return new ConsoleErrorResult(errorMessage);
        }

        public static ConsoleResult Html(string html)
        {
            return new ConsoleResult(html) { isHTML = true };
        }
        public static ConsoleResult AsHtmlTable<TCollection>(TCollection collection, string cssClassName = "webcli-striped-tbl") where TCollection : class, IEnumerable
        {
            var sb = new StringBuilder();
            var type = typeof(TCollection).GetGenericArguments().FirstOrDefault() ?? collection.GetType().GetElementType();

            if (type == null)
                return new ConsoleResult(sb.ToString()) { isHTML = true };


            var properties = type.GetProperties();

            sb.AppendLine($"<table class='{cssClassName}'>")
                .AppendLine("<thead style='text-align:left;'>")
                .AppendLine("<tr>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                                      .Cast<DisplayNameAttribute>()
                                      .FirstOrDefault()?.DisplayName ?? prop.Name;
                sb.Append($"<th>{displayName}</th>");
            }

            sb.AppendLine("</tr>")
                .AppendLine("</thead>")
                .AppendLine("<tbody>");

            // Step 2: Generate table rows
            foreach (var item in collection)
            {
                sb.Append("<tr>");
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                    sb.Append($"<td>{value}</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>")
                .AppendLine("</table>");

            return new ConsoleResult(sb.ToString()) { isHTML = true };
        }
    }

    public class ConsoleErrorResult : ConsoleResult
    {
        public ConsoleErrorResult()
        {
            isError = true;
            output = "Invalid syntax";
        }

        public ConsoleErrorResult(string message)
        {
            isError = true;
            output = message;
        }
    }
}