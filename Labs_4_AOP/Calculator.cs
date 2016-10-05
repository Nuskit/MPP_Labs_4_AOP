using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  [Log]
  public class Calculator
  {
    public int Sum(int left, int right)
    {
      return left + right;
    }
  }
}
