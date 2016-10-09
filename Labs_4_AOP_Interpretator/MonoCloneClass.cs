using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Labs_4_AOP_Interpretator
{
  class MonoCloneClass
  {
    private AssemblyDefinition assembly;
    public MonoCloneClass(AssemblyDefinition treatedAssembly)
    {
      this.assembly = treatedAssembly;
    }

    public MethodDefinition CloneMethod(MethodDefinition templateMethod, TypeDefinition changingClass, MethodAttributes methodAtributes, String format = "{0}")
    {
      MethodDefinition newMethod = InitializeNewMethod(templateMethod, changingClass, methodAtributes, format);
      CopyMethodDefinition(templateMethod, newMethod);
      CopyInstructionDefinition(templateMethod, newMethod);
      return newMethod;
    }

    private MethodDefinition InitializeNewMethod(MethodDefinition templateMethod, TypeDefinition changingClass, MethodAttributes methodAtributes, string format)
    {
      var newMethod = new MethodDefinition(String.Format(format, templateMethod.Name), methodAtributes, assembly.MainModule.Import(templateMethod.ReturnType));
      newMethod.DeclaringType = changingClass;
      assembly.MainModule.Import(newMethod);
      return newMethod;
    }

    private void CopyInstructionDefinition(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
      //we must check all instructions for import
      System.Reflection.ConstructorInfo constructorInfo = GenerateInstuctionCompiler();
      foreach (var instruction in templateMethod.Body.Instructions)
      {
        var newInstruction = (Instruction)constructorInfo.Invoke(new[] { instruction.OpCode, instruction.Operand });
        newInstruction.Offset = instruction.Offset;
        ImportInstuctionDefinition(newInstruction);
        newMethod.Body.Instructions.Add(newInstruction);
      }
    }

    //for calling instuctions we must import all types
    private void ImportInstuctionDefinition(Instruction newInstruction)
    {
      ImprortFieldDefinition(newInstruction);
      ImportMethodDefinition(newInstruction);
      ImportTypeDefinition(newInstruction);
    }

    private void ImportTypeDefinition(Instruction newInstruction)
    {
      if (newInstruction.Operand is TypeReference)
      {
        assembly.MainModule.Import(newInstruction.Operand as TypeReference);
      }
    }

    private void ImportMethodDefinition(Instruction newInstruction)
    {
      //TODO: must import all type
      if (newInstruction.Operand is MethodReference)
      {
        var methodReference = (MethodReference)newInstruction.Operand;
        assembly.MainModule.Import(methodReference.GetElementMethod());
        assembly.MainModule.Import(methodReference.DeclaringType);
        assembly.MainModule.Import(methodReference.ReturnType);
      }
    }

    private void ImprortFieldDefinition(Instruction newInstruction)
    {
      var fieldDefinition = newInstruction.Operand as FieldDefinition;
      if (fieldDefinition != null)
      {
        assembly.MainModule.Import(fieldDefinition.FieldType);
      }
    }

    private static System.Reflection.ConstructorInfo GenerateInstuctionCompiler()
    {
      return typeof(Instruction).GetConstructor(
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, 
        new[] { typeof(OpCode), typeof(object) }, null);
    }

    private void CopyMethodDefinition(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
      CopyVariableDefinition(templateMethod, newMethod);
      CopyParameterDefiniton(templateMethod, newMethod);
      CopyExceptionDefinition(templateMethod, newMethod);
    }

    private static void CopyExceptionDefinition(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
      foreach (var exceptionDefinition in templateMethod.Body.ExceptionHandlers)
      {
        newMethod.Body.ExceptionHandlers.Add(exceptionDefinition);
      }
    }

    private void CopyParameterDefiniton(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
      foreach (var parameterDefinition in templateMethod.Parameters)
      {
        newMethod.Parameters.Add(new ParameterDefinition(assembly.MainModule.Import(parameterDefinition.ParameterType)));
      }
    }

    private void CopyVariableDefinition(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
      foreach (var variableDefinition in templateMethod.Body.Variables)
      {
        newMethod.Body.Variables.Add(new VariableDefinition(assembly.MainModule.Import(variableDefinition.VariableType)));
      }
    }
  }
}
