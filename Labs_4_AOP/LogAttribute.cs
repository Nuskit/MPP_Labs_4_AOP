using System;
using System.Collections.Generic;
using System.Text;

namespace Labs_4_AOP
{
  [AttributeUsage(AttributeTargets.Class)]
  public class LogAttribute : Attribute
  {
    private StringBuilder logInformation = new StringBuilder(200);

    private string GetParameterBlock(Dictionary<string, Tuple<int, object>> currentParameters)
    {
      List<string> parameterInfo = new List<string>(currentParameters.Count);
      foreach (var parameterName in currentParameters.Keys)
      {
        parameterInfo.Add(String.Format("{0} = {1}", WriteNullReference(parameterName), WriteNullReference(currentParameters[parameterName].Item2)));
      }
      //add comma split and reference watching
      return parameterInfo.Count == 0 ? "none" : String.Join(" ,", parameterInfo);
    }

    public virtual void OnEnter(System.Reflection.MethodBase currentMethod,
      Dictionary<string, Tuple<int, object>> currentParameters)
    {
      logInformation.Clear();
      logInformation.AppendFormat("CLASS: {{{0}}}. METHOD: {{{1}}}. PARAMETERS: {{{2}}}",
        WriteNullReference(currentMethod.DeclaringType.Name), WriteNullReference(currentMethod.Name), GetParameterBlock(currentParameters));
    }

    private string WriteNullReference(object value)
    {
      return value == null ? "null" : value.ToString();
    }

    public virtual void OnExit(object returnValue)
    {
      logInformation.AppendFormat(" and RETURN: {{{0}}}", WriteNullReference(returnValue));
      OnExit();
    }

    public virtual void OnExit()
    {
      Console.WriteLine(logInformation);
    }
  }
}