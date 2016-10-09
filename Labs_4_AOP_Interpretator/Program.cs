using System;

namespace Labs_4_AOP_Interpretator
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length > 0)
      {
        MonoGenerator monoGenerator = new MonoGenerator(args[0]);
        monoGenerator.InjectLogger(args.Length > 1 ? args[1] : string.Empty);
        monoGenerator.EndChange(args[0]);
      }
      else
        Console.WriteLine("Not enter fileName");
    }
  }
}