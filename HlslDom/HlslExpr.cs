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
    /// <summary>
    /// The opcode class represents operators like add, subtract,
    /// multiply, divide, and other operations like shifts and 
    /// boolean operations.
    /// </summary>
    public enum OpCode
    {
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
        NOT,
    }

    /// <summary>
    /// Internal class used only to aid in outputting opcodes to strings.
    /// </summary>
    class Operator
    {
        public static string ToString(OpCode opCode)
        {
            switch (opCode)
            {
                case OpCode.ADD: return "+";
                case OpCode.AND: return "&";
                case OpCode.DIV: return "/";
                case OpCode.IDENTITY: return "";
                case OpCode.MOD: return "%";
                case OpCode.MUL: return "*";
                case OpCode.OR: return "|";
                case OpCode.SHL: return "<<";
                case OpCode.SHR: return ">>";
                case OpCode.SUB: return "-";
                case OpCode.XOR: return "^";
            }

            return null;
        }
    }

    /// <summary>
    /// The Expr base class is the root of all syntactic expressions within
    /// the HlslDom. Not all HlslDom Exprs necessarily have values as not all
    /// expressions within the HLSL language have values. For example, a
    /// for-loop expression has no value because it cannot be used on the left-
    /// or right-hand side of an expression, however a function call can be used
    /// as a value.
    /// </summary>
    public abstract class Expr
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
    /// BinaryExprs represent binary expressions, such as multiplication or
    /// addition. These expressions require that the types of both sides 
    /// match. When emitted to HLSL, these expressions will automatically
    /// parenthesize so as to follow program ordering.
    /// </summary>
    public class BinaryExpr : Expr
    {
        OpCode ExprOpCode;
        Value LHS;
        Value RHS;

        /// <summary>
        /// Construct a BinaryExpr with the specified values and operation.
        /// </summary>
        /// <param name="lhs">Left hand side.</param>
        /// <param name="rhs">Right hand side.</param>
        /// <param name="oper">Operator.</param>
        public BinaryExpr(Value lhs, Value rhs, OpCode oper)
        {
            if (lhs.ValueType != rhs.ValueType)
                throw new ShaderDomException("BinaryExpr types must match!");

            if (oper == OpCode.IDENTITY)
                throw new ShaderDomException("Identity operator is not valid!");

            LHS = lhs;
            RHS = rhs;
            ExprOpCode = oper;
        }

        public override bool HasValue()
        {
            return true;
        }

        public override Value Value
        {
            get { return new Value(LHS.ValueType, ToString()); }
        }

        public override string ToString()
        {
            return string.Format("({0} {1} {2})", LHS.Name, Operator.ToString(ExprOpCode), RHS.Name);
        }
    }

    /// <summary>
    /// DeclExpr is used to create a variable declaration expression.
    /// </summary>
    public class DeclExpr : Expr
    {
        Type Type;
        string Name;
        Value InitValue;
        bool IsConst;
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

        /// <summary>
        /// Create a variable declaration initialized to the value of the given expression.
        /// </summary>
        /// <param name="expression">Expression; must resolve to a value.</param>
        public DeclExpr(Expr expression)
        {
            if (!expression.HasValue())
                throw new ShaderDomException("Provided expression must resolve to a value!");

            Type = expression.Value.ValueType;
            Name = string.Format("var{0}", Counter++);
            InitValue = expression.Value;
        }

        /// <summary>
        /// Create a named variable declaration initialized to the value of the given expression.
        /// </summary>
        /// <param name="expression">Expression; must resolve to a value.</param>
        /// <param name="name">Variable name.</param>
        public DeclExpr(Expr expression, string name)
        {
            if (!expression.HasValue())
                throw new ShaderDomException("Provided expression must resolve to a value!");

            Type = expression.Value.ValueType;
            Name = name;
            InitValue = expression.Value;
        }

        /// <summary>
        /// Set this variable declaration to be const.
        /// </summary>
        /// <param name="isConst">True if const, false otherwise.</param>
        public void SetConst(bool isConst)
        {
            IsConst = isConst;
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
            string constString = IsConst ? "const " : "";

            if (InitValue == null)
                SB.AppendFormat("{0}{1} {1};", constString, Type.TypeName(), Name);
            else
                SB.AppendFormat("{0}{1} {2} = {3};", constString, Type.TypeName(), Name, InitValue);

            return SB.ToString();
        }
    }

    /// <summary>
    /// The CommaExpr represents a comma expression, ie: "EXPR1, EXPR2", 
    /// and evaluates to the expression on the right hand side.
    /// </summary>
    public class CommaExpr : Expr
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
    public abstract class CompoundExpr : Expr
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
    public class IfExpr : CompoundExpr
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
    public class ElseExpr : CompoundExpr
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
    public class WhileExpr : CompoundExpr
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
    public class ForExpr : CompoundExpr
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
            if (attributes == LoopAttributes.UNROLL)
                throw new ShaderDomException("Unroll attribute specified without an unroll depth!");
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

    /// <summary>
    /// CallExprs represent a call to a function, either intrinsic or user defined.
    /// </summary>
    public class CallExpr : Expr
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

        /// <summary>
        /// Construct a CallExpr to the specified function with the specified parameters.
        /// </summary>
        /// <param name="fn">Function to call.</param>
        /// <param name="parameters">Parameters for function.</param>
        public CallExpr(Function fn, Expr[] parameters)
        {
            Fn = fn;
            Parameters = new Value[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
                Parameters[i] = parameters[i].Value;

            if (!fn.IsValidCall(Parameters))
                throw new ShaderDomException(string.Format("Call to {0} is not valid!", fn.Name));
        }

        /// <summary>
        /// Construct a CallExpr to the specified function with the specified parameters.
        /// </summary>
        /// <param name="fn">Function to call.</param>
        /// <param name="parameterValues">Parameter values for function.</param>
        public CallExpr(Function fn, Value[] parameterValues)
        {
            Fn = fn;
            Parameters = parameterValues;

            if (!fn.IsValidCall(Parameters))
                throw new ShaderDomException(string.Format("Call to {0} is not valid!", fn.Name));
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            // Clip has no return value.
            if (!(Fn is Hlsl.Intrinsics.Clip))
                SB.AppendFormat("const {0} {1} = {2}(", FnValue.ValueType.TypeName(), FnValue, Fn.Name);
            else
                SB.AppendFormat("{0}(", Fn.Name);        

            for (int i = 0; i < Parameters.Length; ++i)
                SB.AppendFormat("{0}{1}", Parameters[i], i < Parameters.Length - 1 ? ", " : "");

            SB.Append(");");
            return SB.ToString();
        }
    }

    /// <summary>
    /// AssignmentExprs represent the assignment of one value to another and 
    /// permit the addition of modifiers such as +/-/*/etc. Please note that 
    /// currently the AssignmentExprs do no checking if the provided L-value 
    /// is valid, it will currently allow a literal value to be used on the 
    /// LHS which will then fail compilation.
    /// </summary>
    public class AssignmentExpr : Expr
    {
        OpCode Modifier;
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

        /// <summary>
        /// Construct an AssignmentExpr.
        /// </summary>
        /// <param name="lhsValue">Assignment left hand side, must be an L-value.</param>
        /// <param name="rhsValue">Assignment right hand side.</param>
        public AssignmentExpr(Value lhsValue, Value rhsValue)
            : this(lhsValue, rhsValue, OpCode.IDENTITY)
        {
        }

        /// <summary>
        /// Construct a self-modifying AssignmentExpr.
        /// </summary>
        /// <param name="lhsValue">Assignment left hand side, must be an L-value.</param>
        /// <param name="rhsValue">Assignment right hand side.</param>
        /// <param name="modifier">Modification operation.</param>
        public AssignmentExpr(Value lhsValue, Value rhsValue, OpCode modifier)
        {
            LhsValue = lhsValue;
            RhsValue = rhsValue;
            Modifier = modifier;
        }

        public override string ToString()
        {
            string modifierString = Operator.ToString(Modifier);
            return string.Format("{0} {1}= {2};", LhsValue.Name, modifierString, RhsValue.Name);
        }
    }

    /// <summary>
    /// LiteralExprs represent a literal value, such as float4(0,0,0,0).
    /// </summary>
    public class LiteralExpr : Expr
    {
        Type LiteralType;
        object[] Initializers;

        /// <summary>
        /// Default instance of LiteralExpr creates a literal zero for the specified type.
        /// </summary>
        /// <param name="literalType">Type to construct.</param>
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

        /// <summary>
        /// Construct a LiteralExpr with the specified values.
        /// </summary>
        /// <param name="literalType">Type to construct.</param>
        /// <param name="initializers">List of initializer values.</param>
        public LiteralExpr(Type literalType, params object[] initializers)
        {
            LiteralType = literalType;
            Initializers = initializers;

            if (initializers == null || initializers.Length == 0)
                throw new ShaderDomException("Literal initializer must be specified.");

            if (initializers.Length != LiteralType.TotalElements)
                throw new ShaderDomException("Literal initializers must be provided for all elements!");
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

    /// <summary>
    /// Represents a function return value. Every user defined function must contain at
    /// least one ReturnExpr and, in the case of a function having multiple, the 
    /// underlying types must all agree.
    /// </summary>
    public class ReturnExpr : Expr
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

        /// <summary>
        /// Construct a ReturnExpr.
        /// </summary>
        /// <param name="value">Value to return.</param>
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

    /// <summary>
    /// StructMemberExprs evaluate to a particular struct member and may be used
    /// as both L-values and R-values. 
    /// </summary>
    public class StructMemberExpr : Expr
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

        /// <summary>
        /// Construct a StructMemberExpr with a struct field index.
        /// </summary>
        /// <param name="value">StructType instance.</param>
        /// <param name="fieldIndex">StructType's StructField index.</param>
        public StructMemberExpr(Value value, int fieldIndex)
        {
            StructType structType = value.ValueType as StructType;
            if (structType == null)
                throw new ShaderDomException("StructMemberExpr only valid on structs!");

            StructField field = structType.Fields[fieldIndex];
            FieldValue = new Value(field.FieldType, value.Name + "." + field.FieldName);
        }

        /// <summary>
        /// Construct a StructMemberExpr with a struct field instance.
        /// </summary>
        /// <param name="value">StructType instance.</param>
        /// <param name="field">StructType's StructField instance.</param>
        public StructMemberExpr(Value value, StructField field)
        {
            if (!(value.ValueType is StructType))
                throw new ShaderDomException("StructMemberExpr only valid on structs!");

            FieldValue = new Value(field.FieldType, value.Name + "." + field.FieldName);
        }

        /// <summary>
        /// Construct a StructMemberExpr with a struct field name.
        /// </summary>
        /// <param name="value">StructType instance.</param>
        /// <param name="fieldName">StructType's field name.</param>
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
