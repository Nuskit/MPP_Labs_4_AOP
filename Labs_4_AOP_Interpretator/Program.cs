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