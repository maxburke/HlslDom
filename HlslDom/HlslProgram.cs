////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Hlsl.Expressions;

namespace Hlsl
{
    struct Pair<T, U>
    {
        public T first;
        public U second;

        public Pair(T _first, U _second)
        {
            first = _first;
            second = _second;
        }
    }

    class ShaderDomException : Exception
    {
        public new readonly string Message;

        public ShaderDomException(string message)
        {
            Message = message;
        }
    }

    public enum ShaderType
    {
        VertexShader,
        PixelShader,
        NUM_SHADER_TYPES
    }

    public enum ShaderProfile
    {
        vs_1_1,
        ps_2_0, ps_2_x, vs_2_0, vs_2_x,
        ps_3_0, vs_3_0
    }

    abstract class Function
    {
        public readonly string Name;

        public Function()
        {
            throw new NotImplementedException();
        }

        public Function(string name)
        {
            Name = name;
        }

        public abstract bool IsValidCall(Value[] args);
        public abstract Type GetReturnType(Value[] args);
    }

    class UserDefinedFunction : Function
    {
        List<Expr> Expressions = new List<Expr>();
        List<Pair<Value, Semantic>> Arguments = new List<Pair<Value, Semantic>>();
        Type FnReturnType;

        public UserDefinedFunction(string name)
            : base(name)
        {
        }

        public override bool IsValidCall(Value[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].ValueType != Arguments[i].first.ValueType)
                    return false;
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return DetermineReturnType();
        }

        public Value AddArgument(Type structType)
        {
            if (!(structType is StructType))
                throw new ShaderDomException("Argument must be a struct type or have a semantic!");

            Value v = new Value(structType, string.Format("arg{0}", Arguments.Count));
            Arguments.Add(new Pair<Value, Semantic>(v, new Semantic(Semantic.SemanticType.NONE)));

            return v;
        }

        public Value AddArgument(Type argType, Semantic semantic)
        {
            Value v = new Value(argType, string.Format("arg{0}", Arguments.Count));
            Arguments.Add(new Pair<Value, Semantic>(v, semantic));

            return v;
        }

        public void AddExpr(Expr e)
        {
            Expressions.Add(e);
        }

        public Value GetArgValue(int i)
        {
            return Arguments[i].first;
        }

