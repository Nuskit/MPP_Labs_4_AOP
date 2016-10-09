using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
      SumRec(left, "1s", right);
      return left + right;
    }

    public void SumRec(int left, string right, int third)
    {
      Third(new int[] { left, 10, third }, "ks");
    }

    public string Third(int[] a,string ks)
    {
      return ks;
    }

    private int Privates(double koef)
    {
      StringBuilder b = new StringBuilder(200);
      return 1;
    }

    public int PropertyValue
    {
      get;set;
    }

    public SomeClass clash()
    {
      return new SomeClass();
    }

    public object Clastering(params object[] affaid)
    {
      return new object[] { affaid };
    }

    public async Task<int> TaskTest()
    {
      return await Task.Run(() => { Thread.Sleep(300); return 5; });
    }

    public IEnumerable<int> GoYield(int start,int end)
    {
      {
        for (int i = start; i <= end; i++)
          yield return i;
      }
    }

    ~Calculator()
    {
      Console.WriteLine("Finalize");
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
      clash();
      base.Empties();
    }
  }

  public class SomeClass
  {

  }
}