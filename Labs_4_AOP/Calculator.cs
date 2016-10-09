using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  [Log(LogTargetType.Console)]
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

    public SomeClass ReturnSomeClass()
    {
      return new SomeClass();
    }

    public object Clastering(params object[] affaid)
    {
      return new object[] { affaid };
    }

    public async Task<int> TaskTest()
    {
      return await Task.Run(() => 
      {
        Thread.Sleep(250);
        Console.WriteLine("End Sleeping");
        return 5;
      });
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
      Console.WriteLine("Finalize work");
    }
  }

  [Log(LogTargetType.Console)]
  public class ChildCalculatorTest :Calculator
  {
    private int field;
    private int Field
    {
      get
      {
        return field << 2;
      }
      set
      {
        field = value;
      }
    }

    public ChildCalculatorTest():this(7)
    {
      Field = 7;
    }
    public ChildCalculatorTest(int s) :this(ref s,7)
    {
      s++;
    }

    private void RefByDefault(int s)
    {
      s++;
    }

    public ChildCalculatorTest(ref int s,int q)
    {
      RefByDefault(s);
      RefByObject(s);
      s <<= 1;
    }

    private void RefByObject(object s)
    {
    }

    public override void Empties()
    {
      var a = 5;
      ReturnSomeClass();
    }
  }

  public class SomeClass
  {

  }
}