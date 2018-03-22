﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Threading
{
  /// <summary>
  /// A class that acts like a priority access handler. Multiple priority locks can be held
  /// at the same time while low priority locks can only be held when no priority lock is held.
  /// </summary>
  public class AsyncPriorityLock
  {
    public struct Releaser : IDisposable
    {
      private AsyncPriorityLock _parent;
      private readonly bool _isPriority;

      internal Releaser(AsyncPriorityLock toRelease, bool isPriority)
      {
        _parent = toRelease;
        _isPriority = isPriority;
      }

      public void Dispose()
      {
        if (_parent != null)
        {
          if (_isPriority)
            _parent.PriorityRelease();
          else
            _parent.LowPriorityRelease();
        }
      }
    }

    private readonly object _syncObj = new object();

    //Tasks that complete immediately for fast path when there is no need to wait.
    private readonly Task<Releaser> _priorityReleaser;
    private readonly Task<Releaser> _lowPriorityReleaser;

    //Queue of waiting low priority lock requesters
    private readonly Queue<TaskCompletionSource<Releaser>> _waitingLowPriorities = new Queue<TaskCompletionSource<Releaser>>();

    //Current number of priority locks.
    private long _priorityLocks;

    public AsyncPriorityLock()
    {
      _priorityReleaser = Task.FromResult(new Releaser(this, true));
      _lowPriorityReleaser = Task.FromResult(new Releaser(this, false));
    }

    /// <summary>
    /// Acquires a low priority lock.
    /// The acquired Releaser must be disposed to release the low priority lock.
    /// </summary>
    /// <returns></returns>
    public Releaser LowPriorityLock()
    {
      return LowPriorityLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the low priority lock has been acquired.
    /// The acquired Releaser must be disposed to release the low priority lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> LowPriorityLockAsync()
    {
      if (Interlocked.Read(ref _priorityLocks) == 0)
      {
        return _lowPriorityReleaser;
      }
      else
      {
        lock (_syncObj)
        {
          var waiter = new TaskCompletionSource<Releaser>();
          _waitingLowPriorities.Enqueue(waiter);
          return waiter.Task;
        }
      }
    }

    /// <summary>
    /// Acquires a priority lock.
    /// The acquired Releaser must be disposed to release the priority lock.
    /// </summary>
    /// <returns></returns>
    public Releaser PriorityLock()
    {
      return PriorityLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the priority lock has been acquired.
    /// The acquired Releaser must be disposed to release the priority lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> PriorityLockAsync()
    {
      Interlocked.Increment(ref _priorityLocks);
      return _priorityReleaser;
    }

    private void LowPriorityRelease()
    {
    }

    private void PriorityRelease()
    {
      Interlocked.Decrement(ref _priorityLocks);
      lock (_syncObj)
      {
        while (_priorityLocks == 0 && _waitingLowPriorities.Count > 0)
        {
          TaskCompletionSource<Releaser> toWake = _waitingLowPriorities.Dequeue();
          toWake.SetResult(new Releaser(this, false));
        }
      }
    }
  }
}
