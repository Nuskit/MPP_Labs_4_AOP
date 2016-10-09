using System;

namespace Labs_4_AOP
{
  class Program
  {
    static void Main(string[]args)
    {
      Calculator testLog = InitializeClass();
      ShowAllMethods(testLog);
      CallMethodWithMethod(testLog);
      WorkWithProperty(testLog);
      UsePointType(testLog);
      CallMethodWithoutParam(testLog);
      CallMethodParamsType(testLog);
      CallTask(testLog);
      CallYield(testLog);
      CallFinalize();
      ConsoleWait();
    }

    private static void ConsoleWait()
    {
      Console.ReadLine();
    }

    private static void CallFinalize()
    {
      Console.WriteLine("Finalize and exception blocks");
      new ChildCalculatorTest();
      GC.Collect();
      ConsoleSplit();
    }

    private static void CallYield(Calculator testLog)
    {
      Console.WriteLine("Use yield");
      foreach (int i in testLog.GoYield(0, 5))
        Console.WriteLine("Yield value {0}", i);
      ConsoleSplit();
    }

    private static void CallTask(Calculator testLog)
    {
      Console.WriteLine("Load task without await");
      testLog.TaskTest();
      ConsoleSplit();
    }

    private static void CallMethodParamsType(Calculator testLog)
    {
      Console.WriteLine("Method with params parameters");
      testLog.Clastering(null);
      ConsoleSplit();
    }

    private static void CallMethodWithoutParam(Calculator testLog)
    {
      Console.WriteLine("Method without parameters");
      testLog.Empties();
      ConsoleSplit();
    }

    private static void UsePointType(Calculator testLog)
    {
      Console.WriteLine("Use not only primitive type");
      testLog.SumRec(0, null, 1);
      ConsoleSplit();
    }

    private static void WorkWithProperty(Calculator testLog)
    {
      Console.WriteLine("Get/Set Property");
      testLog.PropertyValue = 5;
      Console.WriteLine("Prop value {0}", testLog.PropertyValue);
      ConsoleSplit();
    }

    private static void CallMethodWithMethod(Calculator testLog)
    {
      Console.WriteLine("Call method with method");
      Console.WriteLine("Cal value {0}", testLog.Sum(5, 6));
      ConsoleSplit();
    }

    private static void ShowAllMethods(Calculator testLog)
    {
      Console.WriteLine("All methods");
      foreach (var method in testLog.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        Console.WriteLine(method);
      ConsoleSplit();
    }

    private static Calculator InitializeClass()
    {
      Console.WriteLine("Generate class");
      Calculator testLog = new ChildCalculatorTest();
      ConsoleSplit();
      return testLog;
    }

    private static void ConsoleSplit()
    {
      Console.WriteLine("-----------------------------------------------------------------");
      Console.WriteLine("-----------------------------------------------------------------");
    }
  }
}    