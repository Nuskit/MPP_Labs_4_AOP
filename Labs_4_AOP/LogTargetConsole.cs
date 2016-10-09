using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Labs_4_AOP
{
  public class LogTargetConsole : ILogTarget
  {
    private StringBuilder logInformation = new StringBuilder(200);

    public void WriteEnterLog(MethodBase currentMethod, Dictionary<string, Tuple<int, object>> currentParameters)
    {
      logInformation.Clear();
      logInformation.AppendFormat("CLASS: {{{0}}}. METHOD: {{{1}}}. PARAMETERS: {{{2}}}",
        WriteNullReference(currentMethod.DeclaringType.Name), WriteNullReference(currentMethod.Name), GetParameterBlock(currentParameters));
    }

    public void WriteExitLog()
    {
      Console.WriteLine(logInformation);
    }

    public void WriteExitLog(object returnValue)
    {
      logInformation.AppendFormat(" and RETURN: {{{0}}}", WriteNullReference(returnValue));
      WriteExitLog();
    }

    private string WriteNullReference(object value)
    {
      return value == null ? "null" : value.ToString();
    }

    private string GetParameterBlock(Dictionary<string, Tuple<int, object>> currentParameters)
    {
      List<string> parameterInfo = new List<string>(currentParameters.Count);
      foreach (var parameterName in currentParameters.Keys)
      {
        parameterInfo.Add(String.Format("{0} = {1}", WriteNullReference(parameterName), WriteNullReference(currentParameters[parameterName].Item2)));
      }
      return parameterInfo.Count == 0 ? "none" : String.Join(" ,", parameterInfo);
    }
  }
}
