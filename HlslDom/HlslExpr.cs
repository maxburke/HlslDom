////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;

namespace Hlsl.Expressions
{
    enum Operator
    {
        FIRST_MODIFIER_OPERATOR,
        IDENTITY,
        ADD,
        SUB,
        MUL,
        DIV,
        MOD,
        SHL,
        SHR,
        AND,
        OR,
        XOR,
        LAST_MODIFIER_OPERATOR,

        NOT,
    }

    abstract class Expr
    {
        public abstract bool HasValue();
        public abstract Value Value { get; }
    }

    class ValueExpr : Expr
    {
        Value ContainedValue;

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get { return ContainedValue; }
        }

        public ValueExpr(Value v)
        {
            ContainedValue = v;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class DeclExpr : Expr
    {
        Type Type;
        string Name;
        Value InitValue;
        static int Counter;

        public DeclExpr(Type type)
        {
            Type = type;
            Name = string.Format("var{0}", Counter++);
            InitValue = null;
        }

        public DeclExpr(Type type, string name)
        {
            Type = type;
            Name = name;
            InitValue = null;
        }

        public DeclExpr(Type type, Value value)
        {
            Type = type;
            Name = string.Format("var{0}", Counter++);
            InitValue = value;
        }

        public DeclExpr(Type type, string name, Value value)
        {
            Type = type;
            Name = name;
            InitValue = value;
        }

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get { return new Value(Type, Name); }
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();
            if (InitValue == null)
                SB.AppendFormat("{0} {1};", Type.TypeName(), Name);
            else
                SB.AppendFormat("{0} {1} = {2};", Type.TypeName(), Name, InitValue);

            return SB.ToString();
        }
    }

    class CommaExpr : Expr
    {
        public readonly Expr LHS;
        public readonly Expr RHS;

        public CommaExpr(Expr lhs, Expr rhs)
        {
            if (!lhs.HasValue() || !rhs.HasValue())
                throw new ShaderDomException("Both the left and right hand sides of the comma expression must have a value!");

            LHS = lhs;
            RHS = rhs;
        }

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get { return RHS.Value; }
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class CompoundExpr : Expr
    {
        List<Expr> Body = new List<Expr>();

        public void Add(Expr e)
        {
            Body.Add(e);
        }

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("CompoundExprs have no value!"); }
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class IfExpr : CompoundExpr
    {
        Expr Test;

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("IfExprs have no value!"); }
        }

        public IfExpr(Expr test)
        {
            if (!test.HasValue())
                throw new ShaderDomException("Test expression doesn't return a value!");

            Test = test;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class ElseExpr : CompoundExpr
    {
        IfExpr AssociatedIfExpr;

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("ElseExprs have no value!"); }
        }

        public ElseExpr(IfExpr associatedIfExpr)
        {
            AssociatedIfExpr = associatedIfExpr;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class WhileExpr : CompoundExpr
    {
        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("WhileExprs have no value!"); }
        }

        public WhileExpr(Expr test)
        {
            if (!test.HasValue())
                throw new ShaderDomException("Test expression doesn't return a value!");
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class ForExpr : CompoundExpr
    {
        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("ForExprs have no value!"); }
        }

        DeclExpr Initializer;
        Expr Test;
        Expr Update;
        int Attributes;
        int UnrollDepth;

        public enum LoopAttributes
        {
            NO_ATTRIBUTE,
            UNROLL,
            LOOP,
            FAST_OPT,
            ALLOW_UAV_CONDITION
        }

        public ForExpr(DeclExpr initializer, Expr test, Expr update)
            : this(initializer, test, update, (int)LoopAttributes.NO_ATTRIBUTE, 0)
        {
        }

        public ForExpr(DeclExpr initializer, Expr test, Expr update, int attributes)
            : this(initializer, test, update, attributes, 0)
        {
        }

