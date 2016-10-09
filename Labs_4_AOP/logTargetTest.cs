using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP
{
  class LogTargetTest : ILogTarget
  {
    StringBuilder Buffer { get; set; }

    public LogTargetTest()
    {
      Buffer = new StringBuilder(200);
    }

    public void WriteEnterLog(MethodBase currentMethod, Dictionary<string, Tuple<int, object>> currentParameters)
    {
      Buffer.AppendFormat("c:{0}; m:{1}; p:{2}",
        WriteNullReference(currentMethod.DeclaringType.Name), 
        WriteNullReference(currentMethod.Name), 
        GetParameterBlock(currentParameters));
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
        parameterInfo.Add(String.Format("[{0}] {1} = {2}", (ParameterType)currentParameters[parameterName].Item1, WriteNullReference(parameterName), WriteNullReference(currentParameters[parameterName].Item2)));
      }
      return parameterInfo.Count == 0 ? "none" : String.Join(" ,", parameterInfo);
    }

    public void WriteExitLog()
    {
    }

    public void WriteExitLog(object returnValue)
    {
      Buffer.AppendFormat("; r:{0}", WriteNullReference(returnValue));
      WriteExitLog();
    }
  }
}
