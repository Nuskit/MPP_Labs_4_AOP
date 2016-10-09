using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

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
      assembly.Write(file);
    }

    public void InjectLogger(string advantageParam)
    {
      InitializeInjecting();
      InsertLoggerInClasses(advantageParam);
    }

    private void InsertLoggerInClasses(string advantageParam)
    {
      foreach (var currentType in assembly.MainModule.Types)
      {
        TryInjectLogger(advantageParam, currentType);
      }
    }

    private void InitializeInjecting()
    {
      injectTypes = new MonoTypes(assembly);
    }

    private void TryInjectLogger(string advantageParam, TypeDefinition currentType)
    {
      if (IsTypeHaveLogAttribute(currentType))
      {
        InjectAllClassMethods(currentType, currentType);
        InjectExtraAttribute(advantageParam, currentType);
      }
    }

    private static bool IsTypeHaveLogAttribute(TypeDefinition currentType)
    {
      return currentType.CustomAttributes.Where(attr => attr.AttributeType.Resolve().Name == "LogAttribute").FirstOrDefault() != null;
    }

    private void InjectExtraAttribute(string advantageParam, TypeDefinition currentType)
    {
      if (advantageParam == "-r")
        InjectAllNestedClasses(currentType);
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
        TryCopyMethod(currentClass, parentClass, size);
      }
    }

    private void TryCopyMethod(TypeDefinition currentClass, TypeDefinition parentClass, int currentMethod)
    {
      if (IsCanCopyMethod(currentClass, currentMethod))
      {
        MethodDefinition copyMethod = CloneMethodAndInsertInClass(currentClass, currentMethod);
        InjectLogMethod(currentClass, currentClass.Methods[currentMethod], copyMethod, parentClass);
      }
    }

    private static bool IsCanCopyMethod(TypeDefinition currentClass, int size)
    {
      return currentClass.Methods[size].HasBody;
    }

    private MethodDefinition CloneMethodAndInsertInClass(TypeDefinition currentClass, int size)
    {
      var copyMethod = new MonoCloneClass(assembly).CloneMethod(currentClass.Methods[size], currentClass, AttributesForCopyMethod(currentClass.Methods[size]), "{0}_MCopy");
      currentClass.Methods.Add(copyMethod);
      return copyMethod;
    }

    private static MethodAttributes AttributesForCopyMethod(MethodDefinition method)
    {
      return MethodAttributes.Private | MethodAttributes.HideBySig | (method.Attributes & MethodAttributes.Static);
    }

    private class MonoTypes
    {
      // link for GetCurrentMethod()
      public MethodReference getCurrentMethodRef;
      // link for Attribute.GetCustomAttribute()
      public MethodReference getCustomAttributeRef;
      // link Type.GetTypeFromHandle() - analog typeof()
      public MethodReference getTypeFromHandleRef;
      // link for Type of MethodBase 
      public TypeReference methodBaseRef;
      // link for Type of LogAttribute 
      public TypeDefinition logAttributeRef;
      // link for LogAttribute.ctor()
      public MethodReference logAttributeConstructorRef;
      // link for LogAttribute.OnEnter(some parameters)
      public MethodReference logAttributeOnEnterRef;
      // link for LogAttribute.OnExit(object)
      public MethodReference logAttributeOnExitValueRef;
      // link for LogAttribute.OnExit(void)
      public MethodReference logAttributeOnExitRef;
      // link for Type of Dictionary<string, Tuple<int, object>>
      public TypeReference dictionaryTypeRef;
      //link for Dictionary<string, Tuple<int, object>>.ctor()
      public MethodReference dictConstructorRef;
      //link for Dictionary.Add()
      public MethodReference dictMethodAddRef;
      //link for Type of Tuple<int, object>
      public TypeReference TupleTypeRef;
      //link for Tuple<int, object>.ctor(int, object)
      public MethodReference TupleConstructorRef;

      public MonoTypes(AssemblyDefinition assembly)
      {
        GetSystemTypeRef(assembly);
        GetLogAttibuteRef(assembly);
        GetDictionaryRef(assembly);
        GetTupleRef(assembly);
      }

      private void GetSystemTypeRef(AssemblyDefinition assembly)
      {
        getCurrentMethodRef = assembly.MainModule.Import(typeof(System.Reflection.MethodBase).GetMethod("GetCurrentMethod"));
        getCustomAttributeRef = assembly.MainModule.Import(typeof(Attribute).GetMethod("GetCustomAttribute", new Type[] { typeof(System.Reflection.MethodInfo), typeof(Type) }));
        getTypeFromHandleRef = assembly.MainModule.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
        methodBaseRef = assembly.MainModule.Import(typeof(System.Reflection.MethodBase));
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
        logAttributeConstructorRef = logAttributeRef.Methods.First(x => x.Name == ".ctor");
        logAttributeOnEnterRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnEnter");
        logAttributeOnExitValueRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnExit" && x.HasParameters);
        logAttributeOnExitRef = logAttributeRef.Methods.FirstOrDefault(x => x.Name == "OnExit" && !x.HasParameters);
      }
    }

    private class Variable
    {
      public VariableDefinition attributeValue;
      public VariableDefinition dictionaryValue;
      //must be last
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
      AddVariableHaveReturn(ilProc, variable, fields[fields.Length - 1]);
    }

    private static void AddVariableValue(ILProcessor ilProc, Variable variable, System.Reflection.FieldInfo fieldInfo)
    {
      ilProc.Body.Variables.Add(GetFieldFromVariable(variable, fieldInfo));
    }

    private static void AddVariableHaveReturn(ILProcessor ilProc, Variable variable, System.Reflection.FieldInfo field)
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
      InitializeInjectLog(injectMethod);
      //initialize local log variable in method
      Variable variable = InitializeVariableForMethod(injectMethod, ilProc);
      FoundAttributeLink(injectMethod, ilProc, variable.attributeValue, parentClass);
      PrintEnterParametersInMethod(injectMethod, ilProc, variable);
      CallCopyMethod(injectMethod, saveMethod, ilProc);
      WorkWithReturnValue(variable, ilProc);
      ExitFromTheMethod(ilProc);
    }

    private void PrintEnterParametersInMethod(MethodDefinition injectMethod, ILProcessor ilProc, Variable variable)
    {
      GenerateDictionaryVariable(injectMethod, ilProc, variable);
      CallLogEnterMethod(ilProc, variable);
    }

    private static void ExitFromTheMethod(ILProcessor ilProc)
    {
      ilProc.Emit(OpCodes.Ret);
    }

    private void CallLogEnterMethod(ILProcessor ilProc, Variable variable)
    {
      //load on stack LogAttribute - this
      ilProc.Emit(OpCodes.Ldloc, variable.attributeValue);
      //call GetCurentMethod() and load on stack first parameters
      ilProc.Emit(OpCodes.Call, injectTypes.getCurrentMethodRef);
      //load on stack second parameters Dictionary
      ilProc.Emit(OpCodes.Ldloc, variable.dictionaryValue);
      // call LogAttribute.OnEnter(some parameters)
      ilProc.Emit(OpCodes.Callvirt, injectTypes.logAttributeOnEnterRef);
    }

    private void GenerateDictionaryVariable(MethodDefinition injectMethod, ILProcessor ilProc, Variable variable)
    {
      CreateDictionaryVariable(ilProc, variable);
      AddInDictionaryAllElements(injectMethod, ilProc, variable);
    }

    private void AddInDictionaryAllElements(MethodDefinition injectMethod, ILProcessor ilProc, Variable variable)
    {
      foreach (var argument in injectMethod.Parameters)
      {
        AddSimpleElementInDictionary(ilProc, variable, argument);
      }
    }

    private void CreateDictionaryVariable(ILProcessor ilProc, Variable variable)
    {
      //create new Dictionary<string, Tuple<int, object>
      ilProc.Emit(OpCodes.Newobj, injectTypes.dictConstructorRef);
      // take off the stack into dictionaryVariable
      ilProc.Emit(OpCodes.Stloc, variable.dictionaryValue);
    }

    private void AddSimpleElementInDictionary(ILProcessor ilProc, Variable variable, ParameterDefinition argument)
    {
      PartInitializeDictionaryElement(ilProc, variable, argument);
      CreateTupleVariable(ilProc, argument);
      AddElementInDictionary(ilProc);
    }

    private void AddElementInDictionary(ILProcessor ilProc)
    {
      // call Dictionary.Add(string key, object value)
      ilProc.Emit(OpCodes.Callvirt, injectTypes.dictMethodAddRef);
    }

    private static void PartInitializeDictionaryElement(ILProcessor ilProc, Variable variable, ParameterDefinition argument)
    {
      ilProc.Emit(OpCodes.Ldloc, variable.dictionaryValue);
      ilProc.Emit(OpCodes.Ldstr, argument.Name);
    }

    private void CreateTupleVariable(ILProcessor ilProc, ParameterDefinition argument)
    {
      //load parameter Type
      ilProc.Emit(OpCodes.Ldc_I4, GetParameterType(argument));
      //load value parameter
      ilProc.Emit(OpCodes.Ldarg, argument);
      BoxingPrimitiveType(argument, ilProc);
      //create Tuple<int,object>
      ilProc.Emit(OpCodes.Newobj, injectTypes.TupleConstructorRef);
    }

    private Variable InitializeVariableForMethod(MethodDefinition injectMethod, ILProcessor ilProc)
    {
      var variable = InitializeVariable(injectMethod.ReturnType);
      AddVariable(ilProc, variable);
      return variable;
    }

    private static void InitializeInjectLog(MethodDefinition injectMethod)
    {
      injectMethod.Body.Instructions.Clear();
      injectMethod.Body.ExceptionHandlers.Clear();
      //need to initialize the local variables
      injectMethod.Body.InitLocals = true;
    }

    private void WorkWithReturnValue(Variable variable, ILProcessor ilProc)
    {
      ChooseExitMethodDependingHaveReturnValue(variable, ilProc);
      CallExitLogValue(IsValueType(variable.returnValue), ilProc);
      RepeatNotVoidReturnValue(ilProc, variable.returnValue);
    }

    private void ChooseExitMethodDependingHaveReturnValue(Variable variable, ILProcessor ilProc)
    {
      if (IsValueType(variable.returnValue))
        AddReturnValueLog(variable, ilProc);
      else
        AddReturnLog(variable, ilProc);
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
      //load the link to type class having LogAttribute
      ilProc.Emit(OpCodes.Ldtoken, parentClass);
      ilProc.Emit(OpCodes.Call, injectTypes.getTypeFromHandleRef);
      //load the link to type LogAttribute
      ilProc.Emit(OpCodes.Ldtoken, injectTypes.logAttributeRef);
      //call GetTypeFromHandle (it's tranclated typeof()) - analog typeof(LogAttribute)
      ilProc.Emit(OpCodes.Call, injectTypes.getTypeFromHandleRef);
      //call Attribute.GetCustomAttribute
      ilProc.Emit(OpCodes.Call, injectTypes.getCustomAttributeRef);
      // ive the result of the type LogAttribute
      ilProc.Emit(OpCodes.Castclass, injectTypes.logAttributeRef);
      //save in local variable attributeValue
      ilProc.Emit(OpCodes.Stloc, attributeValue);
    }

    private static void RepeatNotVoidReturnValue(ILProcessor ilProc, VariableDefinition variable)
    {
      if (IsNotVoidType(variable))
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
      return IsNotVoidType(variable)
          || variable.VariableType.IsValueType
          || variable.VariableType.IsGenericInstance
          || variable.VariableType.IsDefinition
          || variable.VariableType.IsPointer;
    }

    private static bool IsNotVoidType(VariableDefinition variable)
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
      //call old method
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

    //default -0
    //reference -1
    //out -2
    private static int GetParameterType(ParameterDefinition argument)
    {
      return argument.IsOut ? 2 : argument.ParameterType.IsByReference ? 1 : 0;
    }

    private static bool IsDefaultArgument(int value)
    {
      return value == 0;
    }
  }
}