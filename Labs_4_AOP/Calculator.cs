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
    public virtual void Empties()
    {}

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
      StringBuilder b = new StringBuilder(200);
      return 1;
    }
  }



  public class ChildCalculator :Calculator
  {
    public ChildCalculator():this(7)
    {
      var k = 6;
    }
    public ChildCalculator(int s) :this(ref s,7)
    {

    }

    public ChildCalculator(ref int s,int q):base()
    {

    }


    public override void Empties()
    {
      var a = 5;
      base.Empties();
    }
  }
}