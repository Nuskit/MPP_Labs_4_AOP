using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_WeakDelegate_AOP
{
  public class WeakDelegateExample
  {
    [Weak]
    public static Delegate WeakDelegate
    { get; set; }
  }
}
