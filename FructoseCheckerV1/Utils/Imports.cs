using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FructoseCheckerV1.Utils
{
	public static class Imports
	{
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern uint GetModuleFileName(IntPtr hModule, System.Text.StringBuilder lpFilename, int nSize);
		static readonly int MAX_PATH = 255;
		public static string GetExecutablePath()
		{
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
			{
				var StringBuilder = new System.Text.StringBuilder(MAX_PATH);
				GetModuleFileName(IntPtr.Zero, StringBuilder, MAX_PATH);
				return StringBuilder.ToString();
			}
			else
			{
				return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			}
		}
	}
}
