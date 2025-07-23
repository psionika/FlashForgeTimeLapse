using System.IO;
using System.Reflection;

namespace FlashForgeTimeLapse.Helpers
{
    public static class AppFolders
    {
        public static string Exe => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static string Images => Path.Combine(Exe, "Images");
    }
}