        public ForExpr(DeclExpr initializer, Expr test, Expr update, int attributes, int unrollDepth)
        {
            if (!test.HasValue())
                throw new ShaderDomException("Test expression doesn't return a value!");

            if (!(test.Value.ValueType is BoolType))
                throw new ShaderDomException("Test expression does not return a boolean value!");

            Initializer = initializer;
            Test = test;
            Update = update;
            Attributes = attributes;
            UnrollDepth = unrollDepth;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    class CallExpr : Expr
    {
        Function Fn;
        Value[] Parameters;
        Value FnValue;
        static int Counter;

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get 
            {
                if (FnValue == null)
                    FnValue = new Value(Fn.GetReturnType(Parameters), string.Format("fnResult{0}", Counter++));

                return FnValue;
            }
        }

        public CallExpr(Function fn, Expr[] parameters)
        {
            Fn = fn;
            Parameters = new Value[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
                Parameters[i] = parameters[i].Value;

            if (!fn.IsValidCall(Parameters))
                throw new ShaderDomException(string.Format("Call to {0} is not valid!", fn.Name));
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            SB.AppendFormat("{0} {1} = {2}(", FnValue.ValueType.TypeName(), FnValue, Fn.Name);

            for (int i = 0; i < Parameters.Length; ++i)
                SB.AppendFormat("{0}{1}", Parameters[i], i < Parameters.Length - 1 ? ", " : "");

            SB.Append(");");
            return SB.ToString();
        }
    }

    class AssignmentExpr : Expr
    {
        Operator Modifier;
        Value LhsValue;
        Value RhsValue;

        public override Value Value
        {
            get { throw new ShaderDomException("AssignmentExprs have no value!"); }
        }

        public override bool HasValue()
        {
            throw new NotImplementedException();
        }

        public AssignmentExpr(Value lhsValue, Value rhsValue)
            : this(lhsValue, rhsValue, Operator.IDENTITY)
        {
        }

        public AssignmentExpr(Value lhsValue, Value rhsValue, Operator modifier)
        {
            if (modifier <= Operator.FIRST_MODIFIER_OPERATOR || modifier >= Operator.LAST_MODIFIER_OPERATOR)
                throw new ShaderDomException(string.Format("Operator {0} cannot be used to modify an assignment!", modifier));

            LhsValue = lhsValue;
            RhsValue = rhsValue;
            Modifier = modifier;
        }

        public override string ToString()
        {
            return string.Format("{0} = {1};", LhsValue.Name, RhsValue.Name);
        }
    }

    class LiteralExpr : Expr
    {
        Type LiteralType;
        object[] Initializers;

        /// <summary>
        /// Default instance of LiteralExpr creates a literal zero for the specified type.
        /// </summary>
        /// <param name="literalType"></param>
        public LiteralExpr(Type literalType)
        {
            if (literalType is SamplerType)
                throw new ShaderDomException("Unable to specify literal sampler value.");

            LiteralType = literalType;
            object initializer = null;
            Type baseScalarType = literalType.GetScalarBaseType();

            if (baseScalarType is BoolType)
                initializer = (object)false;
            else if (baseScalarType is IntType || baseScalarType is UIntType)
                initializer = (object)0;
            else if (baseScalarType is FloatType)
                initializer = (object)(0.0f);

            Initializers = new object[literalType.TotalElements];
            for (int i = 0; i < literalType.TotalElements; ++i)
                Initializers[i] = initializer;
        }

        public LiteralExpr(Type literalType, object[] initializers)
        {
            LiteralType = literalType;
            Initializers = initializers;

            if (initializers == null || initializers.Length == 0)
                throw new ShaderDomException("Literal initializer must be specified.");
        }

        public override Value Value
        {
            get 
            {
                return new Value(LiteralType, ToString());
            }
        }

        public override bool HasValue()
        {
            return false;
        }

        public override string ToString()
        {
            if (LiteralType is ScalarType)
            {
                return Initializers[0].ToString();
            }
            else
            {
                StringBuilder SB = new StringBuilder();
                SB.AppendFormat("{0}(", LiteralType.TypeName());
                for (int i = 0; i < Initializers.Length; ++i)
                    SB.AppendFormat("{0}{1}", Initializers[i], i < Initializers.Length - 1 ? ", " : "");
                SB.Append(")");
                return SB.ToString();
            }
        }
    }

    class ReturnExpr : Expr
    {
        Expr ReturnValue;

        public override Value Value
        {
            get { return ReturnValue.Value; }
        }

        public override bool HasValue()
        {
            return false;
        }

        public ReturnExpr(Expr value)
        {
            ReturnValue = value;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();
            SB.AppendFormat("return {0};", ReturnValue.Value);

            return SB.ToString();
        }
    }

    class StructMemberExpr : Expr
    {
        Value FieldValue;

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get { return FieldValue; }
        }

        public StructMemberExpr(Value value, int fieldIndex)
        {
            StructType structType = value.ValueType as StructType;
            if (structType == null)
                throw new ShaderDomException("StructMemberExpr only valid on structs!");

            StructField field = structType.Fields[fieldIndex];
            FieldValue = new Value(field.FieldType, value.Name + "." + field.FieldName);
        }

        public StructMemberExpr(Value value, StructField field)
        {
            if (!(value.ValueType is StructType))
                throw new ShaderDomException("StructMemberExpr only valid on structs!");

            FieldValue = new Value(field.FieldType, value.Name + "." + field.FieldName);
        }

        public StructMemberExpr(Value value, string fieldName)
        {
            StructType structType = value.ValueType as StructType;
            if (structType == null)
                throw new ShaderDomException("StructMemberExpr only valid on structs!");

            for (int i = 0; i < structType.Fields.Length; ++i)
            {
                StructField field = structType.Fields[i];
                if (field.FieldName == fieldName)
                {
                    FieldValue = new Value(field.FieldType, value.Name + "." + field.FieldName);
                    return;
                }
            }

            throw new ShaderDomException(string.Format("Field {0} does not exist in type {1}!", fieldName, structType.Name));
        }

        public override string ToString()
        {
            return FieldValue.Name;
        }
    }
}
