using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings
{
  public class SerializeableKeyValue<T1, T2>
  {
    public SerializeableKeyValue() { }
    public SerializeableKeyValue(T1 key, T2 value)
    {
      Key = key;
      Value = value;
    }

    public T1 Key { get; set; }
    public T2 Value { get; set; }
  }
}
