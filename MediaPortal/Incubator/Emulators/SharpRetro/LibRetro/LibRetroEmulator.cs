using SharpRetro.Controller;
using SharpRetro.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  #region Helper Classes
  public class VariableDescription
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Options { get; set; }
    public string DefaultOption { get { return Options[0]; } }

    public override string ToString()
    {
      return string.Format("{0} ({1}) = ({2})", Name, Description, string.Join("|", Options));
    }
  }

  public class VideoInfo
  {
    public float DAR { get; set; }
    public int BufferWidth { get; set; }
    public int BufferHeight { get; set; }

    public int VirtualWidth
    {
      get
      {
        if (DAR <= 0)
          return BufferWidth;
        else if (DAR > 1.0f)
          return (int)(BufferHeight * DAR);
        else
          return BufferWidth;
      }
    }

    public int VirtualHeight
    {
      get
      {
        if (DAR <= 0)
          return BufferHeight;
        if (DAR < 1.0f)
          return (int)(BufferWidth / DAR);
        else
          return BufferHeight;
      }
    }
  }

  public class TimingInfo
  {
    public int VSyncNum { get; set; }
    public int VSyncDen { get; set; }
    public double FPS { get; set; }
    public double SampleRate { get; set; }

    public double VSyncRate
    {
      get { return VSyncNum / (double)VSyncDen; }
    }
  }

  public class SystemInfo
  {
    public string LibraryName { get; set; }
    public string LibraryVersion { get; set; }
    public string ValidExtensions { get; set; }
    public bool NeedsFullPath { get; set; }
    public bool BlockExtract { get; set; }
  }

  public class AudioBuffer
  {
    public short[] Data { get; set; }
    public int Length { get; set; }
  }

  #endregion

  public unsafe class LibRetroEmulator : IDisposable
  {
    #region LibRetro Callbacks
    LibRetro.retro_environment_t retro_environment_cb;
    LibRetro.retro_video_refresh_t retro_video_refresh_cb;
    LibRetro.retro_audio_sample_t retro_audio_sample_cb;
    LibRetro.retro_audio_sample_batch_t retro_audio_sample_batch_cb;
    LibRetro.retro_input_poll_t retro_input_poll_cb;
    LibRetro.retro_input_state_t retro_input_state_cb;
    LibRetro.retro_log_printf_t retro_log_printf_cb;
    LibRetro.retro_perf_callback retro_perf_callback = new LibRetro.retro_perf_callback();
    LibRetro.retro_hw_get_current_framebuffer_t retro_hw_get_current_framebuffer_cb;
    LibRetro.retro_hw_get_proc_address_t retro_hw_get_proc_address_cb;
    #endregion

    #region Protected Members
    protected string _corePath;
    protected LibRetro _retro;
    protected UnmanagedResourceHeap _unmanagedResources = new UnmanagedResourceHeap();
    protected Dictionary<string, VariableDescription> _variables = new Dictionary<string, VariableDescription>();

    protected string _systemDirectory;
    protected IntPtr _systemDirectoryAtom;
    protected string _saveDirectory;
    protected IntPtr _saveDirectoryAtom;

    protected int[] _videoBuffer;
    protected AudioBuffer _audioBuffer;
    protected byte[] _saveBuffer;
    protected byte[] _saveBuffer2;

    protected SystemInfo _systemInfo;
    protected VideoInfo _videoInfo;
    protected TimingInfo _timingInfo;
    protected LibRetro.RETRO_PIXEL_FORMAT _pixelFormat = LibRetro.RETRO_PIXEL_FORMAT.XRGB1555;
    #endregion

    #region Init
    public LibRetroEmulator(string corePath, Action<string> logDlgt = null)
    {
      _corePath = corePath;
      LogDelegate = logDlgt;
      retro_environment_cb = new LibRetro.retro_environment_t(retro_environment);
      retro_video_refresh_cb = new LibRetro.retro_video_refresh_t(retro_video_refresh);
      retro_audio_sample_cb = new LibRetro.retro_audio_sample_t(retro_audio_sample);
      retro_audio_sample_batch_cb = new LibRetro.retro_audio_sample_batch_t(retro_audio_sample_batch);
      retro_input_poll_cb = new LibRetro.retro_input_poll_t(retro_input_poll);
      retro_input_state_cb = new LibRetro.retro_input_state_t(retro_input_state);
      retro_log_printf_cb = new LibRetro.retro_log_printf_t(retro_log_printf);
      retro_hw_get_current_framebuffer_cb = new LibRetro.retro_hw_get_current_framebuffer_t(retro_hw_get_current_framebuffer);
      retro_hw_get_proc_address_cb = new LibRetro.retro_hw_get_proc_address_t(retro_hw_get_proc_address);

      //no way (need new mechanism) to check for SSSE3, MMXEXT, SSE4, SSE42
      retro_perf_callback.get_cpu_features = new LibRetro.retro_get_cpu_features_t(() => (ulong)(
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsXMMIAvailable) ? LibRetro.RETRO_SIMD.SSE : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsXMMI64Available) ? LibRetro.RETRO_SIMD.SSE2 : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsSSE3Available) ? LibRetro.RETRO_SIMD.SSE3 : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsMMXAvailable) ? LibRetro.RETRO_SIMD.MMX : 0)
        ));
      retro_perf_callback.get_perf_counter = new LibRetro.retro_perf_get_counter_t(() => System.Diagnostics.Stopwatch.GetTimestamp());
      retro_perf_callback.get_time_usec = new LibRetro.retro_perf_get_time_usec_t(() => DateTime.Now.Ticks / 10);
      retro_perf_callback.perf_log = new LibRetro.retro_perf_log_t(() => { });
      retro_perf_callback.perf_register = new LibRetro.retro_perf_register_t((ref LibRetro.retro_perf_counter counter) => { });
      retro_perf_callback.perf_start = new LibRetro.retro_perf_start_t((ref LibRetro.retro_perf_counter counter) => { });
      retro_perf_callback.perf_stop = new LibRetro.retro_perf_stop_t((ref LibRetro.retro_perf_counter counter) => { });
      Init();
    }

    protected void Init()
    {
      _retro = new LibRetro(_corePath);
      try
      {
        //this series of steps may be mystical.
        LibRetro.retro_system_info system_info = new LibRetro.retro_system_info();
        _retro.retro_get_system_info(ref system_info);

        _systemInfo = new SystemInfo()
        {
          LibraryName = Marshal.PtrToStringAnsi(system_info.library_name),
          LibraryVersion = Marshal.PtrToStringAnsi(system_info.library_version),
          ValidExtensions = Marshal.PtrToStringAnsi(system_info.valid_extensions),
          NeedsFullPath = system_info.need_fullpath,
          BlockExtract = system_info.block_extract
        };

        //the dosbox core calls GET_SYSTEM_DIRECTORY and GET_SAVE_DIRECTORY from retro_set_environment.
        //so, lets set some temporary values (which we'll replace)
        _systemDirectory = Path.GetDirectoryName(_corePath) + "\\";
        _systemDirectoryAtom = _unmanagedResources.StringToHGlobalAnsi(_systemDirectory);
        _saveDirectory = Path.GetDirectoryName(_corePath) + "\\";
        _saveDirectoryAtom = _unmanagedResources.StringToHGlobalAnsi(_saveDirectory);
        _retro.retro_set_environment(retro_environment_cb);

        _retro.retro_set_video_refresh(retro_video_refresh_cb);
        _retro.retro_set_audio_sample(retro_audio_sample_cb);
        _retro.retro_set_audio_sample_batch(retro_audio_sample_batch_cb);
        _retro.retro_set_input_poll(retro_input_poll_cb);
        _retro.retro_set_input_state(retro_input_state_cb);
      }
      catch
      {
        _retro.Dispose();
        _retro = null;
        throw;
      }
    }
    #endregion

    #region Public Properties
    public IRetroController Controller { get; set; }
    public Action<string> LogDelegate { get; set; }

    public SystemInfo SystemInfo
    {
      get { return _systemInfo; }
    }

    public VideoInfo VideoInfo
    {
      get { return _videoInfo; }
    }

    public TimingInfo TimingInfo
    {
      get { return _timingInfo; }
    }

    public int[] VideoBuffer
    {
      get { return _videoBuffer; }
    }

    public AudioBuffer AudioBuffer
    {
      get { return _audioBuffer; }
    }
    #endregion

    #region Load
    public bool LoadGame(byte[] data)
    {
      LibRetro.retro_game_info gameInfo = new LibRetro.retro_game_info();
      fixed (byte* p = &data[0])
      {
        gameInfo.data = (IntPtr)p;
        gameInfo.meta = "";
        gameInfo.path = "";
        gameInfo.size = (uint)data.Length;
        return Load(gameInfo);
      }
    }

    public bool LoadGame(string path)
    {
      LibRetro.retro_game_info gameInfo = new LibRetro.retro_game_info();
      gameInfo.path = path; //is this the right encoding? seems to be ok
      return Load(gameInfo);
    }

    protected bool Load(LibRetro.retro_game_info gameInfo)
    {
      _retro.retro_set_environment(retro_environment_cb);
      _retro.retro_init();

      if (!_retro.retro_load_game(ref gameInfo))
      {
        Log("retro_load_game() failed");
        return false;
      }

      //TODO - libretro cores can return a varying serialize size over time. I tried to get them to write it in the docs...
      _saveBuffer = new byte[_retro.retro_serialize_size()];
      _saveBuffer2 = new byte[_saveBuffer.Length + 13];

      LibRetro.retro_system_av_info av = new LibRetro.retro_system_av_info();
      _retro.retro_get_system_av_info(ref av);

      _videoInfo = new VideoInfo()
      {
        BufferWidth = (int)av.geometry.base_width,
        BufferHeight = (int)av.geometry.base_height,
        DAR = av.geometry.aspect_ratio,
      };
      _timingInfo = new TimingInfo()
      {
        FPS = av.timing.fps,
        SampleRate = av.timing.sample_rate,
        VSyncNum = (int)(10000000 * av.timing.fps),
        VSyncDen = 10000000
      };

      _videoBuffer = new int[av.geometry.max_width * av.geometry.max_height];
      _audioBuffer = new AudioBuffer();
      _audioBuffer.Data = new short[2];
      return true;
    }
    
    #endregion

    #region Public Methods
    public void Run()
    {
      _retro.retro_run();
    }
    #endregion

    #region LibRetro Environment Delegates
    unsafe bool retro_environment(LibRetro.RETRO_ENVIRONMENT cmd, IntPtr data)
    {
      Log("Environment: {0}", cmd);
      switch (cmd)
      {
        case LibRetro.RETRO_ENVIRONMENT.SET_ROTATION:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_OVERSCAN:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_CAN_DUPE:
          //gambatte requires this
          *(bool*)data.ToPointer() = true;
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_MESSAGE:
          {
            LibRetro.retro_message msg = new LibRetro.retro_message();
            Marshal.PtrToStructure(data, msg);
            if (!string.IsNullOrEmpty(msg.msg))
              Log("LibRetro Message: {0}", msg.msg);
            return true;
          }
        case LibRetro.RETRO_ENVIRONMENT.SHUTDOWN:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_PERFORMANCE_LEVEL:
          Log("Core suggested SET_PERFORMANCE_LEVEL {0}", *(uint*)data.ToPointer());
          return true;
        case LibRetro.RETRO_ENVIRONMENT.GET_SYSTEM_DIRECTORY:
          //mednafen NGP neopop fails to launch with no system directory
          Directory.CreateDirectory(_systemDirectory); //just to be safe, it seems likely that cores will crash without a created system directory
          Log("returning system directory: " + _systemDirectory);
          *((IntPtr*)data.ToPointer()) = _systemDirectoryAtom;
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_PIXEL_FORMAT:
          {
            LibRetro.RETRO_PIXEL_FORMAT fmt = 0;
            int[] tmp = new int[1];
            Marshal.Copy(data, tmp, 0, 1);
            fmt = (LibRetro.RETRO_PIXEL_FORMAT)tmp[0];
            switch (fmt)
            {
              case LibRetro.RETRO_PIXEL_FORMAT.RGB565:
              case LibRetro.RETRO_PIXEL_FORMAT.XRGB1555:
              case LibRetro.RETRO_PIXEL_FORMAT.XRGB8888:
                _pixelFormat = fmt;
                Log("New pixel format set: {0}", _pixelFormat);
                return true;
              default:
                Log("Unrecognized pixel format: {0}", (int)_pixelFormat);
                return false;
            }
          }
        case LibRetro.RETRO_ENVIRONMENT.SET_INPUT_DESCRIPTORS:
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_KEYBOARD_CALLBACK:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_DISK_CONTROL_INTERFACE:
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_HW_RENDER:
          {
            //mupen64plus needs this, as well as 3dengine
            LibRetro.retro_hw_render_callback* info = (LibRetro.retro_hw_render_callback*)data.ToPointer();
            info->get_current_framebuffer = Marshal.GetFunctionPointerForDelegate(retro_hw_get_current_framebuffer_cb);
            info->get_proc_address = Marshal.GetFunctionPointerForDelegate(retro_hw_get_proc_address_cb);
            Log("SET_HW_RENDER: {0}, version={1}.{2}, dbg/cache={3}/{4}, depth/stencil = {5}/{6}{7}", info->context_type, info->version_minor, info->version_major, info->debug_context, info->cache_context, info->depth, info->stencil, info->bottom_left_origin ? " (bottomleft)" : "");
            return true;
          }
        case LibRetro.RETRO_ENVIRONMENT.GET_VARIABLE:
          {
            void** variablesPtr = (void**)data.ToPointer();
            IntPtr pKey = new IntPtr(*variablesPtr++);
            string key = Marshal.PtrToStringAnsi(pKey);
            Log("Requesting variable: {0}", key);
            //always return default
            //TODO: cache settings atoms
            if (!_variables.ContainsKey(key))
              return false;
            //HACK: return pointer for desmume mouse, i want to implement that first
            if (key == "desmume_pointer_type")
            {
              *variablesPtr = _unmanagedResources.StringToHGlobalAnsi("touch").ToPointer();
              return true;
            }
            *variablesPtr = _unmanagedResources.StringToHGlobalAnsi(_variables[key].DefaultOption).ToPointer();
            return true;
          }
        case LibRetro.RETRO_ENVIRONMENT.SET_VARIABLES:
          {
            void** variablesPtr = (void**)data.ToPointer();
            for (;;)
            {
              IntPtr pKey = new IntPtr(*variablesPtr++);
              IntPtr pValue = new IntPtr(*variablesPtr++);
              if (pKey == IntPtr.Zero)
                break;
              string key = Marshal.PtrToStringAnsi(pKey);
              string value = Marshal.PtrToStringAnsi(pValue);
              var vd = new VariableDescription() { Name = key };
              var parts = value.Split(';');
              vd.Description = parts[0];
              vd.Options = parts[1].TrimStart(' ').Split('|');
              _variables[vd.Name] = vd;
              Log("set variable: Name: {0}, Description: {1}, Options: {2}", key, parts[0], parts[1].TrimStart(' '));
            }
          }
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_VARIABLE_UPDATE:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_SUPPORT_NO_GAME:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_LIBRETRO_PATH:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_AUDIO_CALLBACK:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_FRAME_TIME_CALLBACK:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_RUMBLE_INTERFACE:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_INPUT_DEVICE_CAPABILITIES:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_LOG_INTERFACE:
          *(IntPtr*)data = Marshal.GetFunctionPointerForDelegate(retro_log_printf_cb);
          return true;
        case LibRetro.RETRO_ENVIRONMENT.GET_PERF_INTERFACE:
          //some builds of fmsx core crash without this set
          Marshal.StructureToPtr(retro_perf_callback, data, false);
          return true;
        case LibRetro.RETRO_ENVIRONMENT.GET_LOCATION_INTERFACE:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_CORE_ASSETS_DIRECTORY:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.GET_SAVE_DIRECTORY:
          //supposedly optional like everything else here, but without it ?? crashes (please write which case)
          //this will suffice for now. if we find evidence later it's needed we can stash a string with 
          //unmanagedResources and CoreFileProvider
          //mednafen NGP neopop, desmume, and others, request this, and falls back on the system directory if it isn't provided
          //desmume crashes if the directory doesn't exist
          Directory.CreateDirectory(_saveDirectory);
          Log("returning save directory: " + _saveDirectory);
          *((IntPtr*)data.ToPointer()) = _saveDirectoryAtom;
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_CONTROLLER_INFO:
          return true;
        case LibRetro.RETRO_ENVIRONMENT.SET_MEMORY_MAPS:
          return false;
        case LibRetro.RETRO_ENVIRONMENT.SET_GEOMETRY:
          LibRetro.retro_game_geometry geometry = *((LibRetro.retro_game_geometry*)data.ToPointer());
          VideoInfo videoInfo = new VideoInfo()
          {
            BufferWidth = (int)geometry.base_width,
            BufferHeight = (int)geometry.base_height,
            DAR = geometry.aspect_ratio
          };
          _videoInfo = videoInfo;
          return true;
        default:
          Log("Unknkown retro_environment command {0}", (int)cmd);
          return false;
      }
    }
    #endregion

    #region LibRetro Video Delegates
    void retro_video_refresh(IntPtr data, uint width, uint height, uint pitch)
    {
      if (data == IntPtr.Zero) // dup frame
        return;

      VideoInfo videoInfo = new VideoInfo()
      {
        BufferWidth = (int)width,
        BufferHeight = (int)height,
        DAR = _videoInfo.DAR
      };
      _videoInfo = videoInfo;

      if (width * height > _videoBuffer.Length)
      {
        Log("Unexpected libretro video buffer overrun?");
        return;
      }
      fixed (int* dst = &_videoBuffer[0])
      {
        if (_pixelFormat == LibRetro.RETRO_PIXEL_FORMAT.XRGB8888)
          Blit888((int*)data, dst, (int)width, (int)height, (int)pitch / 4);
        else if (_pixelFormat == LibRetro.RETRO_PIXEL_FORMAT.RGB565)
          Blit565((short*)data, dst, (int)width, (int)height, (int)pitch / 2);
        else
          Blit555((short*)data, dst, (int)width, (int)height, (int)pitch / 2);
      }
    }

    UIntPtr retro_hw_get_current_framebuffer()
    {
      return UIntPtr.Zero;
    }

    IntPtr retro_hw_get_proc_address(string sym)
    {
      return IntPtr.Zero;
    }

    void Blit555(short* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        short* row = src;
        for (int i = 0; i < width; i++)
        {
          short ci = *row;
          int r = ci & 0x001f;
          int g = ci & 0x03e0;
          int b = ci & 0x7c00;

          r = (r << 3) | (r >> 2);
          g = (g >> 2) | (g >> 7);
          b = (b >> 7) | (b >> 12);
          int co = r | g | b | unchecked((int)0xff000000);

          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }

    void Blit565(short* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        short* row = src;
        for (int i = 0; i < width; i++)
        {
          short ci = *row;
          int r = ci & 0x001f;
          int g = (ci & 0x07e0) >> 5;
          int b = (ci & 0xf800) >> 11;

          r = (r << 3) | (r >> 2);
          g = (g << 2) | (g >> 4);
          b = (b << 3) | (b >> 2);
          int co = (b << 16) | (g << 8) | r;

          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }

    void Blit888(int* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        int* row = src;
        for (int i = 0; i < width; i++)
        {
          int ci = *row;
          int co = ci | unchecked((int)0xff000000);
          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }
    #endregion

    #region LibRetro Audio Delegates
    void retro_audio_sample(short left, short right)
    {
      _audioBuffer.Data[0] = left;
      _audioBuffer.Data[1] = right;
      _audioBuffer.Length = 2;
    }

    uint retro_audio_sample_batch(IntPtr data, uint frames)
    {
      int samples = (int)(frames * 2);
      if (_audioBuffer.Data.Length < samples)
        _audioBuffer.Data = new short[samples];
      Marshal.Copy(data, _audioBuffer.Data, 0, samples);
      _audioBuffer.Length = samples;
      return frames;
    }
    #endregion

    #region LibRetro Input Delegates

    void retro_input_poll()
    {
    }

    //meanings (they are kind of hazy, but once we're done implementing this it will be completely defined by example)
    //port = console physical port?
    //device = logical device type
    //index = sub device index? (multitap?)
    //id = button id (or key id)
    short retro_input_state(uint port, uint device, uint index, uint id)
    {
      switch ((LibRetro.RETRO_DEVICE)device)
      {
        case LibRetro.RETRO_DEVICE.POINTER:
          {
            IRetroPointer pointer = Controller as IRetroPointer;
            if (pointer != null)
            {
              switch ((LibRetro.RETRO_DEVICE_ID_POINTER)id)
              {
                case LibRetro.RETRO_DEVICE_ID_POINTER.X: return pointer.GetPointerX();
                case LibRetro.RETRO_DEVICE_ID_POINTER.Y: return pointer.GetPointerY();
                case LibRetro.RETRO_DEVICE_ID_POINTER.PRESSED: return (short)(pointer.IsPointerPressed() ? 1 : 0);
              }
            }
            return 0;
          }

        case LibRetro.RETRO_DEVICE.KEYBOARD:
          {            
            LibRetro.RETRO_KEY key = (LibRetro.RETRO_KEY)id;
            IRetroKeyboard keyboard = Controller as IRetroKeyboard;
            return (short)(keyboard != null && keyboard.IsKeyPressed(key) ? 1 : 0);
          }

        case LibRetro.RETRO_DEVICE.JOYPAD:
          {
            LibRetro.RETRO_DEVICE_ID_JOYPAD button = (LibRetro.RETRO_DEVICE_ID_JOYPAD)id;
            IRetroPad pad = Controller as IRetroPad;
            return (short)(pad != null && pad.IsButtonPressed((int)port, button) ? 1 : 0);
          }
        default:
          return 0;
      }
    }
    #endregion

    #region LibRetro Log Delegates
    protected void Log(string format, params object[] args)
    {
      var logDlgt = LogDelegate;
      if (logDlgt != null)
        logDlgt(string.Format(format, args));
    }

    unsafe void retro_log_printf(LibRetro.RETRO_LOG_LEVEL level, string fmt, IntPtr a0, IntPtr a1, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7, IntPtr a8, IntPtr a9, IntPtr a10, IntPtr a11, IntPtr a12, IntPtr a13, IntPtr a14, IntPtr a15)
    {
      var logDlgt = LogDelegate;
      if (logDlgt == null)
        return;
      //avert your eyes, these things were not meant to be seen in c#
      //I'm not sure this is a great idea. It would suck for silly logging to be unstable. But.. I dont think this is unstable. The sprintf might just print some garbledy stuff.
      var args = new IntPtr[] { a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15 };
      int idx = 0;
      logDlgt(Sprintf.sprintf(fmt, () => args[idx++]));
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
      if (_retro != null)
      {
        _retro.Dispose();
        _retro = null;
      }
      _unmanagedResources.Dispose();
    }
    #endregion
  }
}
