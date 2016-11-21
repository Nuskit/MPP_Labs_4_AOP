using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_labs_4_WeakDelegate_AOP
{
  class WeakDelegateTestClass
  {
    public int IntValue { get; set; }

    public string stringValue { get; set; }

    public void Sum(int left,int right)
    {
      IntValue= left + right;
    }

    public void Multi(int left,string right)
    {
      IntValue = left * int.Parse(right);
    }

    public int Generic(int first,int second,int third)
    {
      return first + second + third;  
    }

    public void NullFunc()
    {
      IntValue = 1;
    }

    public void ThreeParam(int first,string second,byte third)
    {
      IntValue = first + int.Parse(second) + third;
    }

    public int TestWeakDelete()
    {
      return 5;
    }
  }
}
