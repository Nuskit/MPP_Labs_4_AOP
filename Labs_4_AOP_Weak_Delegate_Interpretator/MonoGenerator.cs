using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Labs_4_AOP_Weak_Delegate_Interpretator
{
  class MonoGenerator
  {
    private AssemblyDefinition assembly;
    private MonoTypes injectTypes;
    private Instruction firstInstruction;

    public MonoGenerator(string file)
    {
      assembly = AssemblyDefinition.ReadAssembly(file);
    }

    public void EndChange(string file)
    {
      assembly.Write(file);
    }

    public void InjectWeak()
    {
      InitializeInjecting();
      InsertWeakInProperty();
    }

    private void InsertWeakInProperty()
    {
      foreach (var currentType in assembly.MainModule.Types)
      {
        TryInjectWeak(currentType);
      }
    }

    private void InitializeInjecting()
    {
      injectTypes = new MonoTypes(assembly);
    }

    private void TryInjectWeak(TypeDefinition currentType)
    {
      foreach (var currentProperty in GetAllPropertyWithWeakAttribute(currentType))
        InjectSetMethod(currentType, currentProperty);
    }

    private IEnumerable<PropertyDefinition> GetAllPropertyWithWeakAttribute(TypeDefinition currentType)
    {
      return currentType.Properties.Where(
              prop =>
              (prop.CustomAttributes.Where(attr => attr.AttributeType.Name == "WeakAttribute").FirstOrDefault() != null)
           && (prop.PropertyType.FullName == "System.Delegate"));
    }

    private class MonoTypes
    {
      // link for GetCurrentMethod()
      public MethodReference getCurrentMethodRef;
      // link for Attribute.GetCustomAttribute()
      public MethodReference getCustomAttributeRef;
      // link Type.GetTypeFromHandle() - analog typeof()
      public MethodReference getTypeFromHandleRef;
      // link for GetProperty(string, BindingFlags)
      public MethodReference getPropertyRef;
      // link for Type of WeakAttribute
      public TypeDefinition weakAttributeRef;
      // link for WeakAttribute.WeakDelegate(Delegate)
      public MethodReference weakAttributeWeakDelegateRef;

      public MonoTypes(AssemblyDefinition assembly)
      {
        GetSystemTypeRef(assembly);
        GetLogAttibuteRef(assembly);
      }

      private void GetSystemTypeRef(AssemblyDefinition assembly)
      {
        getCurrentMethodRef = assembly.MainModule.Import(typeof(System.Reflection.MethodBase).GetMethod("GetCurrentMethod"));
        getCustomAttributeRef = assembly.MainModule.Import(typeof(Attribute).GetMethod("GetCustomAttribute", new Type[] { typeof(System.Reflection.MethodInfo), typeof(Type) }));
        getTypeFromHandleRef = assembly.MainModule.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
        var k=typeof(System.Reflection.PropertyInfo).GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
        getPropertyRef = assembly.MainModule.Import(typeof(Type).GetMethod("GetProperty", new Type[] { typeof(string), typeof(System.Reflection.BindingFlags) }));
      }

      private void GetLogAttibuteRef(AssemblyDefinition assembly)
      {
        weakAttributeRef = assembly.MainModule.GetType("Labs_4_WeakDelegate_AOP.WeakAttribute");
        weakAttributeWeakDelegateRef = weakAttributeRef.Methods.FirstOrDefault(x => x.Name == "WeakDelegate");
      }
    }

    private class Variable
    {
      public VariableDefinition attributeValue;
      //must be last
      public VariableDefinition returnValue;
    }

    private Variable InitializeVariable(TypeReference returnType)
    {
      return new Variable()
      {
        attributeValue = new VariableDefinition(injectTypes.weakAttributeRef),
        returnValue = new VariableDefinition(returnType)
      };
    }

    private static void AddVariable(ILProcessor ilProc, Variable variable)
    {
      foreach (var field in variable.GetType().GetFields())
        AddVariableValue(ilProc, variable, field);
    }

    private static void AddVariableValue(ILProcessor ilProc, Variable variable, System.Reflection.FieldInfo fieldInfo)
    {
      ilProc.Body.Variables.Add(GetFieldFromVariable(variable, fieldInfo));
    }

    private static VariableDefinition GetFieldFromVariable(Variable variable, System.Reflection.FieldInfo field)
    {
      return (VariableDefinition)field.GetValue(variable);
    }

    private void InjectSetMethod(TypeDefinition typeDef, PropertyDefinition currentProperty)
    {
      var ilProc = currentProperty.SetMethod.Body.GetILProcessor();
      firstInstruction = ilProc.Body.Instructions[0];
      FoundAttributeLink(ilProc, currentProperty);
    }

    private void IlEmit(ILProcessor ilProc,Instruction instruction)
    {
      ilProc.InsertBefore(firstInstruction, instruction);
    }

    private Variable InitializeVariableForMethod(MethodDefinition injectMethod, ILProcessor ilProc)
    {
      var variable = InitializeVariable(injectMethod.ReturnType);
      AddVariable(ilProc, variable);
      return variable;
    }

    private void FoundAttributeLink(ILProcessor ilProc, PropertyDefinition currentProperty)
    {
      IlEmit(ilProc, Instruction.Create(OpCodes.Ldtoken, currentProperty.DeclaringType));
      //call GetTypeFromHandle (it's tranclated typeof()) - analog typeof(WeakAttribute)
      IlEmit(ilProc, Instruction.Create(OpCodes.Call, injectTypes.getTypeFromHandleRef));
      IlEmit(ilProc, Instruction.Create(OpCodes.Ldstr, currentProperty.Name));
      IlEmit(ilProc, Instruction.Create(OpCodes.Ldc_I4, GetPropetyAttributes()));
      IlEmit(ilProc, Instruction.Create(OpCodes.Call, injectTypes.getPropertyRef));
      //load the link to type WeakAttribute
      IlEmit(ilProc, Instruction.Create(OpCodes.Ldtoken, injectTypes.weakAttributeRef));
      IlEmit(ilProc, Instruction.Create(OpCodes.Call, injectTypes.getTypeFromHandleRef));
      //call Attribute.GetCustomAttribute
      IlEmit(ilProc, Instruction.Create(OpCodes.Call, injectTypes.getCustomAttributeRef));
      // ive the result of the type WeakAttribute
      IlEmit(ilProc, Instruction.Create(OpCodes.Castclass, injectTypes.weakAttributeRef));
      IlEmit(ilProc, Instruction.Create(OpCodes.Ldarg, GetSetterValuePosition(currentProperty)));
      IlEmit(ilProc, Instruction.Create(OpCodes.Callvirt, injectTypes.weakAttributeWeakDelegateRef));
      IlEmit(ilProc, Instruction.Create(OpCodes.Starg, GetSetterValuePosition(currentProperty)));
    }

    private static ParameterDefinition GetSetterValuePosition(PropertyDefinition currentProperty)
    {
      return currentProperty.SetMethod.Parameters[0];
    }

    private static int GetPropetyAttributes()
    {
      //System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
      return 60;
    }
  }
}