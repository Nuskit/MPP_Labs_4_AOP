using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP_Weak_Delegate_Interpretator
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length > 0)
      {
        MonoGenerator monoGenerator = new MonoGenerator(args[0]);
        monoGenerator.InjectWeak();
        monoGenerator.EndChange(args[0]);
      }
      else
        Console.WriteLine("Not enter fileName");
    }
  }
}
