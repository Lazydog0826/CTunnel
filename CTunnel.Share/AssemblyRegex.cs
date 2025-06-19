using System.Text.RegularExpressions;

namespace CTunnel.Share;

public partial class AssemblyRegex
{
    [GeneratedRegex(@"^CTunnel\..+")]
    public static partial Regex Create();
}
