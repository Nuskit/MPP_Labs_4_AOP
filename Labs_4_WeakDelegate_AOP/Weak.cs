using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_WeakDelegate_AOP
{
  [AttributeUsage(AttributeTargets.Property)]
  public class WeakAttribute : Attribute
  {
    private class DelegateFactory
    {
      private MethodInfo weakMethod;
      private WeakReference weakTarget;

      public Delegate CreateDelegate(MethodInfo weakMethod, WeakReference weakReference)
      {
        SetOldDelegateParameters(weakMethod, weakReference);
        return weakMethod.ReturnType == typeof(void) ? GenerateAction() : GenerateFunc();
      }

      private void SetOldDelegateParameters(MethodInfo weakMethod, WeakReference weakTarget)
      {
        this.weakMethod = weakMethod;
        this.weakTarget = weakTarget;
      }

      private Delegate GenerateAction()
      {
        var argumentsType = CreateArgumentsType();
        return Expression.Lambda(GetDelegateType(), GenerateBlockCall(argumentsType, CallAction(argumentsType)), argumentsType).Compile();
      }

      private Delegate GenerateFunc()
      {
        var argumentsType = CreateArgumentsType();
        var returnVariable = Expression.Variable(weakMethod.ReturnType);

        return Expression.Lambda(GetDelegateType(), Expression.Block(GetVariablesList(argumentsType).Concat(new[] { returnVariable }),
          GenerateBlockCall(argumentsType, CallFunc(returnVariable, argumentsType)), returnVariable), argumentsType).Compile();
      }

      private ConditionalExpression GenerateBlockCall(ParameterExpression[] argumentsType, Expression[] ifHasTargetCallBack)
      {
        return Expression.IfThen(Expression.IsTrue(GetCheckIsAlive()), Expression.Block(GetVariablesList(argumentsType), ifHasTargetCallBack));
      }

      private Expression[] CallAction(ParameterExpression[] argumentsType)
      {
        return new Expression[] { CallDelegate(argumentsType) };
      }

      private Expression[] CallFunc(ParameterExpression variable, ParameterExpression[] argumentsType)
      {
        return new Expression[] { Expression.Assign(variable, CallDelegate(argumentsType)) };
      }

      private MethodCallExpression CallDelegate(ParameterExpression[] argumentsType)
      {
        return Expression.Call(
            instance: Expression.Convert(GetTarget(), weakMethod.DeclaringType),
            method: weakMethod,
            arguments: argumentsType);
      }

      private MemberExpression GetTarget()
      {
        return Expression.Property(Expression.Convert(Expression.Constant(weakTarget), typeof(WeakReference)), "Target");
      }

      private MemberExpression GetCheckIsAlive()
      {
        return Expression.Property(Expression.Convert(Expression.Constant(weakTarget), typeof(WeakReference)), "IsAlive");
      }

      private ParameterExpression[] CreateArgumentsType()
      {
        return weakMethod.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
      }

      private Type GetDelegateType()
      {
        return Expression.GetDelegateType(
            weakMethod.GetParameters().
            Select(param => param.ParameterType).
            Concat(new[] { weakMethod.ReturnType }).
            ToArray());
      }

      private List<ParameterExpression> GetVariablesList(ParameterExpression[] argumentsType)
      {
        return new List<ParameterExpression>(argumentsType.Select(argument => Expression.Variable(argument.Type)));
      }
    }

    private DelegateFactory delegateGenerator;
    private WeakReference weakReference;
    private MethodInfo weakMethod;

    public Delegate WeakDelegate(Delegate simpleDelegate)
    {
      GetDelegateDescription(simpleDelegate);
      delegateGenerator = new DelegateFactory();
      return ConvertToWeakDelegate;
    }

    private void GetDelegateDescription(Delegate simpleDelegate)
    {
      weakReference = new WeakReference(simpleDelegate.Target);
      weakMethod = simpleDelegate.Method;
    }

    private Delegate ConvertToWeakDelegate
    {
      get
      {
        return delegateGenerator.CreateDelegate(weakMethod, weakReference);
      }
    }
  }
}