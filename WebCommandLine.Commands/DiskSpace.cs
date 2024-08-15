using System.Text;

namespace WebCommandLine.Commands
{
    [ConsoleCommand("diskspace", "Reports the amount of free disk space in megabytes")]
    public class DiskSpace : ConsoleCommandBase
    {
        public override ConsoleResult Help()
        {
            var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
            sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>diskspace</td></tr>");
            sb.Append("</table>");

            return new ConsoleResult(sb.ToString()) { isHTML = true };
        }


        protected override Task<ConsoleResult> RunAsyncCore(CommandContext context, string[] args)
        {
            var drives = DriveInfo.GetDrives();
            var result = "";

            //If drive specified, show just that drive's space
            if (args.Length != 0)
            {
                var drive = drives.Single(d => d.Name.ToLower() == args[0].ToLower());
                result = GetDriveSpace(drive);
            }
            else //Show all drives
            {
                foreach (var drive in drives)
                {
                    result += GetDriveSpace(drive);
                }
            }

            return Task.FromResult(new ConsoleResult(result) { isHTML = true });
        }

        private string GetDriveSpace(DriveInfo drive)
        {
            string fmt = "<div style='color:#D6B054;white-space:pre'>Drive {0} {1,13} megabytes free</div>";
            return string.Format(fmt, drive.Name, drive.AvailableFreeSpace / 1048576);
        }
    }
}