﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Labs_4_AOP
{
  public interface ILogTarget
  {
    void WriteEnterLog(MethodBase currentMethod, Dictionary<string, Tuple<int, object>> currentParameters);
    void WriteExitLog(object returnValue);
    void WriteExitLog();
  }

  public enum ParameterType
  {
    DEF = 0,
    REF = 1,
    OUT = 2
  }
}
