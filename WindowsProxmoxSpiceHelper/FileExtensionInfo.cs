using System.Runtime.InteropServices;
using System.Text;

namespace ProxmoxSpiceHelper;

// See <https://www.pinvoke.net/default.aspx/shlwapi/AssocQueryString.html>, <https://stackoverflow.com/a/774482>

public static class FileExtensionInfo
{
    [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string? pszExtra,
        [Out] StringBuilder? pszOut, [In] [Out] ref uint pcchOut);

    public static string GetInformation(AssocStr assocStr, string extension)
    {
        uint pcchOut = 0;

        _ = AssocQueryString(AssocF.Verify, assocStr, extension, null, null, ref pcchOut);

        StringBuilder pszOut = new((int)pcchOut);
        if (AssocQueryString(AssocF.Verify, assocStr, extension, null, pszOut, ref pcchOut) != 0)
            throw new SystemException("Failed to query file association");

        return pszOut.ToString();
    }

    [Flags]
    private enum AssocF : uint
    {
        Verify = 0x40
    }

    public enum AssocStr
    {
        Executable = 2
    }
}