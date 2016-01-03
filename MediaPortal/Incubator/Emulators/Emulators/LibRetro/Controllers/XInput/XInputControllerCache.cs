using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.XInput
{
  /// <summary>
  /// Helper class to cache the connected state of controllers.
  /// Polling the connected state of disconnected controllers causes high CPU load if done repeatedly.
  /// This class only updates the connected state every cacheTimeoutMs milliseconds
  /// </summary>
  class XInputControllerCache
  {
    protected Controller _controller;
    protected bool _isConnected;
    protected DateTime _lastCheck = DateTime.MinValue;

    public XInputControllerCache(Controller controller)
    {
      _controller = controller;
    }

    public Controller Controller { get { return _controller; } }

    public bool GetState(int cacheTimeoutMs, out State state)
    {
      DateTime now = DateTime.Now;
      if (!_isConnected && (now - _lastCheck).TotalMilliseconds < cacheTimeoutMs)
      {
        state = default(State);
        return false;
      }
      _lastCheck = now;
      _isConnected = _controller.GetState(out state);
      return _isConnected;
    }
  }
}
