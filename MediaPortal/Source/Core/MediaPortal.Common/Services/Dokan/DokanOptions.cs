﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Dokan mount options used to describe dokan device behaviour.
  /// </summary>
  [Flags]
  public enum DokanOptions : long
  {
    /// <summary>Enable ouput debug message</summary>
    DebugMode = 1,

    /// <summary>Enable ouput debug message to stderr</summary>
    StderrOutput = 2,

    /// <summary>Use alternate stream</summary>
    AltStream = 4,

    /// <summary>Enable mount drive as write-protected.</summary>
    WriteProtection = 8,

    /// <summary>Use network drive - Dokan network provider need to be installed.</summary>
    NetworkDrive = 16,

    /// <summary>Use removable drive</summary>
    RemovableDrive = 32,

    /// <summary>Use mount manager</summary>
    MountManager = 64,

    /// <summary>Mount the drive on current session only</summary>
    CurrentSession = 128,

    /// <summary>Fixed Driver</summary>
    FixedDrive = 0,
  }
}
