using System;
using System.IO;
using symfile;

namespace symdump
{
	internal class Program
	{
		private static void Main(string[] args)
		{
		    using (var fs = new FileStream(args[0], FileMode.Open))
		    {
		        var symFile = new SymFile(new BinaryReader(fs), System.Console.Out);
		    }
		}
	}
}