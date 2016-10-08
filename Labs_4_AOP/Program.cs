using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  class Program
  {
    static void Main(string[]args)
    {
      Calculator a = new ChildCalculator();
      foreach (var method in a.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        Console.WriteLine(method);
      Console.WriteLine("--------------------");
      Console.WriteLine("Cal value {0}", a.Sum(5, 6));

      a.PropertyValue = 5;
      Console.WriteLine("Prop value {0}", a.PropertyValue);

      a.SumRec(0, null, 1);

      a.Empties();
      Console.WriteLine("--------------------");
      a.Clastering(null);
      Console.ReadLine();
    }
  }
}    