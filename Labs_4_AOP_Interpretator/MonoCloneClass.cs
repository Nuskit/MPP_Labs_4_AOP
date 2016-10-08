using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_4_AOP_Interpretator
{
  class MonoCloneClass
  {
    private AssemblyDefinition assembly;
    public MonoCloneClass(AssemblyDefinition treatedAssembly)
    {
      this.assembly = treatedAssembly;
    }

    public MethodDefinition CloneMethod(MethodDefinition templateMethod, TypeDefinition typeDef, MethodAttributes methodAtributes, String format = "{0}")
    {
      var newMethod = new MethodDefinition(String.Format(format, templateMethod.Name), methodAtributes, assembly.MainModule.Import(templateMethod.ReturnType));

      foreach (var variableDefinition in templateMethod.Body.Variables)
      {
        newMethod.Body.Variables.Add(new VariableDefinition(assembly.MainModule.Import(variableDefinition.VariableType)));
      }

      foreach (var parameterDefinition in templateMethod.Parameters)
      {
        newMethod.Parameters.Add(new ParameterDefinition(assembly.MainModule.Import(parameterDefinition.ParameterType)));
      }

      foreach (var instruction in templateMethod.Body.Instructions)
      {
        var constructorInfo = typeof(Instruction).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) },
null);
        var newInstruction = (Instruction)constructorInfo.Invoke(new[] { instruction.OpCode, instruction.Operand });

        var fieldDefinition = newInstruction.Operand as FieldDefinition;
        if (fieldDefinition != null)
        {
          assembly.MainModule.Import(fieldDefinition.FieldType);
          newInstruction.Operand = typeDef.Fields.First(x => x.Name == fieldDefinition.Name);
        }

        //don't copy all Type
        if (newInstruction.Operand is MethodReference)
        {
          
          var methodReference= (MethodReference)newInstruction.Operand;
 
          assembly.MainModule.Import(methodReference.GetElementMethod());
          assembly.MainModule.Import(methodReference.DeclaringType);
          assembly.MainModule.Import(methodReference.ReturnType);
        }
        if (newInstruction.Operand is TypeReference)
        {
          assembly.MainModule.Import(newInstruction.Operand as TypeReference);
        }
        newMethod.Body.Instructions.Add(newInstruction);
      }
      newMethod.DeclaringType = typeDef;
      assembly.MainModule.Import(newMethod);
      return newMethod;
    }
  }
}
