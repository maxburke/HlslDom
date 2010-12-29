////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////
// This file includes the definitions of the basic HLSL syntactic
// expressions, such as conditionals, loops, and other basic statements.
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

    /// <summary>
    /// The Expr base class is the root of all syntactic expressions within
    /// the HlslDom. Not all HlslDom Exprs necessarily have values as not all
    /// expressions within the HLSL language have values. For example, a
    /// for-loop expression has no value because it cannot be used on the left-
    /// or right-hand side of an expression, however a function call can be used
    /// as a value.
    /// </summary>
    abstract class Expr
    {
        /// <summary>
        /// Predicate used to determine if a particular expression has a value.
        /// </summary>
        /// <returns>True if the expression has a value, false otherwise.</returns>
        public abstract bool HasValue();

        /// <summary>
        /// 
        /// </summary>
        public abstract Value Value { get; }
    }

    /// <summary>
    /// DeclExpr is used to create a variable declaration expression.
    /// </summary>
    class DeclExpr : Expr
    {
        Type Type;
        string Name;
        Value InitValue;
        static int Counter;

        /// <summary>
        /// Declare a variable of the given type, giving it an automatically generated name.
        /// </summary>
        /// <param name="type">Variable type.</param>
        public DeclExpr(Type type)
        {
            Type = type;
            Name = string.Format("var{0}", Counter++);
            InitValue = null;
        }

        /// <summary>
        /// Declare a variable of the given type with the given name.
        /// </summary>
        /// <param name="type">Variable type.</param>
        /// <param name="name">Variable name.</param>
        public DeclExpr(Type type, string name)
        {
            Type = type;
            Name = name;
            InitValue = null;
        }

        /// <summary>
        /// Declare a variable of the given type with the provided initial value. Automatically generates a name.
        /// </summary>
        /// <param name="type">Variable type.</param>
        /// <param name="value">Initial value.</param>
        public DeclExpr(Type type, Value value)
        {
            Type = type;
            Name = string.Format("var{0}", Counter++);
            InitValue = value;
        }

        /// <summary>
        /// Declare a variable of the given type with the given name and with the provided initial value.
        /// </summary>
        /// <param name="type">Variable type.</param>
        /// <param name="name">Variable name.</param>
        /// <param name="value">Initial value.</param>
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

    /// <summary>
    /// The CommaExpr represents a comma expression, ie: "EXPR1, EXPR2", 
    /// and evaluates to the expression on the right hand side.
    /// </summary>
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

    /// <summary>
    /// Compound expressions are used for general bodies of code that 
    /// need not evaluate to a particular value, such as the body of a
    /// for-loop or if-clause.
    /// </summary>
    abstract class CompoundExpr : Expr
    {
        protected List<Expr> Body = new List<Expr>();

        /// <summary>
        /// Add an expression to the body of this compound expression.
        /// </summary>
        /// <param name="expr">The expression to add</param>
        public void Add(Expr expr)
        {
            Body.Add(expr);
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

    /// <summary>
    /// Represents an if clause, including both the test expression and the 
    /// body of the clause.
    /// </summary>
    class IfExpr : CompoundExpr
    {
        Expr Test;

        /// <summary>
        /// Constructs an IfExpr with the provided test expression.
        /// </summary>
        /// <param name="test">The condition to test.</param>
        public IfExpr(Expr test)
        {
            if (!test.HasValue())
                throw new ShaderDomException("Test expression doesn't return a value!");

            if (!(test.Value.ValueType is BoolType))
                throw new ShaderDomException("Test expression doesn't evaluate to a boolean type!");

            Test = test;
        }

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("IfExprs have no value!"); }
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();
            SB.AppendFormat("if ({0}) {{{1}", Test.ToString(), System.Environment.NewLine);

            foreach (Expr E in Body)
                SB.AppendFormat("        {0};{1}", E.ToString(), System.Environment.NewLine);

            SB.AppendLine("    }");

            return SB.ToString();
        }
    }

    /// <summary>
    /// ElseExpr is the associated else-clause for an if, if necessary.
    /// </summary>
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

        /// <summary>
        /// Because an else cannot just exist on its own, ElseExprs must be 
        /// associated with a particular IfExpr.
        /// </summary>
        /// <param name="associatedIfExpr">The IfExpr this else is associated with.</param>
        public ElseExpr(IfExpr associatedIfExpr)
        {
            AssociatedIfExpr = associatedIfExpr;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();
            SB.AppendFormat("else {{{0}", System.Environment.NewLine);

            foreach (Expr E in Body)
                SB.AppendFormat("        {0};{1}", E.ToString(), System.Environment.NewLine);

            SB.AppendLine("    }");

            return SB.ToString();
        }
    }

    /// <summary>
    /// WhileExprs represent while loops, loops that continue until the specified
    /// test condition evaluates true.
    /// </summary>
    class WhileExpr : CompoundExpr
    {
        Expr Test;

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("WhileExprs have no value!"); }
        }

        /// <summary>
        /// Constructs a WhileExpr with the specified test expression.
        /// </summary>
        /// <param name="test">Boolean test expression.</param>
        public WhileExpr(Expr test)
        {
            if (!test.HasValue())
                throw new ShaderDomException("Test expression doesn't return a value!");

            if (!(test.Value.ValueType is BoolType))
                throw new ShaderDomException("Test expression doesn't evaluate to a boolean type!");

            Test = test;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();
            SB.AppendFormat("while ({0}) {{{1}", Test.ToString(), System.Environment.NewLine);

            foreach (Expr E in Body)
                SB.AppendFormat("        {0};{1}", E.ToString(), System.Environment.NewLine);

            SB.AppendLine("    }");

            return SB.ToString();
        }
    }

    /// <summary>
    /// ForExprs represent for loops and require three separate expressions
    /// to be constructed: an initialization expression which is executed before
    /// the loop begins, a test expression that is evaluated every iteration
    /// to determine if the loop should continue, and an update expression
    /// that updates loop counters.
    /// </summary>
    class ForExpr : CompoundExpr
    {
        DeclExpr Initializer;
        Expr Test;
        Expr Update;
        LoopAttributes Attributes;
        int UnrollDepth;

        /// <summary>
        /// For loops may have attributes applied to them that determine what
        /// behavior the compiler and/or device may perform with the code in
        /// the body of the loop.
        /// </summary>
        public enum LoopAttributes
        {
            /// <summary>
            /// Default value, no attributes applied.
            /// </summary>
            NO_ATTRIBUTE,

            /// <summary>
            /// Unroll the loop to the specified depth. This must be used with
            /// the constructor that takes an unroll depth parameter.
            /// </summary>
            UNROLL,

            /// <summary>
            /// Give preference to using flow control statements instead of
            /// unrolling the loop.
            /// </summary>
            LOOP,

            /// <summary>
            /// Reduce time spent optimizing the loop within shader compilation,
            /// which generally prevents unrolling from occurring.
            /// </summary>
            FAST_OPT,
        }

        /// <summary>
        /// Construct a ForExpr.
        /// </summary>
        /// <param name="initializer">Initializer expression.</param>
        /// <param name="test">Test expression, must evaluate to a scalar boolean.</param>
        /// <param name="update">Update expression.</param>
        public ForExpr(DeclExpr initializer, Expr test, Expr update)
            : this(initializer, test, update, (int)LoopAttributes.NO_ATTRIBUTE, 0)
        {
        }

        /// <summary>
        /// Construct a ForExpr with an attribute.
        /// </summary>
        /// <param name="initializer">Initializer expression.</param>
        /// <param name="test">Test expression, must evaluate to a scalar boolean.</param>
        /// <param name="update">Update expression.</param>
        /// <param name="attributes">Loop attribute.</param>
        public ForExpr(DeclExpr initializer, Expr test, Expr update, LoopAttributes attributes)
            : this(initializer, test, update, attributes, 0)
        {
            if (attributes == (int)LoopAttributes.UNROLL)
                throw new ShaderDomException("Unroll attribute specified without an unroll depth!")
        }

        /// <summary>
        /// Construct a ForExpr with an attribute and an unroll depth.
        /// </summary>
        /// <param name="initializer">Initializer expression.</param>
        /// <param name="test">Test expression, must evaluate to a scalar boolean.</param>
        /// <param name="update">Update expression.</param>
        /// <param name="attributes">Loop attribute.</param>
        /// <param name="unrollDepth">Depth with which to unroll the loop, used with LoopAttributes.UNROLL</param>
        public ForExpr(DeclExpr initializer, Expr test, Expr update, LoopAttributes attributes, int unrollDepth)
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

        public override bool HasValue()
        {
            return false;
        }

        public override Value Value
        {
            get { throw new ShaderDomException("ForExprs have no value!"); }
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            string attribute = null;

            switch (Attributes)
            {
                case LoopAttributes.UNROLL:
                    SB.Append(string.Format("[unroll({0})] ", UnrollDepth));
                    break;
                case LoopAttributes.LOOP:
                    SB.Append("[loop] ");
                    break;
                case LoopAttributes.FAST_OPT:
                    SB.Append("[fastopt] ");
                    break;
                case LoopAttributes.NO_ATTRIBUTE:
                    break;
            }

            SB.AppendFormat("for ({0}; {1}; {2}) {{", Initializer, Test, Update);
            foreach (Expr E in Body)
                SB.AppendFormat("        {0};{1}", E.ToString(), System.Environment.NewLine);

            SB.AppendLine("    }");
            return SB.ToString();
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
