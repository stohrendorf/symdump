using System.Text.RegularExpressions;

namespace symdump.symfile.util
{
    public static class StringUtil
    {
        private static readonly Regex fakeRe = new Regex(@"^\.\d+fake$");

        public static bool IsFake(this string name)
        {
            return fakeRe.IsMatch(name);
        }
    }
}
