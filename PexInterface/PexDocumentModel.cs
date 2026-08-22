using System;
using System.Collections.Generic;
using System.Linq;
using static PexInterface.PexReader;

namespace PexInterface
{
    /// <summary>
    /// Represents an immutable managed snapshot consumed by analysis and code generation.
    /// </summary>
    internal sealed class PexDocumentModel
    {
        internal PexDocumentModel(
            IReadOnlyList<PexModelString> strings,
            IReadOnlyList<PexModelObject> objects,
            IReadOnlyList<ushort> debugFunctionNames)
        {
            Strings = strings;
            Objects = objects;
            DebugFunctionNames = debugFunctionNames;
        }

        internal IReadOnlyList<PexModelString> Strings { get; }
        internal IReadOnlyList<PexModelObject> Objects { get; }
        internal IReadOnlyList<ushort> DebugFunctionNames { get; }

        internal static PexDocumentModel Create(PexReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return new PexDocumentModel(
                reader.StringTable.Select(item => new PexModelString(item.Index, item.Value)).ToArray(),
                reader.Objects.Select(CreateObject).ToArray(),
                reader.DebugInfo.Functions.Select(function => function.FunctionNameIndex).ToArray());
        }

        private static PexModelObject CreateObject(PexObject source)
        {
            return new PexModelObject(
                source.NameIndex,
                source.ParentClassNameIndex,
                source.AutoStateNameIndex,
                source.Variables.Select(variable => new PexModelVariable(
                    variable.VarIndex,
                    variable.NameIndex,
                    variable.TypeNameIndex,
                    variable.DataValue)).ToArray(),
                source.Properties.Select(property => new PexModelProperty(
                    property.NameIndex,
                    property.TypeIndex,
                    property.AutoVarNameIndex)).ToArray(),
                source.States.Select(CreateState).ToArray());
        }

        private static PexModelState CreateState(PexState source)
        {
            return new PexModelState(source.NameIndex, source.Functions.Select(CreateFunction).ToArray());
        }

        private static PexModelFunction CreateFunction(PexFunction source)
        {
            return new PexModelFunction(
                source.FunctionNameIndex,
                source.ReturnTypeIndex,
                source.Flags,
                source.Parameters.Select(parameter =>
                    new PexModelFunctionParameter(parameter.NameIndex, parameter.TypeIndex)).ToArray(),
                source.Instructions.Select(instruction => new PexModelInstruction(
                    instruction.GetOpcodeName(),
                    instruction.Arguments.Select(argument =>
                        new PexModelInstructionArgument(argument.Type, argument.Value)).ToArray())).ToArray());
        }
    }

    internal sealed class PexModelString
    {
        internal PexModelString(ushort index, string value) { Index = index; Value = value ?? string.Empty; }
        internal ushort Index { get; }
        internal string Value { get; }
    }

    internal sealed class PexModelObject
    {
        internal PexModelObject(
            ushort nameIndex,
            ushort parentClassNameIndex,
            ushort autoStateNameIndex,
            IReadOnlyList<PexModelVariable> variables,
            IReadOnlyList<PexModelProperty> properties,
            IReadOnlyList<PexModelState> states)
        {
            NameIndex = nameIndex;
            ParentClassNameIndex = parentClassNameIndex;
            AutoStateNameIndex = autoStateNameIndex;
            Variables = variables;
            Properties = properties;
            States = states;
        }

        internal ushort NameIndex { get; }
        internal ushort ParentClassNameIndex { get; }
        internal ushort AutoStateNameIndex { get; }
        internal IReadOnlyList<PexModelVariable> Variables { get; }
        internal IReadOnlyList<PexModelProperty> Properties { get; }
        internal IReadOnlyList<PexModelState> States { get; }
    }

    internal sealed class PexModelVariable
    {
        internal PexModelVariable(ushort variableIndex, ushort nameIndex, ushort typeNameIndex, object value)
        {
            VariableIndex = variableIndex;
            NameIndex = nameIndex;
            TypeNameIndex = typeNameIndex;
            Value = value;
        }

        internal ushort VariableIndex { get; }
        internal ushort NameIndex { get; }
        internal ushort TypeNameIndex { get; }
        internal object Value { get; }
    }

    internal sealed class PexModelProperty
    {
        internal PexModelProperty(ushort nameIndex, ushort typeIndex, ushort autoVariableNameIndex)
        {
            NameIndex = nameIndex;
            TypeIndex = typeIndex;
            AutoVariableNameIndex = autoVariableNameIndex;
        }

        internal ushort NameIndex { get; }
        internal ushort TypeIndex { get; }
        internal ushort AutoVariableNameIndex { get; }
    }

    internal sealed class PexModelState
    {
        internal PexModelState(ushort nameIndex, IReadOnlyList<PexModelFunction> functions)
        {
            NameIndex = nameIndex;
            Functions = functions;
        }

        internal ushort NameIndex { get; }
        internal IReadOnlyList<PexModelFunction> Functions { get; }
    }

    internal sealed class PexModelFunction
    {
        internal PexModelFunction(
            ushort nameIndex,
            ushort returnTypeIndex,
            byte flags,
            IReadOnlyList<PexModelFunctionParameter> parameters,
            IReadOnlyList<PexModelInstruction> instructions)
        {
            NameIndex = nameIndex;
            ReturnTypeIndex = returnTypeIndex;
            Flags = flags;
            Parameters = parameters;
            Instructions = instructions;
        }

        internal ushort NameIndex { get; }
        internal ushort ReturnTypeIndex { get; }
        internal byte Flags { get; }
        internal IReadOnlyList<PexModelFunctionParameter> Parameters { get; }
        internal IReadOnlyList<PexModelInstruction> Instructions { get; }
    }

    internal sealed class PexModelFunctionParameter
    {
        internal PexModelFunctionParameter(ushort nameIndex, ushort typeIndex)
        {
            NameIndex = nameIndex;
            TypeIndex = typeIndex;
        }

        internal ushort NameIndex { get; }
        internal ushort TypeIndex { get; }
    }

    internal sealed class PexModelInstruction
    {
        internal PexModelInstruction(string opcode, IReadOnlyList<PexModelInstructionArgument> arguments)
        {
            Opcode = opcode;
            Arguments = arguments;
        }

        internal string Opcode { get; }
        internal IReadOnlyList<PexModelInstructionArgument> Arguments { get; }
    }

    internal sealed class PexModelInstructionArgument
    {
        internal PexModelInstructionArgument(byte type, object value) { Type = type; Value = value; }
        internal byte Type { get; }
        internal object Value { get; }
    }
}
