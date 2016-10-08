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
      var a = SumRec(left, 1, right);
      return left + right;
    }

    private int SumRec(int left, int right, int third)
    {
      return left + right + third;
    }

    private int Privates(double koef)
    {
      var a = new SomeNamespace.testing();
      return 1;
    }
  }
}

namespace SomeNamespace
{
  public class testing
  {

  }
}
