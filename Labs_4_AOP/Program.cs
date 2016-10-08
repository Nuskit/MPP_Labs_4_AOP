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
      Calculator a = new Calculator();
      foreach (var member in a.GetType().GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        Console.WriteLine(String.Format("{0}",member));
      Console.WriteLine(String.Format("Cal value {0}", a.Sum(5, 6)));

      foreach (var item in Assembly.GetExecutingAssembly().GetTypes())
      {
        Console.WriteLine(String.Format("Type {0}", item));
      }
      Console.ReadLine();
    }
  }
}    