using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Labs_4_AOP_Interpretator
{
  class Program
  {
    static void Main(string[] args)
    {
      MonoGenerator monoGenerator = new MonoGenerator(args[0]);
      monoGenerator.InjectLogger();
      monoGenerator.EndChange(args[0]);
    }
  }
}