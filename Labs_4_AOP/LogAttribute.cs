using System;
using System.Collections.Generic;

namespace Labs_4_AOP
{
  [AttributeUsage(AttributeTargets.Class)]
  public class LogAttribute : Attribute
  {
    private static readonly Dictionary<LogTargetType, Lazy<ILogTarget>> targets = new Dictionary<LogTargetType, Lazy<ILogTarget>>()
    {
      { LogTargetType.Console, new Lazy<ILogTarget>(()=>new LogTargetConsole()) }
    };

    ILogTarget logTarget;

    public LogAttribute(LogTargetType target)
    {
      this.logTarget = targets[target].Value;
    }

    public virtual void OnEnter(System.Reflection.MethodBase currentMethod,
      Dictionary<string, Tuple<int, object>> currentParameters)
    {
      logTarget.WriteEnterLog(currentMethod, currentParameters);
    }

    public virtual void OnExit(object returnValue)
    {
      logTarget.WriteExitLog(returnValue);
    }

    public virtual void OnExit()
    {
      logTarget.WriteExitLog();
    }
  }
}