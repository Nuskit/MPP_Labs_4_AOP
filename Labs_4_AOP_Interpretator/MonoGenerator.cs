using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public void InjectLogger()
    {
      injectTypes = new MonoTypes(assembly);
      foreach (var typeDef in assembly.MainModule.Types)
      {
        if (typeDef.CustomAttributes.Where(attr => attr.AttributeType.Resolve().Name == "LogAttribute").FirstOrDefault() != null)
        {
          for (int size = typeDef.Methods.Count - 1; size >= 0; size--)
          {
            var copyMethod = new MonoCloneClass(assembly).CloneMethod(typeDef.Methods[size], typeDef, MethodAttributes.Private | MethodAttributes.HideBySig, "{0}_MCopy");
            typeDef.Methods.Add(copyMethod);
            InjectLogMethod(typeDef, typeDef.Methods[size], copyMethod);
          }
        }
      }
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
      // ссылка на тип LogImplementation 
      public TypeReference logImplementationRef;
      // ссылка на тип LogImplementation 
      public MethodReference logImplementationConstructorRef;
      // ссылка на LogImplementation.OnEnter
      public MethodReference logImplementationOnEnter;
      // ссылка на LogImplementation.OnExit
      public MethodReference logImplementationOnExit;
      // ссылка на тип Dictionary<string, Pair<ParameterType, object>>
      public Type dictionaryType;
      //ссылка на первый параметр Dictionary
      public TypeReference dictStringObjectRef;
      //ссылка на конструктор
      public MethodReference dictConstructorRef;
      //ссылка на Dictionary.Add()
      public MethodReference dictMethodAddRef;
      //ссылка на тип Pair<ParameterType,object>
      public Type pairType;
      public TypeReference pairStringObjectRef;
      public MethodReference pairConstructorRef;
      //ссылка на GetParameterType
      public MethodReference getParameterRef;
      //ссылка на GetValue
      public MethodReference getValueRef;

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

        ImportLogImplementation(assembly);

        // ссылка на тип Dictionary<string,class Pair<ParameterType,object>>
        dictionaryType = Type.GetType("System.Collections.Generic.Dictionary`2[[System.String],[Labs_4_AOP_Interpretator.Pair`2[[Labs_4_AOP_Interpretator.ParameterType],[System.Object]]]]");
        dictStringObjectRef = assembly.MainModule.Import(dictionaryType);
        dictConstructorRef = assembly.MainModule.Import(dictionaryType.GetConstructor(Type.EmptyTypes));
        dictMethodAddRef = assembly.MainModule.Import(dictionaryType.GetMethod("Add"));
        //ссылка на тип Pair<ParameterType,object>
        pairType = Type.GetType("Labs_4_AOP_Interpretator.Pair`2[[Labs_4_AOP_Interpretator.ParameterType], [System.Object]]");
        pairStringObjectRef = assembly.MainModule.Import(pairType);
        pairConstructorRef = assembly.MainModule.Import(pairType.GetConstructors()[0]);

        //ссылка на GetParameterType
        getParameterRef = assembly.MainModule.Import(typeof(MonoGenerator).GetMethod("GetParameterType"));
        //ссылка на GetValue
        getValueRef = assembly.MainModule.Import(typeof(MonoGenerator).GetMethod("GetValue"));
      }

      private void ImportLogImplementation(AssemblyDefinition assembly)
      {
        var currentAssembly =AssemblyDefinition.ReadAssembly(System.Reflection.Assembly.GetEntryAssembly().Location);
        var logImplementation = currentAssembly.MainModule.Types.FirstOrDefault(x => x.Name == "LogImplementation");
        if (logImplementation != null)
        {
          var logAttribute =assembly.MainModule.Types.FirstOrDefault(x => x.Name == "LogAttribute");
          if (logAttribute != null)
          {
            new MonoCloneClass(assembly).CloneClass(logAttribute, logImplementation);
            logImplementationRef = logAttribute;
            logImplementationConstructorRef = logAttribute.Methods.FirstOrDefault(x => x.IsSpecialName && x.Name == ".ctor");
            // ссылка на MethodInterceptionAttribute.OnEnter
            logImplementationOnEnter = logAttribute.Methods.FirstOrDefault(x => x.Name == "OnEnter");
            // ссылка на MethodInterceptionAttribute.OnEnter
            logImplementationOnExit = logAttribute.Methods.FirstOrDefault(x => x.Name == "OnExit");
          }
        }
      }
    }

    private void InjectLogMethod(TypeDefinition typeDef, MethodDefinition injectMethod, MethodDefinition saveMethod)
    {
      var ilProc = injectMethod.Body.GetILProcessor();
      // необходимо установить InitLocals в true, так как если он находился в false (в методе изначально не было локальных переменных)
      // а теперь локальные переменные появятся - верификатор IL кода выдаст ошибку.
      injectMethod.Body.Instructions.Clear();
      injectMethod.Body.InitLocals = true;
      var logVariable = new VariableDefinition(injectTypes.logImplementationRef);
      var returnVariable = new VariableDefinition(injectMethod.ReturnType);
      ilProc.Body.Variables.Add(logVariable);

      ilProc.Emit(OpCodes.Newobj, injectTypes.logImplementationConstructorRef);
      ilProc.Emit(OpCodes.Stloc, logVariable);

      for (int i = 0; i <= injectMethod.Parameters.Count; i++)
        ilProc.Emit(OpCodes.Ldarg, i);
      ilProc.Emit(OpCodes.Call, saveMethod);
    //  ilProc.Emit(OpCodes.Stloc, returnVariable);
   //   ilProc.Emit(OpCodes.Ldloc, logVariable);
   //   ilProc.Emit(OpCodes.Ldloc, returnVariable);
   //   ilProc.Emit(OpCodes.Call, injectTypes.logImplementationOnExit);
      ilProc.Emit(OpCodes.Ret);
    }

    

    //public void MainWorking()
    //{
      
    //  foreach (var typeDef in assembly.MainModule.Types)
    //  {
    //    if (typeDef.CustomAttributes.Where(attr => attr.AttributeType.Resolve().BaseType.Name == "LogAttribure").FirstOrDefault() != null)

    //      foreach (var method in typeDef.Methods.Where(m => m.CustomAttributes.Where(
    //        attr => attr.AttributeType.Resolve().BaseType.Name == "MethodInterceptionAttribute").FirstOrDefault() != null))
    //      {
    //        var ilProc = method.Body.GetILProcessor();
    //        // необходимо установить InitLocals в true, так как если он находился в false (в методе изначально не было локальных переменных)
    //        // а теперь локальные переменные появятся - верификатор IL кода выдаст ошибку.
    //        method.Body.InitLocals = true;


    //        // создаем три локальных переменных для attribute, currentMethod и parameters
    //        var attributeVariable = new VariableDefinition(interceptionAttributeRef);
    //        var currentMethodVar = new VariableDefinition(methodBaseRef);
    //        var parametersVariable = new VariableDefinition(dictStringObjectRef);
    //        ilProc.Body.Variables.Add(attributeVariable);
    //        ilProc.Body.Variables.Add(currentMethodVar);
    //        ilProc.Body.Variables.Add(parametersVariable);
    //        Instruction firstInstruction = ilProc.Body.Instructions[0];
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Nop));
    //        // получаем текущий метод
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, getCurrentMethodRef));
    //        // помещаем результат со стека в переменную currentMethodVar
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Stloc, currentMethodVar));
    //        // загружаем на стек ссылку на текущий метод
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, currentMethodVar));
    //        // загружаем ссылку на тип InterceptionAttribute
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldtoken, interceptionAttributeRef));
    //        // Вызываем GetTypeFromHandle (в него транслируется typeof()) - эквивалент typeof(MethodInterceptionAttribute)
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, getTypeFromHandleRef));
    //        // теперь у нас на стеке текущий метод и тип MethodInterceptionAttribute. Вызываем Attribute.GetCustomAttribute
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, getCustomAttributeRef));
    //        // приводим результат к типу MethodInterceptionAttribute
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Castclass, interceptionAttributeRef));
    //        // сохраняем в локальной переменной attributeVariable
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Stloc, attributeVariable));
    //        // создаем новый Dictionary<stirng, object>
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Newobj, dictConstructorRef));
    //        // помещаем в parametersVariable
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Stloc, parametersVariable));
    //        foreach (var argument in method.Parameters)
    //        {
    //          //insert call method
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, argument));
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, getParameterRef));

    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, getValueRef));
    //          //загружаем тип аргумента
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Newobj, pairConstructorRef));

    //          //для каждого аргумента метода
    //          // загружаем на стек наш Dictionary<string,Pair<ParameterType,object>>
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, parametersVariable));
    //          // загружаем имя аргумента
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldstr, argument.Name));
    //          // загружаем значение аргумента
    //          // вызываем Dictionary.Add(string key, object value)
    //          ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, dictMethodAddRef));
    //        }
    //        // загружаем на стек сначала атрибут, потом параметры для вызова его метода OnEnter
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, attributeVariable));
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, currentMethodVar));
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, parametersVariable));
    //        // вызываем OnEnter. На стеке должен быть объект, на котором вызывается OnEnter и параметры метода
    //        ilProc.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Callvirt, interceptionAttributeOnEnter));



    //        //Блок выхода
    //        foreach (var retCommand in ilProc.Body.Instructions.Where(command => command.OpCode == OpCodes.Ret))
    //        {
    //          //add argument return
    //          ilProc.InsertBefore(retCommand, Instruction.Create(OpCodes.Callvirt, interceptionAttributeOnExit));
    //        }
    //      }
    //  }
    //}

    public ParameterType GetParameterType(ParameterDefinition argument)
    {
      return argument.IsOut
        ? ParameterType.Out
        : argument.ParameterType.IsByReference
          ? ParameterType.Reference
          : ParameterType.Default;
    }

    public object GetValue(ParameterDefinition argument)
    {
      return argument.Constant;
    }

  }
}