using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Labs_4_AOP_Interpretator
{
  class MonoGenerator
  {
    private AssemblyDefinition assembly;
    private MonoTypes injectTypes;

    public MonoGenerator(string file)
    {
      assembly = AssemblyDefinition.ReadAssembly(file);
    }

    public void EndChange(string file)
    {
      assembly.MainModule.Import(typeof(StringBuilder).GetConstructor(new Type[] { typeof(int) }));
      assembly.Write(file);
    }

    public void InjectLogger()
    {
      injectTypes = new MonoTypes(assembly);
      foreach (var currentType in assembly.MainModule.Types)
      {
        if (currentType.CustomAttributes.Where(attr => attr.AttributeType.Resolve().Name == "LogAttribute").FirstOrDefault() != null)
        {
          InjectAllClassMethods(currentType, currentType);
          InjectAllNestedClasses(currentType);
        }
      }
    }

    private void InjectAllNestedClasses(TypeDefinition currentType)
    {
      foreach (var nestedClass in currentType.NestedTypes)
      {
        InjectAllClassMethods(nestedClass, currentType);
        InjectAllNestedClasses(nestedClass);
      }
    }

    private void InjectAllClassMethods(TypeDefinition currentClass, TypeDefinition parentClass)
    {
      for (int size = currentClass.Methods.Count - 1; size >= 0; size--)
      {
        if (currentClass.Methods[size].HasBody)
        {
          var copyMethod = new MonoCloneClass(assembly).CloneMethod(currentClass.Methods[size], currentClass, AttributesForCopyMethod(currentClass.Methods[size]), "{0}_MCopy");
          currentClass.Methods.Add(copyMethod);
          InjectLogMethod(currentClass, currentClass.Methods[size], copyMethod, parentClass);
        }
      }
    }

    private static MethodAttributes AttributesForCopyMethod(MethodDefinition method)
    {
      return MethodAttributes.Private | MethodAttributes.HideBySig | (method.Attributes & MethodAttributes.Static);
    }

    private class MonoTypes
    {
      // ссылка на GetCurrentMethod()
      public MethodReference getCurrentMethodRef;
      // ссылка на Attribute.GetCustomAttribute()
      public MethodReference getCustomAttributeRef;
      // ссылка на Type.GetTypeFromHandle() - аналог typeof()
      public MethodReference getTypeFromHandleRef;
      // ссылка на тип MethodBase 
      public TypeReference methodBaseRef;
      // ссылка на тип logAttribute 
      public TypeDefinition logAttributeRef;
      public MethodReference logAttributeConstructorRef;
      // ссылка на logAttribute.OnEnter
      public MethodReference logAttributeOnEnterRef;
      // ссылка на logAttribute.OnExit(object)
      public MethodReference logAttributeOnExitValueRef;
      // ссылка на logAttribute.OnExit(void)
      public MethodReference logAttributeOnExitRef;
      // ссылка на тип Dictionary<string, Tuple<int, object>>
      public TypeReference dictionaryTypeRef;
      //ссылка на конструктор
      public MethodReference dictConstructorRef;
      //ссылка на Dictionary.Add()
      public MethodReference dictMethodAddRef;
      //ссылка на GetValue
      //ссылка на тип Tuple<int,object>
      // ссылка на тип Dictionary<string, Tuple<int, object>>
      public TypeReference TupleTypeRef;
      //ссылка на конструктор
      public MethodReference TupleConstructorRef;

      public MonoTypes(AssemblyDefinition assembly)
      {
        // ссылка на GetCurrentMethod()
        getCurrentMethodRef = assembly.MainModule.Import(typeof(System.Reflection.MethodBase).GetMethod("GetCurrentMethod"));
        // ссылка на Attribute.GetCustomAttribute()
        getCustomAttributeRef = assembly.MainModule.Import(typeof(Attribute).GetMethod("GetCustomAttribute", new Type[] { typeof(System.Reflection.MethodInfo), typeof(Type) }));
        // ссылка на Type.GetTypeFromHandle() - аналог typeof()
        getTypeFromHandleRef = assembly.MainModule.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
        // ссылка на тип MethodBase 
        methodBaseRef = assembly.MainModule.Import(typeof(System.Reflection.MethodBase));
        // ссылка на тип MethodInterceptionAttribute 

        GetLogAttibuteRef(assembly);
        GetDictionaryRef(assembly);
        GetTupleRef(assembly);
      }

      private void GetTupleRef(AssemblyDefinition assembly)
      {
        TupleTypeRef = assembly.MainModule.Import(typeof(Tuple<int, object>));
        TupleConstructorRef = assembly.MainModule.Import(typeof(Tuple<int, object>).GetConstructor(new Type[] { typeof(int), typeof(object) }));
      }

      private void GetDictionaryRef(AssemblyDefinition assembly)
      {
        dictionaryTypeRef = assembly.MainModule.Import(typeof(Dictionary<string, Tuple<int, object>>));
        dictConstructorRef = assembly.MainModule.Import(typeof(Dictionary<string, Tuple<int, object>>).GetConstructor(Type.EmptyTypes));
        dictMethodAddRef = assembly.MainModule.Import(typeof(Dictionary<string, Tuple<int, object>>).GetMethod("Add"));
      }

      private void GetLogAttibuteRef(AssemblyDefinition assembly)
      {
        logAttributeRef = assembly.MainModule.GetType("Labs_4_AOP.LogAttribute");
        logAttributeConstructorRef = logAttributeRef.Methods.First(x => x.Name == ".ctor");//?
        // ссылка на MethodInterceptionAttribute.OnEnter
        logAttributeOnEnterRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnEnter");
        // ссылка на MethodInterceptionAttribute.OnExit(object)
        logAttributeOnExitValueRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnExit" && x.HasParameters);
        // ссылка на MethodInterceptionAttribute.OnExit(void)
        logAttributeOnExitRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnExit" && !x.HasParameters);
      }
    }

    private class Variable
    {
      public VariableDefinition attributeValue;
      public VariableDefinition dictionaryValue;
      public VariableDefinition returnValue;
    }

    private Variable InitializeVariable(TypeReference returnType)
    {
      return new Variable()
      {
        attributeValue = new VariableDefinition(injectTypes.logAttributeRef),
        dictionaryValue = new VariableDefinition(injectTypes.dictionaryTypeRef),
        returnValue = new VariableDefinition(returnType)
      };
    }

    private static void AddVariable(ILProcessor ilProc, Variable variable)
    {
      var fields = variable.GetType().GetFields();
      for (int i = fields.Length - 2; i >= 0; i--)
        AddVariableValue(ilProc, variable, fields[i]);
      AddVariableHaveValue(ilProc, variable, fields[fields.Length - 1]);
    }

    private static void AddVariableValue(ILProcessor ilProc, Variable variable, System.Reflection.FieldInfo fieldInfo)
    {
      ilProc.Body.Variables.Add(GetFieldFromVariable(variable, fieldInfo));
    }

    private static void AddVariableHaveValue(ILProcessor ilProc, Variable variable, System.Reflection.FieldInfo field)
    {
      if (IsValueType(GetFieldFromVariable(variable, field)))
        AddVariableValue(ilProc, variable, field);
    }

    private static VariableDefinition GetFieldFromVariable(Variable variable, System.Reflection.FieldInfo field)
    {
      return (VariableDefinition)field.GetValue(variable);
    }

    private void InjectLogMethod(TypeDefinition typeDef, MethodDefinition injectMethod, MethodDefinition saveMethod, TypeDefinition parentClass)
    {
      var ilProc = injectMethod.Body.GetILProcessor();
      // необходимо установить InitLocals в true, так как если он находился в false (в методе изначально не было локальных переменных)
      // а теперь локальные переменные появятся - верификатор IL кода выдаст ошибку.
      injectMethod.Body.Instructions.Clear();
      injectMethod.Body.ExceptionHandlers.Clear();
      injectMethod.Body.InitLocals = true;

      var variable = InitializeVariable(injectMethod.ReturnType);
      AddVariable(ilProc, variable);

      FoundAttributeLink(injectMethod, ilProc, variable.attributeValue, parentClass);


      //создаем новый Dictionary<stirng, object>
      ilProc.Emit(OpCodes.Newobj, injectTypes.dictConstructorRef);
      // помещаем в parametersVariable
      ilProc.Emit(OpCodes.Stloc, variable.dictionaryValue);
      foreach (var argument in injectMethod.Parameters)
      {
        ilProc.Emit(OpCodes.Ldloc, variable.dictionaryValue);
        ilProc.Emit(OpCodes.Ldstr, argument.Name);

        //загружаем тип параметра
        ilProc.Emit(OpCodes.Ldc_I4, GetParameterType(argument));
        //загружаем параметр
        ilProc.Emit(OpCodes.Ldarg, argument);
        BoxingPrimitiveType(argument, ilProc);
        //создаем Tuple<int,object>
        ilProc.Emit(OpCodes.Newobj, injectTypes.TupleConstructorRef);

        // вызываем Dictionary.Add(string key, object value)
        ilProc.Emit(OpCodes.Callvirt, injectTypes.dictMethodAddRef);
      }
      ilProc.Emit(OpCodes.Ldloc, variable.attributeValue);
      ilProc.Emit(OpCodes.Call, injectTypes.getCurrentMethodRef);
      ilProc.Emit(OpCodes.Ldloc, variable.dictionaryValue);
      // вызываем OnEnter. На стеке должен быть объект, на котором вызывается OnEnter и параметры метода
      ilProc.Emit(OpCodes.Callvirt, injectTypes.logAttributeOnEnterRef);


      CallCopyMethod(injectMethod, saveMethod, ilProc);
      WorkWithReturnValue(variable, ilProc);
      ilProc.Emit(OpCodes.Ret);
    }

    private void WorkWithReturnValue(Variable variable, ILProcessor ilProc)
    {
      if (IsValueType(variable.returnValue))
        AddReturnValueLog(variable, ilProc);
      else
        AddReturnLog(variable, ilProc);

      CallExitLogValue(IsValueType(variable.returnValue), ilProc);
      RepeatNotVoidReturnValue(ilProc, variable.returnValue);
    }

    private void AddReturnValueLog(Variable variable, ILProcessor ilProc)
    {
      ilProc.Emit(OpCodes.Stloc, variable.returnValue);
      ilProc.Emit(OpCodes.Ldloc, variable.attributeValue);
      ilProc.Emit(OpCodes.Ldloc, variable.returnValue);
      BoxingPrimitiveType(variable.returnValue, ilProc);
    }

    private static void AddReturnLog(Variable variable, ILProcessor ilProc)
    {
      ilProc.Emit(OpCodes.Ldloc, variable.attributeValue);
    }

    private void FoundAttributeLink(MethodDefinition injectMethod, ILProcessor ilProc, VariableDefinition attributeValue, TypeDefinition parentClass)
    {
      ilProc.Emit(OpCodes.Ldtoken, parentClass);
      ilProc.Emit(OpCodes.Call, injectTypes.getTypeFromHandleRef);
      // загружаем ссылку на тип LogAttribute
      ilProc.Emit(OpCodes.Ldtoken, injectTypes.logAttributeRef);
      // Вызываем GetTypeFromHandle (в него транслируется typeof()) - эквививалент typeof(LogAttribute)
      ilProc.Emit(OpCodes.Call, injectTypes.getTypeFromHandleRef);
      // теперь у нас на стеке текущий метод и тип LogAttribute. Вызываем Attribute.GetCustomAttribute
      ilProc.Emit(OpCodes.Call, injectTypes.getCustomAttributeRef);
      // приводим результат к типу LogAttribute
      ilProc.Emit(OpCodes.Castclass, injectTypes.logAttributeRef);
      // сохраняем в локальной переменной attributeValue
      ilProc.Emit(OpCodes.Stloc, attributeValue);
    }

    private static void RepeatNotVoidReturnValue(ILProcessor ilProc, VariableDefinition variable)
    {
      if (isNotVoidType(variable))
        ilProc.Emit(OpCodes.Ldloc, variable);
    }

    private void CallExitLogValue(bool isValueType, ILProcessor ilProc)
    {
      if (isValueType)
        ilProc.Emit(OpCodes.Callvirt, injectTypes.logAttributeOnExitValueRef);
      else
        ilProc.Emit(OpCodes.Callvirt, injectTypes.logAttributeOnExitRef);
    }

    private void BoxingPrimitiveType(VariableDefinition variable, ILProcessor ilProc)
    {
      if (IsPrimitiveType(variable))
        ilProc.Emit(OpCodes.Box, assembly.MainModule.Import(variable.VariableType));
    }

    private void BoxingPrimitiveType(ParameterDefinition parameter, ILProcessor ilProc)
    {
      if (IsPrimitiveType(parameter))
        ilProc.Emit(OpCodes.Box, assembly.MainModule.Import(parameter.ParameterType));
    }

    private static bool IsValueType(VariableDefinition variable)
    {
      return isNotVoidType(variable)
          || variable.VariableType.IsValueType
          || variable.VariableType.IsGenericInstance
          || variable.VariableType.IsDefinition
          || variable.VariableType.IsPointer;
    }

    private static bool isNotVoidType(VariableDefinition variable)
    {
      return !(variable.VariableType.Name == "Void");
    }

    private static bool IsPrimitiveType(VariableDefinition variable)
    {
      return variable.VariableType.IsPrimitive;
    }

    private bool IsPrimitiveType(ParameterDefinition parameter)
    {
      return parameter.ParameterType.IsPrimitive;
    }

    private static void CallCopyMethod(MethodDefinition injectMethod, MethodDefinition saveMethod, ILProcessor ilProc)
    {
      //load this
      LoadThisValue(ilProc, saveMethod);
      //load argument variable
      for (int i = 0; i < injectMethod.Parameters.Count; i++)
        LoadArgumentOnStack(ilProc, i + 1, GetParameterType(injectMethod.Parameters[i]));
      ilProc.Emit(OpCodes.Call, saveMethod);
    }

    private static void LoadThisValue(ILProcessor ilProc, MethodDefinition saveMethod)
    {
      if (!saveMethod.IsStatic)
        LoadArgumentOnStack(ilProc, 0, 0);
    }

    private static void LoadArgumentOnStack(ILProcessor ilProc, int numberArgument, int argumentType)
    {
      ilProc.Emit(IsDefaultArgument(argumentType) ? OpCodes.Ldarg : OpCodes.Ldarga, numberArgument);
    }

    private bool IsConstuctor(MethodDefinition injectMethod)
    {
      return injectMethod.IsConstructor;
    }

    private static bool IsDefaultArgument(int value)
    {
      return value == 0;
    }

    private static int GetParameterType(ParameterDefinition argument)
    {
      return argument.IsOut ? 2 : argument.ParameterType.IsByReference ? 1 : 0;
    }

  }
}