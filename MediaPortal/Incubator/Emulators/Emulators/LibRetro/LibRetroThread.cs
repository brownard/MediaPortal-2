using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroThread : IDisposable
  {
    #region ILogger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    protected Thread _controllerThread;
    protected ManualResetEventSlim _initWaitHandle;
    protected ManualResetEventSlim _runWaitHandle;
    protected ManualResetEventSlim _pauseWaitHandle;
    protected bool _isInit;
    protected volatile bool _doRun;

    public event EventHandler Initializing;
    public event EventHandler Started;
    public event EventHandler Running;
    public event EventHandler Finishing;
    public event EventHandler Finished;
    public event EventHandler Paused;
    public event EventHandler UnPaused;

    public LibRetroThread()
    {
      _initWaitHandle = new ManualResetEventSlim();
      _runWaitHandle = new ManualResetEventSlim();
      _pauseWaitHandle = new ManualResetEventSlim(true);
    }

    public bool IsInit
    {
      get { return _isInit; }
      set { _isInit = value; }
    }

    public bool Init()
    {
      try
      {
        _controllerThread = new Thread(ThreadProc) { Name = "LibRetroThread" };
        _controllerThread.Start();
        _initWaitHandle.Wait();
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroThread: Error starting thread", ex);
        return false;
      }
      return _isInit;
    }

    public void Run()
    {
      _doRun = true;
      _runWaitHandle.Set();
    }

    public void Stop()
    {
      _doRun = false;
      TrySet(_runWaitHandle);
      TrySet(_pauseWaitHandle);
      if (_controllerThread != null)
      {
        _controllerThread.Join();
        _controllerThread = null;
      }
    }

    public void Pause()
    {
      _pauseWaitHandle.Reset();
    }

    public void UnPause()
    {
      _pauseWaitHandle.Set();
    }

    protected void Fire(EventHandler handler)
    {
      if (handler != null)
        handler(this, EventArgs.Empty);
    }

    protected void ThreadProc()
    {
      DoInit();
      if (!_isInit)
        return;
      _runWaitHandle.Wait();
      DoRun();
    }

    protected void DoInit()
    {
      try
      {
        Fire(Initializing);
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroThread: Error in DoInit", ex);
      }
      finally
      {
        _initWaitHandle.Set();
      }
    }

    protected void DoRun()
    {
      try
      {
        Fire(Started);
        while (_doRun)
        {
          Fire(Running);
          CheckPauseState();
        }
        Fire(Finishing);
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroThread: Error in DoRun", ex);
      }
      finally
      {
        Fire(Finished);
      }
    }

    protected void CheckPauseState()
    {
      if (!_pauseWaitHandle.IsSet)
      {
        Fire(Paused);
        _pauseWaitHandle.Wait();
        Fire(UnPaused);
      }
    }

    protected void TrySet(ManualResetEventSlim resetEvent)
    {
      if (resetEvent != null && !resetEvent.IsSet)
        resetEvent.Set();
    }

    protected void TryDispose(ref ManualResetEventSlim resetEvent)
    {
      if (resetEvent != null)
      {
        resetEvent.Dispose();
        resetEvent = null;
      }
    }

    public void Dispose()
    {
      Stop();
      TryDispose(ref _initWaitHandle);
      TryDispose(ref _runWaitHandle);
      TryDispose(ref _pauseWaitHandle);
    }
  }
}