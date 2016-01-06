using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
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
}
