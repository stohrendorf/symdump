using System;
using System.IO;
using symfile;

namespace symdump
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var fs = new FileStream(args[0], FileMode.Open);
			var symFile = new SymFile(fs);
		}
	}
}