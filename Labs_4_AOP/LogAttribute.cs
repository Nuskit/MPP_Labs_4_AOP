﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  [AttributeUsage(AttributeTargets.Class)]
  public class LogAttribute : Attribute
  {
    private StringBuilder logInformation = new StringBuilder(200);

    private string GetParameterBlock(Dictionary<string, Tuple<int,object>> currentParameters)
    {
      StringBuilder parameterInfo = new StringBuilder(100);
      foreach (var parameterName in currentParameters.Keys)
      {
        
        parameterInfo.Append(String.Format("{0} = {1}", parameterName, currentParameters[parameterName].Item2.ToString()));
      }
      //add comma split and reference watching
      return parameterInfo.Length == 0 ? "none" : parameterInfo.ToString();
    }

    public virtual void OnEnter(System.Reflection.MethodBase currentMethod,
      Dictionary<string, Tuple<int, object>> currentParameters)
    {
      logInformation.Clear();
      logInformation.Append(String.Format("CLASS: {{{0}}}. METHOD: {{{1}}}. PARAMETERS: {{{2}}}",
        currentMethod.DeclaringType.Name, currentMethod.Name, GetParameterBlock(currentParameters)));
    }

    public virtual void OnExit(object returnValue)
    {
      logInformation.Append(String.Format("RETURN: {{{0}}}", returnValue.ToString()));
      OnExit();
    }

    public virtual void OnExit()
    {
      Console.WriteLine(logInformation);
    }
  }
}