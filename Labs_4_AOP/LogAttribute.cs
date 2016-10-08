using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  [AttributeUsage(AttributeTargets.Class)]
  public class LogAttribute : Attribute
  {
  }
}