        Type DetermineReturnType()
        {
            if (FnReturnType != null)
                return FnReturnType;

            Type returnType = null;

            foreach (Expr E in Expressions)
            {
                ReturnExpr RE = E as ReturnExpr;
                if (RE != null)
                {
                    if (returnType == null)
                        returnType = RE.Value.ValueType;
                    else if (returnType != RE.Value.ValueType)
                        throw new ShaderDomException("Function cannot return different types");
                }
            }

            if (returnType == null)
                throw new ShaderDomException("Function has no return type!");

            FnReturnType = returnType;
            return returnType;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            Type returnType = DetermineReturnType();
            
            /// TODO: Add support for return value sematics.
            SB.AppendFormat("{0} {1}(", returnType.TypeName(), Name);

            for (int i = 0; i < Arguments.Count; ++i)
            {
                string separator = i < Arguments.Count - 1 ? "," : "";

                Semantic none = new Semantic(Semantic.SemanticType.NONE);
                if (Arguments[i].second != none)
                    SB.AppendFormat("{0} {1} : {2}{3}", Arguments[i].first.ValueType.TypeName(), Arguments[i].first.Name, Arguments[i].second, separator);
                else
                    SB.AppendFormat("{0} {1}{2}", Arguments[i].first.ValueType.TypeName(), Arguments[i].first.Name, separator);
            }

            SB.AppendLine(") {");

            foreach (Expr E in Expressions)
                SB.AppendLine("    " + E.ToString());

            SB.AppendLine("}");

            return SB.ToString();
        }
    }

    class HlslProgram : IDisposable
    {
        List<DeclExpr> Globals = new List<DeclExpr>();
        List<Function> Functions = new List<Function>();
        List<UserDefinedFunction> UserFunctions = new List<UserDefinedFunction>();
        Pair<ShaderProfile, Function>[] Shaders = new Pair<ShaderProfile, Function>[(int)ShaderType.NUM_SHADER_TYPES];
        public TypeRegistry Types = new TypeRegistry();

        public HlslProgram()
        {
            Functions.AddRange(Intrinsics.DataTransformFunction.CreateDataTransformFunctions());
            Functions.Add(new Intrinsics.Clip());
            Functions.Add(new Intrinsics.Atan2());
            Functions.Add(new Intrinsics.Determinant());
            Functions.Add(new Intrinsics.Distance());
            Functions.Add(new Intrinsics.Dot());
            Functions.Add(new Intrinsics.Faceforward());
            Functions.Add(new Intrinsics.Frexp());
            Functions.Add(new Intrinsics.Ldexp());
            Functions.Add(new Intrinsics.Length());
            Functions.Add(new Intrinsics.Lerp());
            Functions.Add(new Intrinsics.Lit());
            Functions.Add(new Intrinsics.Max());
            Functions.Add(new Intrinsics.Min());
            Functions.Add(new Intrinsics.Noise());
            Functions.Add(new Intrinsics.Normalize());
            Functions.Add(new Intrinsics.Pow());
            Functions.Add(new Intrinsics.Reflect());
            Functions.Add(new Intrinsics.Refract());
            Functions.Add(new Intrinsics.Sincos());
            Functions.Add(new Intrinsics.Smoothstep());
            Functions.Add(new Intrinsics.Transpose());
            Functions.Add(new Intrinsics.Trunc());
            Functions.Add(new Intrinsics.Mul());
            Functions.Add(new Intrinsics.Isfinite());
            Functions.Add(new Intrinsics.Isinf());
            Functions.Add(new Intrinsics.Isnan());
            Functions.Add(new Intrinsics.Tex1D());
            Functions.Add(new Intrinsics.Tex1DBias());
            Functions.Add(new Intrinsics.Tex1DGrad());
            Functions.Add(new Intrinsics.Tex1DLod());
            Functions.Add(new Intrinsics.Tex1DProj());
            Functions.Add(new Intrinsics.Tex2D());
            Functions.Add(new Intrinsics.Tex2DBias());
            Functions.Add(new Intrinsics.Tex2DGrad());
            Functions.Add(new Intrinsics.Tex2DLod());
            Functions.Add(new Intrinsics.Tex2DProj());
            Functions.Add(new Intrinsics.Tex3D());
            Functions.Add(new Intrinsics.Tex3DBias());
            Functions.Add(new Intrinsics.Tex3DGrad());
            Functions.Add(new Intrinsics.Tex3DLod());
            Functions.Add(new Intrinsics.Tex3DProj());
            Functions.Add(new Intrinsics.TexCUBE());
            Functions.Add(new Intrinsics.TexCUBEBias());
            Functions.Add(new Intrinsics.TexCUBEGrad());
            Functions.Add(new Intrinsics.TexCUBELod());
            Functions.Add(new Intrinsics.TexCUBEProj());
        }

        public Function GetFunctionByName(string name)
        {
            foreach (Function fn in Functions)
                if (fn.Name == name)
                    return fn;

            throw new ShaderDomException("Function does not exist!");
        }

        public void AddFunction(UserDefinedFunction function)
        {
            if (Functions.Contains(function))
                throw new ShaderDomException(string.Format("Function {0} already exists!", function.Name));

            Functions.Add(function);
            UserFunctions.Add(function);
        }

        public void AddGlobal(DeclExpr globalVariableDecl)
        {
            Globals.Add(globalVariableDecl);
        }

        public void SetShader(ShaderType type, UserDefinedFunction function, ShaderProfile profile)
        {
            if (!UserFunctions.Contains(function))
                AddFunction(function);

            Shaders[(int)type] = new Pair<ShaderProfile, Function>(profile, function);
        }

        StringBuilder EmitRawShaderCodeToBuilder()
        {
            StringBuilder SB = new StringBuilder();

            ReadOnlyCollection<StructType> structTypes = Types.GetAllStructTypes();

            foreach (StructType ST in structTypes)
                SB.AppendLine(ST.ToString());

            foreach (DeclExpr GVE in Globals)
                SB.AppendLine(GVE.ToString());

            SB.AppendLine();

            foreach (Function fn in UserFunctions)
                SB.AppendLine(fn.ToString());

            return SB;
        }

        public string EmitRawShaderCode()
        {
            return EmitRawShaderCodeToBuilder().ToString();
        }

        public string EmitEffect()
        {
            StringBuilder SB = EmitRawShaderCodeToBuilder();

            SB.AppendLine("technique defaultTechnique {");
            SB.AppendLine("    pass P0 {");

            for (int i = 0; i < (int)ShaderType.NUM_SHADER_TYPES; ++i)
            {
                if (Shaders[i].second == null)
                    throw new ShaderDomException(string.Format("HlslProgram has null {0}", Enum.GetName(typeof(ShaderType), (object)i)));

                SB.AppendFormat("        {0} = compile {1} {2}();{3}",
                    Enum.GetName(typeof(ShaderType), (object)i),
                    Shaders[i].first,
                    Shaders[i].second.Name,
                    System.Environment.NewLine);
            }

            SB.AppendLine("    }");
            SB.AppendLine("}");

            return SB.ToString();
        }

        public void Dispose()
        {
            Globals = null;
            Functions = null;
            UserFunctions = null;
            Shaders = null;
            Types = null;
        }
    }
}
