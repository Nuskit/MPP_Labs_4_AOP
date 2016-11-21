using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Labs_4_WeakDelegate_AOP;
using System.Linq;

namespace Test_labs_4_WeakDelegate_AOP
{
  [TestClass]
  public class TestWeak
  {
    private WeakDelegateTestClass TestWeakClass
    {
      get
      {
        return new WeakDelegateTestClass();
      }
    }

    [Weak]
    private static Delegate WeakDelegate
    {
      get
      {
        return WeakDelegateExample.WeakDelegate;
      }
      set
      {
        WeakDelegateExample.WeakDelegate = value;
      }
    }

    [TestMethod]
    public void TestDefaultTwoParam()
    {
      var testWeakClass = TestWeakClass;
      WeakDelegate = (Action<int, int>)testWeakClass.Sum;
      WeakDelegate.DynamicInvoke(5, 6);
      Assert.AreEqual(testWeakClass.IntValue, 11);
    }

    [TestMethod]
    public void TestDefaultActionWithZeroParam()
    {
      var testWeakClass = TestWeakClass;
      WeakDelegate=(Action)testWeakClass.NullFunc;
      WeakDelegate.DynamicInvoke();
      Assert.AreEqual(testWeakClass.IntValue, 1);
    }

    [TestMethod]
    public void TestDefaultThreeParam()
    {
      var testWeakClass = TestWeakClass;
      WeakDelegate=(Action<int, string, byte>)testWeakClass.ThreeParam;
      WeakDelegate.DynamicInvoke(5, "6", (byte)1);
      Assert.AreEqual(testWeakClass.IntValue, 12);
    }

    private event Func<int> FuncEvent;
    [TestMethod]
    public void TestDefaultWeakDelete()
    {
      WeakDelegate = (Func<int>)TestWeakClass.TestWeakDelete;
      FuncEvent += (Func<int>)WeakDelegate;

      Assert.AreEqual(FuncEvent.DynamicInvoke(), 5);
      GC.Collect();
      Assert.AreEqual(FuncEvent.DynamicInvoke(), 0);
    }

    [TestMethod]
    public void TestDefaultReturnValue()
    {
      var testWeakClass = TestWeakClass;
      WeakDelegate=(Func<int, int, int, int>)testWeakClass.Generic;
      Assert.AreEqual((int)WeakDelegate.DynamicInvoke(3, 4, 5), 12);
    }
  }
}
