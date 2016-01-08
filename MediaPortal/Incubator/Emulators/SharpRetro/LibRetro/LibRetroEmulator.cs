﻿using SharpRetro.Controller;
using SharpRetro.RetroGL;
using SharpRetro.Utils;
using SharpRetro.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public delegate void LogDelegate(LibRetroCore.RETRO_LOG_LEVEL level, string message);

  /// <summary>
  /// Implements the LibRetro API, enabling the loading and running of LibRetro cores
  /// </summary>
  public unsafe class LibRetroEmulator : IDisposable
  {
    #region LibRetro Callbacks
    LibRetroCore.retro_environment_t retro_environment_cb;
    LibRetroCore.retro_video_refresh_t retro_video_refresh_cb;
    LibRetroCore.retro_audio_sample_t retro_audio_sample_cb;
    LibRetroCore.retro_audio_sample_batch_t retro_audio_sample_batch_cb;
    LibRetroCore.retro_input_poll_t retro_input_poll_cb;
    LibRetroCore.retro_input_state_t retro_input_state_cb;
    LibRetroCore.retro_log_printf_t retro_log_printf_cb;
    LibRetroCore.retro_perf_callback retro_perf_callback = new LibRetroCore.retro_perf_callback();
    LibRetroCore.retro_hw_get_current_framebuffer_t retro_hw_get_current_framebuffer_cb;
    LibRetroCore.retro_hw_get_proc_address_t retro_hw_get_proc_address_cb;
    LibRetroCore.retro_hw_context_reset_t retro_hw_context_reset_cb;
    LibRetroCore.retro_hw_context_reset_t retro_hw_context_destroy_cb;
    LibRetroCore.retro_rumble_interface retro_rumble_interface = new LibRetroCore.retro_rumble_interface();
    #endregion

    #region Protected Members
    protected string _corePath;
    protected LibRetroCore _retro;
    protected UnmanagedResourceHeap _unmanagedResources = new UnmanagedResourceHeap();
    protected LibRetroVariables _variables = new LibRetroVariables();
    protected LogDelegate _logDelegate;

    protected bool _firstRun = true;
    protected string _systemDirectory;
    protected string _saveDirectory;
    protected bool _canDupe = true;
    protected uint _performanceLevel;

    protected bool _supportsNoGame;
    protected IRetroGLContext _glContext;
    protected int[] _videoBuffer;
    protected int _maxVideoWidth;
    protected int _maxVideoHeight;
    protected bool _depthBuffer;
    protected bool _stencilBuffer;
    protected bool _bottomLeftOrigin;
    protected AudioBuffer _audioBuffer;

    protected SystemInfo _systemInfo;
    protected VideoInfo _videoInfo;
    protected TimingInfo _timingInfo;
    protected LibRetroCore.RETRO_PIXEL_FORMAT _pixelFormat = LibRetroCore.RETRO_PIXEL_FORMAT.XRGB1555;

    protected IRetroController _retroController;
    protected IRetroPad _retroPad;
    protected IRetroAnalog _retroAnalog;
    protected IRetroKeyboard _retroKeyboard;
    protected IRetroPointer _retroPointer;
    protected IRetroRumble _retroRumble;
    #endregion

    #region Init
    /// <summary>
    /// 
    /// </summary>
    /// <param name="corePath">The path to the LibRetro core to load</param>
    public LibRetroEmulator(string corePath)
    {
      _corePath = corePath;
      retro_environment_cb = new LibRetroCore.retro_environment_t(retro_environment);
      retro_video_refresh_cb = new LibRetroCore.retro_video_refresh_t(retro_video_refresh);
      retro_audio_sample_cb = new LibRetroCore.retro_audio_sample_t(retro_audio_sample);
      retro_audio_sample_batch_cb = new LibRetroCore.retro_audio_sample_batch_t(retro_audio_sample_batch);
      retro_input_poll_cb = new LibRetroCore.retro_input_poll_t(retro_input_poll);
      retro_input_state_cb = new LibRetroCore.retro_input_state_t(retro_input_state);
      retro_log_printf_cb = new LibRetroCore.retro_log_printf_t(retro_log_printf);
      retro_hw_get_current_framebuffer_cb = new LibRetroCore.retro_hw_get_current_framebuffer_t(retro_hw_get_current_framebuffer);
      retro_hw_get_proc_address_cb = new LibRetroCore.retro_hw_get_proc_address_t(retro_hw_get_proc_address);
      retro_rumble_interface.set_rumble_state = new LibRetroCore.retro_set_rumble_state_t(retro_set_rumble_state);

      //no way (need new mechanism) to check for SSSE3, MMXEXT, SSE4, SSE42
      retro_perf_callback.get_cpu_features = new LibRetroCore.retro_get_cpu_features_t(() => (ulong)(
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsXMMIAvailable) ? LibRetroCore.RETRO_SIMD.SSE : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsXMMI64Available) ? LibRetroCore.RETRO_SIMD.SSE2 : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsSSE3Available) ? LibRetroCore.RETRO_SIMD.SSE3 : 0) |
          (Win32PInvokes.IsProcessorFeaturePresent(Win32PInvokes.ProcessorFeature.InstructionsMMXAvailable) ? LibRetroCore.RETRO_SIMD.MMX : 0)
        ));
      retro_perf_callback.get_perf_counter = new LibRetroCore.retro_perf_get_counter_t(() => System.Diagnostics.Stopwatch.GetTimestamp());
      retro_perf_callback.get_time_usec = new LibRetroCore.retro_perf_get_time_usec_t(() => DateTime.Now.Ticks / 10);
      retro_perf_callback.perf_log = new LibRetroCore.retro_perf_log_t(() => { });
      retro_perf_callback.perf_register = new LibRetroCore.retro_perf_register_t((ref LibRetroCore.retro_perf_counter counter) => { });
      retro_perf_callback.perf_start = new LibRetroCore.retro_perf_start_t((ref LibRetroCore.retro_perf_counter counter) => { });
      retro_perf_callback.perf_stop = new LibRetroCore.retro_perf_stop_t((ref LibRetroCore.retro_perf_counter counter) => { });
    }

    /// <summary>
    /// Initializes the LibRetro core
    /// </summary>
    public void Init()
    {
      _retro = new LibRetroCore(_corePath);
      try
      {
        LibRetroCore.retro_system_info system_info = new LibRetroCore.retro_system_info();
        _retro.retro_get_system_info(ref system_info);
        _systemInfo = new SystemInfo()
        {
          LibraryName = Marshal.PtrToStringAnsi(system_info.library_name),
          LibraryVersion = Marshal.PtrToStringAnsi(system_info.library_version),
          ValidExtensions = Marshal.PtrToStringAnsi(system_info.valid_extensions),
          NeedsFullPath = system_info.need_fullpath,
          BlockExtract = system_info.block_extract
        };
        if (string.IsNullOrEmpty(_systemDirectory))
          _systemDirectory = Path.GetDirectoryName(_corePath);
        if (string.IsNullOrEmpty(_saveDirectory))
          _saveDirectory = Path.GetDirectoryName(_corePath);
        _retro.retro_set_environment(retro_environment_cb);
        _retro.retro_set_video_refresh(retro_video_refresh_cb);
        _retro.retro_set_audio_sample(retro_audio_sample_cb);
        _retro.retro_set_audio_sample_batch(retro_audio_sample_batch_cb);
        _retro.retro_set_input_poll(retro_input_poll_cb);
        _retro.retro_set_input_state(retro_input_state_cb);
        _retro.retro_init();
      }
      catch
      {
        _retro.Dispose();
        _retro = null;
        throw;
      }
    }
    #endregion

    #region Events
    /// <summary>
    /// Fired when the <see cref="VideoBuffer"/> has been updated
    /// </summary>
    public event EventHandler VideoReady;
    protected virtual void OnVideoReady()
    {
      var handler = VideoReady;
      if (handler != null)
        handler(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fired when the FrameBuffer provided by the <see cref="GLContext"/> has been updated
    /// </summary>
    public event EventHandler FrameBufferReady;
    protected virtual void OnFrameBufferReady()
    {
      var handler = FrameBufferReady;
      if (handler != null)
        handler(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fired when the <see cref="AudioBuffer"/> has been updated
    /// </summary>
    public event EventHandler AudioReady;
    protected virtual void OnAudioReady()
    {
      var handler = AudioReady;
      if (handler != null)
        handler(this, EventArgs.Empty);
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Gets or sets a delegate to use for logging
    /// </summary>
    public LogDelegate LogDelegate
    {
      get { return _logDelegate; }
      set { _logDelegate = value; }
    }

    /// <summary>
    /// Gets or sets an OpenGL context that RetroGL cores can render to
    /// </summary>
    public IRetroGLContext GLContext
    {
      get { return _glContext; }
      set { _glContext = value; }
    }

    /// <summary>
    /// Gets or sets a controller interface to use for polling for input events
    /// </summary>
    public IRetroController Controller
    {
      get { return _retroController; }
      set
      {
        _retroController = value;
        _retroPad = value as IRetroPad;
        _retroAnalog = value as IRetroAnalog;
        _retroKeyboard = value as IRetroKeyboard;
        _retroPointer = value as IRetroPointer;
        _retroRumble = value as IRetroRumble;
      }
    }

    /// <summary>
    /// Gets or sets the system directory to pass to the LibRetro core
    /// </summary>
    public string SystemDirectory
    {
      get { return _systemDirectory; }
      set { _systemDirectory = value; }
    }

    /// <summary>
    /// Gets or sets the save directory to provide to the LibRetro core
    /// </summary>
    public string SaveDirectory
    {
      get { return _saveDirectory; }
      set { _saveDirectory = value; }
    }

    /// <summary>
    /// Gets the system information provided by the LibRetro core
    /// </summary>
    public SystemInfo SystemInfo
    {
      get { return _systemInfo; }
    }

    /// <summary>
    /// Gets the available variables defined by the LibRetro core and allows variables to be set
    /// </summary>
    public LibRetroVariables Variables
    {
      get { return _variables; }
    }

    /// <summary>
    /// Gets information about the video output of the LibRetro core.
    /// Potentially updated every time the video buffer/OpenGL context is updated
    /// </summary>
    public VideoInfo VideoInfo
    {
      get { return _videoInfo; }
    }

    /// <summary>
    /// Gets the timing information provided by the LibRetro core
    /// </summary>
    public TimingInfo TimingInfo
    {
      get { return _timingInfo; }
    }

    /// <summary>
    /// Gets the pixel data of the last video frame generated by the LibRetro core.
    /// Not used for RetroGL cores, they directly render to an OpenGL frame buffer provided by the <see cref="GLContext"/>
    /// </summary>
    public int[] VideoBuffer
    {
      get { return _videoBuffer; }
    }

    /// <summary>
    /// Gets the audio data of the last audio frame generated by the LibRetro core.
    /// </summary>
    public AudioBuffer AudioBuffer
    {
      get { return _audioBuffer; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the frontend can duplicate video frames 
    /// </summary>
    public bool CanDupeFrame
    {
      get { return _canDupe; }
      set { _canDupe = value; }
    }

    /// <summary>
    /// Gets the suggested performance level of the frontend.
    /// Values range from 1 - 15, where 1 is low performance and 15 high performance.
    /// </summary>
    public uint PerformanceLevel
    {
      get { return _performanceLevel; }
    }

    #endregion

    #region Public Methods
    /// <summary>
    /// Loads the specified game.
    /// </summary>
    /// <param name="path">The path to the game to load</param>
    /// <param name="data">If <see cref="SystemInfo.NeedsFullPath"/> is <c>false</c> this should be a byte array containing the game. Otherwise <c>null</c></param>
    /// <returns></returns>
    public bool LoadGame(string path, byte[] data)
    {
      LibRetroCore.retro_game_info gameInfo = new LibRetroCore.retro_game_info();
      gameInfo.path = path;
      gameInfo.meta = "";
      if (data == null || data.Length == 0)
        return Load(gameInfo);

      fixed (byte* p = &data[0])
      {
        gameInfo.data = (IntPtr)p;
        gameInfo.size = (uint)data.Length;
        return Load(gameInfo);
      }
    }

    /// <summary>
    /// Unloads the currently loaded game
    /// </summary>
    public void UnloadGame()
    {
      _retro.retro_unload_game();
    }

    /// <summary>
    /// Advances the currently loaded game by 1 frame
    /// </summary>
    public void Run()
    {
      if (_firstRun)
      {
        if (_glContext != null)
          _glContext.Init(_maxVideoWidth, _maxVideoHeight, _depthBuffer, _stencilBuffer, _bottomLeftOrigin);
        if (retro_hw_context_reset_cb != null)
          retro_hw_context_reset_cb();
        _firstRun = false;
      }
      else if (_glContext != null && _glContext.NeedsReset)
      {
        if (retro_hw_context_reset_cb != null)
          retro_hw_context_reset_cb();
        _glContext.NeedsReset = false;
      }
      _retro.retro_run();
    }

    /// <summary>
    /// Retrieves memory data of the specified type from the LibRetro core.
    /// </summary>
    /// <param name="memoryType">The type of memory to retrieve</param>
    /// <returns>A byte array containing the memory data</returns>
    public byte[] SaveState(LibRetroCore.RETRO_MEMORY memoryType)
    {
      uint size = _retro.retro_get_memory_size(memoryType);
      IntPtr ptr = _retro.retro_get_memory_data(memoryType);
      if (ptr == IntPtr.Zero)
        return null;
      byte[] saveBuffer = new byte[size];
      Marshal.Copy(ptr, saveBuffer, 0, saveBuffer.Length);
      return saveBuffer;
    }

    /// <summary>
    /// Loads memory data of the specified type into the LibRetro core
    /// </summary>
    /// <param name="memoryType">The type of memory to load</param>
    /// <param name="saveBuffer">A byte array containing the memory data</param>
    public void LoadState(LibRetroCore.RETRO_MEMORY memoryType, byte[] saveBuffer)
    {
      if (saveBuffer == null || saveBuffer.Length == 0)
        return;
      uint size = _retro.retro_get_memory_size(memoryType);
      IntPtr ptr = _retro.retro_get_memory_data(memoryType);
      if (ptr != IntPtr.Zero)
        Marshal.Copy(saveBuffer, 0, ptr, Math.Min(saveBuffer.Length, (int)size));
    }

    /// <summary>
    /// Serializes the current state of the LibRetro core into a byte array
    /// </summary>
    /// <returns>A byte array containg the current state of the LibRetro core</returns>
    public byte[] Serialize()
    {
      uint size = _retro.retro_serialize_size();
      byte[] buffer = new byte[size];
      fixed (byte* p = &buffer[0])
        _retro.retro_serialize((IntPtr)p, size);
      return buffer;
    }

    /// <summary>
    /// Unserializes a serialized state into the LibRetro core
    /// </summary>
    /// <param name="serializedState">A byte array containing the state</param>
    public void Unserialize(byte[] serializedState)
    {
      fixed (byte* p = &serializedState[0])
        _retro.retro_unserialize((IntPtr)p, (uint)serializedState.Length);
    }
    #endregion

    #region Load
    protected bool Load(LibRetroCore.retro_game_info gameInfo)
    {
      if (!_retro.retro_load_game(ref gameInfo))
      {
        Log(LibRetroCore.RETRO_LOG_LEVEL.WARN, "retro_load_game() failed");
        return false;
      }

      LibRetroCore.retro_system_av_info av = new LibRetroCore.retro_system_av_info();
      _retro.retro_get_system_av_info(ref av);
      _maxVideoWidth = (int)av.geometry.max_width;
      _maxVideoHeight = (int)av.geometry.max_height;
      _videoBuffer = new int[_maxVideoWidth * _maxVideoHeight];
      _audioBuffer = new AudioBuffer();
      _audioBuffer.Data = new short[2];
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
      return true;
    }
    #endregion

    #region LibRetro Environment Delegates
    unsafe bool retro_environment(LibRetroCore.RETRO_ENVIRONMENT cmd, IntPtr data)
    {
      //Log("Environment: {0}", cmd);
      switch (cmd)
      {
        case LibRetroCore.RETRO_ENVIRONMENT.SET_ROTATION:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_OVERSCAN:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_CAN_DUPE:
          *(bool*)data.ToPointer() = _canDupe;
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_MESSAGE:
          return SetMessage(data);
        case LibRetroCore.RETRO_ENVIRONMENT.SHUTDOWN:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_PERFORMANCE_LEVEL:
          _performanceLevel = *(uint*)data.ToPointer();
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "Core suggested SET_PERFORMANCE_LEVEL {0}", _performanceLevel);
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_SYSTEM_DIRECTORY:
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "returning system directory: " + _systemDirectory);
          return GetAndCreateDirectory(data, _systemDirectory);
        case LibRetroCore.RETRO_ENVIRONMENT.SET_PIXEL_FORMAT:
          return SetPixelFormat(data);
        case LibRetroCore.RETRO_ENVIRONMENT.SET_INPUT_DESCRIPTORS:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_KEYBOARD_CALLBACK:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_DISK_CONTROL_INTERFACE:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_HW_RENDER:
          return SetHardwareRenderer(data);
        case LibRetroCore.RETRO_ENVIRONMENT.GET_VARIABLE:
          return GetVariable(data);
        case LibRetroCore.RETRO_ENVIRONMENT.SET_VARIABLES:
          return SetVariables(data);
        case LibRetroCore.RETRO_ENVIRONMENT.GET_VARIABLE_UPDATE:
          return _variables.Updated;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_SUPPORT_NO_GAME:
          _supportsNoGame = true;
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_LIBRETRO_PATH:
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "returning libretro path: " + _corePath);
          *((IntPtr*)data.ToPointer()) = _unmanagedResources.StringToHGlobalAnsiCached(_corePath);
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_AUDIO_CALLBACK:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_FRAME_TIME_CALLBACK:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_RUMBLE_INTERFACE:
          Marshal.StructureToPtr(retro_rumble_interface, data, false);
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_INPUT_DEVICE_CAPABILITIES:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_LOG_INTERFACE:
          *(IntPtr*)data = Marshal.GetFunctionPointerForDelegate(retro_log_printf_cb);
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_PERF_INTERFACE:
          Marshal.StructureToPtr(retro_perf_callback, data, false);
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_LOCATION_INTERFACE:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_CORE_ASSETS_DIRECTORY:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.GET_SAVE_DIRECTORY:
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "returning save directory: " + _saveDirectory);
          return GetAndCreateDirectory(data, _saveDirectory);
        case LibRetroCore.RETRO_ENVIRONMENT.SET_CONTROLLER_INFO:
          return true;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_MEMORY_MAPS:
          return false;
        case LibRetroCore.RETRO_ENVIRONMENT.SET_GEOMETRY:
          return SetGeometry(data);
        default:
          Log(LibRetroCore.RETRO_LOG_LEVEL.WARN, "Unknkown retro_environment command {0} - {1}", (int)cmd, cmd);
          return false;
      }
    }

    protected bool SetMessage(IntPtr data)
    {
      LibRetroCore.retro_message msg = new LibRetroCore.retro_message();
      Marshal.PtrToStructure(data, msg);
      if (!string.IsNullOrEmpty(msg.msg))
        Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "LibRetro Message: {0}", msg.msg);
      return true;
    }

    protected bool GetAndCreateDirectory(IntPtr data, string directory)
    {
      try
      {
        Directory.CreateDirectory(directory);
      }
      catch (Exception ex)
      {
        Log(LibRetroCore.RETRO_LOG_LEVEL.ERROR, "Error creating directory '{0}' - {1}", directory, ex);
        return false;
      }
      *((IntPtr*)data.ToPointer()) = _unmanagedResources.StringToHGlobalAnsiCached(directory);
      return true;
    }

    protected bool SetPixelFormat(IntPtr data)
    {
      LibRetroCore.RETRO_PIXEL_FORMAT fmt = 0;
      int[] tmp = new int[1];
      Marshal.Copy(data, tmp, 0, 1);
      fmt = (LibRetroCore.RETRO_PIXEL_FORMAT)tmp[0];
      switch (fmt)
      {
        case LibRetroCore.RETRO_PIXEL_FORMAT.RGB565:
        case LibRetroCore.RETRO_PIXEL_FORMAT.XRGB1555:
        case LibRetroCore.RETRO_PIXEL_FORMAT.XRGB8888:
          _pixelFormat = fmt;
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "New pixel format set: {0}", _pixelFormat);
          return true;
        default:
          Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "Unrecognized pixel format: {0}", (int)_pixelFormat);
          return false;
      }
    }

    protected bool GetVariable(IntPtr data)
    {
      void** variablesPtr = (void**)data.ToPointer();
      IntPtr pKey = new IntPtr(*variablesPtr++);
      string key = Marshal.PtrToStringAnsi(pKey);
      Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "Requesting variable: {0}", key);
      VariableDescription variable;
      if (!_variables.TryGet(key, out variable))
      {
        Log(LibRetroCore.RETRO_LOG_LEVEL.WARN, "Variable {0}: not found", key);
        return false;
      }
      Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "Variable {0}: {1}", key, variable.SelectedOption);
      *variablesPtr = _unmanagedResources.StringToHGlobalAnsiCached(variable.SelectedOption).ToPointer();
      return true;
    }

    protected bool SetVariables(IntPtr data)
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
        _variables.AddOrUpdate(vd);
        Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "Set variable: Name: {0}, Description: {1}, Options: {2}", key, parts[0], parts[1].TrimStart(' '));
      }
      return true;
    }

    protected bool SetHardwareRenderer(IntPtr data)
    {
      //mupen64plus needs this, as well as 3dengine
      LibRetroCore.retro_hw_render_callback* info = (LibRetroCore.retro_hw_render_callback*)data.ToPointer();
      Log(LibRetroCore.RETRO_LOG_LEVEL.DEBUG, "SET_HW_RENDER: {0}, version={1}.{2}, dbg/cache={3}/{4}, depth/stencil = {5}/{6}{7}", info->context_type, info->version_minor, info->version_major, info->debug_context, info->cache_context, info->depth, info->stencil, info->bottom_left_origin ? " (bottomleft)" : "");
      if (_glContext == null)
        return false;

      info->get_current_framebuffer = Marshal.GetFunctionPointerForDelegate(retro_hw_get_current_framebuffer_cb);
      info->get_proc_address = Marshal.GetFunctionPointerForDelegate(retro_hw_get_proc_address_cb);
      retro_hw_context_reset_cb = Marshal.GetDelegateForFunctionPointer<LibRetroCore.retro_hw_context_reset_t>(info->context_reset);
      if (info->context_destroy != IntPtr.Zero)
        retro_hw_context_destroy_cb = Marshal.GetDelegateForFunctionPointer<LibRetroCore.retro_hw_context_reset_t>(info->context_destroy);
      _depthBuffer = info->depth;
      _stencilBuffer = info->stencil;
      _bottomLeftOrigin = info->bottom_left_origin;
      return true;
    }

    protected bool SetGeometry(IntPtr data)
    {
      LibRetroCore.retro_game_geometry geometry = *((LibRetroCore.retro_game_geometry*)data.ToPointer());
      VideoInfo videoInfo = new VideoInfo()
      {
        BufferWidth = (int)geometry.base_width,
        BufferHeight = (int)geometry.base_height,
        DAR = geometry.aspect_ratio
      };
      _videoInfo = videoInfo;
      return true;
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

      //Frame buffer ready
      if (data.ToInt32() == LibRetroCore.RETRO_HW_FRAME_BUFFER_VALID)
      {
        if (_glContext != null)
        {
          _glContext.OnFrameBufferReady((int)width, (int)height);
          OnFrameBufferReady();
        }
        return;
      }

      if (width * height > _videoBuffer.Length)
      {
        Log(LibRetroCore.RETRO_LOG_LEVEL.ERROR, "Unexpected libretro video buffer overrun?");
        return;
      }

      fixed (int* dst = &_videoBuffer[0])
        VideoBlitter.Blit(_pixelFormat, data, dst, (int)width, (int)height, (int)pitch);

      OnVideoReady();
    }

    uint retro_hw_get_current_framebuffer()
    {
      return _glContext != null ? _glContext.FrameBufferId : 0;
    }

    IntPtr retro_hw_get_proc_address(IntPtr sym)
    {
      return _glContext != null ? _glContext.GetProcAddress(sym) : IntPtr.Zero;
    }
    #endregion

    #region LibRetro Audio Delegates
    void retro_audio_sample(short left, short right)
    {
      _audioBuffer.Data[0] = left;
      _audioBuffer.Data[1] = right;
      _audioBuffer.Length = 2;
      OnAudioReady();
    }

    uint retro_audio_sample_batch(IntPtr data, uint frames)
    {
      int samples = (int)(frames * 2);
      if (_audioBuffer.Data.Length < samples)
        _audioBuffer.Data = new short[samples];
      Marshal.Copy(data, _audioBuffer.Data, 0, samples);
      _audioBuffer.Length = samples;
      OnAudioReady();
      return frames;
    }
    #endregion

    #region LibRetro Input Delegates
    void retro_input_poll() { }
        
    short retro_input_state(uint port, uint device, uint index, uint id)
    {
      switch ((LibRetroCore.RETRO_DEVICE)device)
      {
        case LibRetroCore.RETRO_DEVICE.POINTER:
          if (_retroPointer != null)
          {
            switch ((LibRetroCore.RETRO_DEVICE_ID_POINTER)id)
            {
              case LibRetroCore.RETRO_DEVICE_ID_POINTER.X: return _retroPointer.GetPointerX();
              case LibRetroCore.RETRO_DEVICE_ID_POINTER.Y: return _retroPointer.GetPointerY();
              case LibRetroCore.RETRO_DEVICE_ID_POINTER.PRESSED: return (short)(_retroPointer.IsPointerPressed() ? 1 : 0);
            }
          }
          return 0;
        case LibRetroCore.RETRO_DEVICE.KEYBOARD:
          if (_retroKeyboard == null)
            break;
          LibRetroCore.RETRO_KEY key = (LibRetroCore.RETRO_KEY)id;
          return (short)(_retroKeyboard.IsKeyPressed(key) ? 1 : 0);
        case LibRetroCore.RETRO_DEVICE.JOYPAD:
          if (_retroPad == null)
            break;
          LibRetroCore.RETRO_DEVICE_ID_JOYPAD button = (LibRetroCore.RETRO_DEVICE_ID_JOYPAD)id;
          return (short)(_retroPad.IsButtonPressed(port, button) ? 1 : 0);
        case LibRetroCore.RETRO_DEVICE.ANALOG:
          if (_retroAnalog == null)
            break;
          LibRetroCore.RETRO_DEVICE_INDEX_ANALOG analogIndex = (LibRetroCore.RETRO_DEVICE_INDEX_ANALOG)index;
          LibRetroCore.RETRO_DEVICE_ID_ANALOG analogDirection = (LibRetroCore.RETRO_DEVICE_ID_ANALOG)id;
          return _retroAnalog.GetAnalog(port, analogIndex, analogDirection);
      }
      return 0;
    }

    bool retro_set_rumble_state(uint port, LibRetroCore.retro_rumble_effect effect, ushort strength)
    {
      return _retroRumble != null ? _retroRumble.SetRumbleState(port, effect, strength) : false;
    }
    #endregion

    #region LibRetro Log Delegates
    protected void Log(LibRetroCore.RETRO_LOG_LEVEL level, string format, params object[] args)
    {
      if (_logDelegate != null)
        _logDelegate(level, string.Format(format, args));
    }

    unsafe void retro_log_printf(LibRetroCore.RETRO_LOG_LEVEL level, string fmt, IntPtr a0, IntPtr a1, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7, IntPtr a8, IntPtr a9, IntPtr a10, IntPtr a11, IntPtr a12, IntPtr a13, IntPtr a14, IntPtr a15)
    {
      if (_logDelegate == null)
        return;
      //avert your eyes, these things were not meant to be seen in c#
      //I'm not sure this is a great idea. It would suck for silly logging to be unstable. But.. I dont think this is unstable. The sprintf might just print some garbledy stuff.
      var args = new IntPtr[] { a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15 };
      int idx = 0;
      string logStr;
      try
      {
        logStr = Sprintf.sprintf(fmt, () => args[idx++]);
      }
      catch (Exception ex)
      {
        logStr = string.Format("Error in sprintf - {0}", ex);
      }
      _logDelegate(level, logStr);
    }
    #endregion

    #region IDisposable
    /// <summary>
    /// Disposes the LibRetro core, the OpenGL context and any unmanaged resources
    /// </summary>
    public void Dispose()
    {
      if (_retro != null)
      {
        //Mupen64 crashes if deinit is called when run hasn't been called
        if (!_firstRun)
          _retro.retro_deinit();
        _retro.Dispose();
        _retro = null;
      }
      if (_glContext != null)
      {
        _glContext.Dispose();
        _glContext = null;
      }
      _unmanagedResources.Dispose();
    }
    #endregion
  }
}