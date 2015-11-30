using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpRetro.Utils
{
	public class InstanceDll : IDisposable
  {
    IntPtr _hModule;

    public InstanceDll(string dllPath)
    {
      //try to locate dlls in the current directory (for libretro cores)
      //this isnt foolproof but its a little better than nothing
      var envpath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
      try
      {
        string envpath_new = Path.GetDirectoryName(dllPath) + ";" + envpath;
        Environment.SetEnvironmentVariable("PATH", envpath_new, EnvironmentVariableTarget.Process);
        _hModule = LoadLibrary(dllPath);
        if (_hModule == IntPtr.Zero)
        {
          var error = Marshal.GetLastWin32Error();
          return;
        }
      }
      finally
      {
        Environment.SetEnvironmentVariable("PATH", envpath, EnvironmentVariableTarget.Process);
      }
    }

    [Flags]
		enum LoadLibraryFlags : uint
		{
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadLibrary(string dllToLoad);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);
		[DllImport("kernel32.dll")]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
		[DllImport("kernel32.dll")]
		static extern bool FreeLibrary(IntPtr hModule);

		public IntPtr GetProcAddress(string procName)
		{
			return GetProcAddress(_hModule, procName);
		}

		public void Dispose()
		{
			if (_hModule != IntPtr.Zero)
			{
				FreeLibrary(_hModule);
				_hModule = IntPtr.Zero;
			}
		}
	}
}