////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////
// This file includes definitions of all the HLSL (as of SM3) intrinsics
// and provides, for each intrinsic, validation methods for input values
// as well as return type determination functionality.
////////////////////////////////////////////////////////////////////////

using System;

namespace Hlsl.Intrinsics
{
    #region Data Transformation Functions

    class DataTransformFunction : Function
    {
        public DataTransformFunction(string name)
            : base(name)
        { }

        public override bool IsValidCall(Value[] args)
        {
            if (args.Length != 1)
                return false;

            if (args[0].ValueType is SamplerType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }

        public static Function[] CreateDataTransformFunctions()
        {
            string[] fnNames = new string[] {
                "abs",
                "acos",
                "all",
                "any",
                "asin",
                "atan",
                "ceil",
                "clamp",
                "cos",
                "cosh",
                "ddx",
                "ddy",
                "degrees",
                "exp",
                "exp2",
                "frac",
                "floor",
                "fmod",
                "fwidth",
                "log",
                "log10",
                "log2",
                "radians",
                "rsqrt",
                "saturate",
                "sign",
                "sin",
                "sinh",
                "sqrt",
                "tan",
                "tanh",
                "trunc"
            };
            
            Function[] fns = new Function[fnNames.Length];

            for (int i = 0; i < fnNames.Length; ++i)
                fns[i] = new DataTransformFunction(fnNames[i]);

            return fns;
        }
    }

    #endregion

    #region Clip
    class Clip : Function
    {
        public Clip() : base("clip") 
        {
        }

        public override bool IsValidCall(Value[] args)
        {
            if (args.Length != 1)
                return false;

            if (args[0].ValueType is SamplerType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            throw new ShaderDomException("'clip' has no return value.");
        }
    }
    #endregion

    #region Miscellaneous

    class Atan2 : Function
    {
        public Atan2()
            : base("atan2")
        { }

        public override bool IsValidCall(Value[] args)
        {
            if (args.Length != 2)
                return false;

            if (args[0].ValueType is SamplerType || args[0].ValueType is StructType)
                return false;

            if (args[0].ValueType != args[1].ValueType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Determinant : Function
    {
        public Determinant()
            : base("determinant")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // determinant takes a matrix of any dimension and returns
            // a scalar float.

            if (args.Length != 1)
                return false;

            if (!(args[0].ValueType is MatrixType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Distance : Function
    {
        public Distance()
            : base("distance")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // distance takes two vectors, both float.

            if (args.Length != 2)
                return false;

            if (!(args[0].ValueType is VectorType)
                || !(args[0].ValueType is VectorType))
                return false;

            VectorType VT0 = args[0].ValueType as VectorType;
            VectorType VT1 = args[1].ValueType as VectorType;

            if (!(VT0.BaseType is FloatType) || !(VT1.BaseType is FloatType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetFloatType();
        }
    }

    class Dot : Function
    {
        public Dot()
            : base("dot")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // dot takes two parameters, both vectors of either float or int.

            if (args.Length != 2)
                return false;

            if (!(args[0].ValueType is VectorType)
                || !(args[0].ValueType is VectorType))
                return false;

            VectorType VT0 = args[0].ValueType as VectorType;
            VectorType VT1 = args[1].ValueType as VectorType;

            if (!(VT0.BaseType is FloatType) || !(VT0.BaseType is IntType))
                return false;

            if (!(VT1.BaseType is FloatType) || !(VT1.BaseType is IntType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            VectorType VT0 = args[0].ValueType as VectorType;
            return VT0.BaseType;
        }
    }

    class Faceforward : Function
    {
        public Faceforward()
            : base("faceforward")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // faceforward requires all three arguments to be float vectors.

            if (args.Length != 3)
                return false;

            foreach (Value value in args)
            {
                if (!(value.ValueType is VectorType))
                    return false;

                VectorType VT = value.ValueType as VectorType;

                if (!(VT.BaseType is FloatType))
                    return false;
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Frexp : Function
    {
        public Frexp()
            : base("frexp")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // frexp permits as arguments all scalar, vector, and matrix
            // types with float components.

            if (args.Length != 2)
                return false;

            for (int i = 0; i < 2; ++i)
            {
                try
                {
                    Type scalarBase = args[i].ValueType.GetScalarBaseType();
                    if (!(scalarBase is FloatType))
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Ldexp : Function
    {
        public Ldexp()
            : base("ldexp")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // ldexp permits as arguments all scalar, vector, and matrix
            // types with float components.

            if (args.Length != 2)
                return false;

            for (int i = 0; i < 2; ++i)
            {
                try
                {
                    Type scalarBase = args[i].ValueType.GetScalarBaseType();
                    if (!(scalarBase is FloatType))
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Length : Function
    {
        public Length()
            : base("length")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Length takes a single float vector as input and returns a float.

            if (args.Length != 1)
                return false;

            VectorType VT = args[0].ValueType as VectorType;
            if (VT == null)
                return false;

            if (!(VT.BaseType is FloatType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetFloatType();
        }
    }

    class Lerp : Function
    {
        public Lerp()
            : base("lerp")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // lerp permits as arguments all scalar, vector, and matrix
            // types with float components.

            if (args.Length != 3)
                return false;

            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    Type scalarBase = args[i].ValueType.GetScalarBaseType();
                    if (!(scalarBase is FloatType))
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Lit : Function
    {
        public Lit()
            : base("lit")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Lit calculates a 4-element lighting vector based on
            // three float inputs.

            if (args.Length != 3)
                return false;

            for (int i = 0; i < args.Length; ++i)
                if (!(args[i].ValueType is FloatType))
                    return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetVectorType(TypeRegistry.GetFloatType(), 4);
        }
    }

    abstract class MinMaxFunction : Function
    {
        public MinMaxFunction(string name)
            : base(name)
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Min/Max both take two values of the same type,
            // though that type may be any scalar/vector/matrix 
            // conglomerate of float/int base types.
            if (args.Length != 2)
                return false;

            try
            {
                Type scalarBase = args[0].ValueType.GetScalarBaseType();
                if (!(scalarBase is FloatType)
                    || !(scalarBase is IntType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            if (args[1].ValueType != args[0].ValueType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Max : MinMaxFunction
    {
        public Max() : base("max") { }
    }

    class Min : MinMaxFunction
    {
        public Min() : base("min") { }
    }

    class Noise : Function
    {
        public Noise()
            : base("noise")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Noise calculates a Perlin noise value based on
            // an input vector.

            if (args.Length != 1)
                return false;

            if (!(args[0].ValueType is VectorType))
                return false;

            if (!(args[0].ValueType.GetScalarBaseType() is FloatType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetFloatType();
        }
    }

    class Normalize : Function
    {
        public Normalize()
            : base("normalize")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Normalize only operates on a single float vector.

            if (args.Length != 1)
                return false;

            if (!(args[0].ValueType is VectorType))
                return false;

            if (!(args[0].ValueType.GetScalarBaseType() is FloatType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Pow : Function
    {
        public Pow()
            : base("pow")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Pow takes two parameters, x and y, which
            // have to be the same type but can be any
            // scalar/vector/matrix of floats.
            if (args.Length != 2)
                return false;

            try
            {
                Type baseType = args[0].ValueType.GetScalarBaseType();
                if (!(baseType is FloatType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            if (args[1].ValueType != args[0].ValueType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Reflect : Function
    {
        public Reflect()
            : base("reflect")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Reflect takes two float vectors and returns
            // a vector of the same dimension as the first
            // parameter.

            if (args.Length != 2)
                return false;

            try
            {
                Type scalarBase = args[0].ValueType.GetScalarBaseType();
                if (!(scalarBase is FloatType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            if (!(args[0].ValueType is VectorType))
                return false;

            if (args[0].ValueType != args[1].ValueType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Refract : Function
    {
        public Refract()
            : base("refract")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Refract takes two float vectors and a scalar
            // float and returns a vector of the same dimension
            // as the first parameter.

            if (args.Length != 2)
                return false;

            try
            {
                Type scalarBase = args[0].ValueType.GetScalarBaseType();
                if (!(scalarBase is FloatType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            if (!(args[0].ValueType is VectorType))
                return false;

            if (args[0].ValueType != args[1].ValueType)
                return false;

            if (!(args[2].ValueType is FloatType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    class Sincos : Function
    {
        public Sincos()
            : base("sincos")
        { }

        public override bool IsValidCall(Value[] args)
        {
            throw new NotImplementedException();
        }

        public override Type GetReturnType(Value[] args)
        {
            throw new NotImplementedException();
        }
    }

    class Smoothstep : Function
    {
        public Smoothstep()
            : base("smoothstep")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Smoothstep takes three parameters, the third of which
            // is used to determine the types of the first two and the
            // return type.

            if (args.Length != 3)
                return false;

            try
            {
                Type baseType = args[2].ValueType.GetScalarBaseType();
                if (!(baseType is FloatType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            if (args[0].ValueType != args[2].ValueType)
                return false;

            if (args[1].ValueType != args[2].ValueType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[2].ValueType;
        }
    }

    class Transpose : Function
    {
        public Transpose()
            : base("transpose")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Transpose takes an m-by-n matrix and returns
            // an n-by-m matrix.

            if (args.Length != 1)
                return false;

            if (!(args[0].ValueType is MatrixType))
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            MatrixType MT = args[0].ValueType as MatrixType;
            VectorType VT = MT.BaseType as VectorType;
            Type baseType = VT.BaseType;

            return TypeRegistry.GetMatrixType(
                TypeRegistry.GetVectorType(baseType, MT.Dimension), VT.Dimension);
        }
    }

    class Trunc : Function
    {
        public Trunc()
            : base("trunc")
        { }

        public override bool IsValidCall(Value[] args)
        {
            // Trunc takes any scalar/vector/matrix type
            // if it is float-based and returns a value of
            // the same type.

            if (args.Length != 1)
                return false;

            try
            {
                Type baseType = args[0].ValueType.GetScalarBaseType();
                if (!(baseType is FloatType))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return args[0].ValueType;
        }
    }

    #endregion

    #region Mul
    class Mul : Function
    {
        public Mul()
            : base("mul")
        { }

        Type InternalTryGetReturnType(Value[] args)
        {
            if (args.Length != 2)
                return null;

            Type x = args[0].ValueType;
            Type y = args[1].ValueType;

            try
            {
                foreach (Value arg in args)
                    if (!(arg.ValueType.GetScalarBaseType() is FloatType) && !(arg.ValueType.GetScalarBaseType() is IntType))
                        return null;
            }
            catch (Exception)
            {
                return null;
            }

            if (x is ScalarType)
            {
                if (y is ScalarType)
                    return (x == y) ? x : null;

                if (y is VectorType || y is MatrixType)
                    return y;
            }

            if (x is VectorType)
            {
                VectorType xVT = (VectorType)x;

                if (y is ScalarType)
                    return x;

                if (y is VectorType)
                {
                    VectorType yVT = (VectorType)y;
                    return yVT.Dimension == xVT.Dimension ? TypeRegistry.GetFloatType() : null;
                }

                if (y is MatrixType)
                {
                    MatrixType yMT = (MatrixType)y;
                    return yMT.Dimension == xVT.Dimension ? yMT.BaseType : null;
                }
            }

            if (x is MatrixType)
            {
                MatrixType xMT = (MatrixType)x;

                if (y is ScalarType)
                    return x;

                if (y is VectorType)
                {
                    VectorType yVT = (VectorType)y;
                    VectorType xColVT = (VectorType)xMT.BaseType;
                    return xColVT.Dimension == yVT.Dimension ? xMT.BaseType : null;
                }

                if (y is MatrixType)
                {
                    MatrixType yMT = (MatrixType)y;
                    VectorType xColVT = (VectorType)xMT.BaseType;

                    return xColVT.Dimension == yMT.Dimension
                        ? TypeRegistry.GetMatrixType(yMT.BaseType, xMT.Dimension) : null;
                }
            }

            throw new ShaderDomException("Unable to determine return type.");
        }

        public override bool IsValidCall(Value[] args)
        {
            return InternalTryGetReturnType(args) != null;
        }

        public override Type GetReturnType(Value[] args)
        {
            return InternalTryGetReturnType(args);
        }
    }
    #endregion

    #region Predicates
    class PredicateFunction : Function
    {
        public PredicateFunction(string name)
            : base(name)
        { }

        public override bool IsValidCall(Value[] args)
        {
            if (args.Length != 1)
                return false;

            if (args[0].ValueType is SamplerType)
                return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetBoolType();
        }
    }

    class Isfinite : PredicateFunction
    {
        public Isfinite() : base("isfinite") { }
    }

    class Isinf : PredicateFunction
    {
        public Isinf() : base("isinf") { }
    }

    class Isnan : PredicateFunction
    {
        public Isnan() : base("isnan") { }
    }

    #endregion

    #region Sampler functions

    #region TexBase
    abstract class TexBase : Function
    {
        int TextureDimension;
        bool HasDdxDdy;
        bool HasMandatoryDdxDdy;

        public TexBase(string name, int dimension, bool hasDdxDdy, bool hasMandatoryDdxDdy)
            : base(name)
        {
            TextureDimension = dimension;
            HasMandatoryDdxDdy = hasMandatoryDdxDdy;
            HasDdxDdy = hasMandatoryDdxDdy || hasDdxDdy;
        }

        bool VerifyDimensions(Value arg)
        {
            VectorType VT = arg.ValueType as VectorType;

            // Permit scalar floats as well as 1-dimensional float vectors
            // if this is a 1d texture sample function.
            if (TextureDimension == 1)
            {
                if (arg.ValueType is FloatType)
                    return true;

                if (VT != null)
                    return VT.Dimension == 1;
            }

            if (VT != null)
                return VT.Dimension == TextureDimension;

            return false;
        }

        bool VerifyArgs(Value[] args)
        {
            if (HasMandatoryDdxDdy)
                return args.Length == 4;

            if (HasDdxDdy)
                return args.Length == 4 || args.Length == 2;

            return args.Length == 2;
        }

        public override bool IsValidCall(Value[] args)
        {
            if (!VerifyArgs(args))
                return false;

            if (!(args[0].ValueType is SamplerType))
                return false;

            if (!VerifyDimensions(args[1]))
                return false;

            if (HasDdxDdy)
                if (HasMandatoryDdxDdy || args.Length == 4)
                    if (!VerifyDimensions(args[2]) || !VerifyDimensions(args[3]))
                        return false;

            return true;
        }

        public override Type GetReturnType(Value[] args)
        {
            return TypeRegistry.GetVectorType(TypeRegistry.GetFloatType(), 4);
        }

    }
    #endregion

    #region 1D samplers
    class Tex1D : TexBase { public Tex1D() : base("tex1D", 1, true, false) { } }
    class Tex1DBias : TexBase { public Tex1DBias() : base("tex1Dbias", 1, false, false) { } }
    class Tex1DGrad : TexBase { public Tex1DGrad() : base("tex1Dgrad", 1, true, true) { } }
    class Tex1DLod : TexBase { public Tex1DLod() : base("tex1Dlod", 1, false, false) { } }
    class Tex1DProj : TexBase { public Tex1DProj() : base("tex1Dproj", 1, false, false) { } }
    #endregion

    #region 2D samplers
    class Tex2D : TexBase { public Tex2D() : base("tex2D", 2, true, false) { } }
    class Tex2DBias : TexBase { public Tex2DBias() : base("tex2Dbias", 2, false, false) { } }
    class Tex2DGrad : TexBase { public Tex2DGrad() : base("tex2Dgrad", 2, true, true) { } }
    class Tex2DLod : TexBase { public Tex2DLod() : base("tex2Dlod", 2, false, false) { } }
    class Tex2DProj : TexBase { public Tex2DProj() : base("tex2Dproj", 2, false, false) { } }
    #endregion

    #region 3D samplers
    class Tex3D : TexBase { public Tex3D() : base("tex3D", 3, true, false) { } }
    class Tex3DBias : TexBase { public Tex3DBias() : base("tex3Dbias", 3, false, false) { } }
    class Tex3DGrad : TexBase { public Tex3DGrad() : base("tex3Dgrad", 3, true, true) { } }
    class Tex3DLod : TexBase { public Tex3DLod() : base("tex3Dlod", 3, false, false) { } }
    class Tex3DProj : TexBase { public Tex3DProj() : base("tex3Dproj", 3, false, false) { } }
    #endregion

    #region Cube samplers
    class TexCUBE : TexBase { public TexCUBE() : base("texCUBE", 3, true, false) { } }
    class TexCUBEBias : TexBase { public TexCUBEBias() : base("texCUBEbias", 3, false, false) { } }
    class TexCUBEGrad : TexBase { public TexCUBEGrad() : base("texCUBEgrad", 3, true, true) { } }
    class TexCUBELod : TexBase { public TexCUBELod() : base("texCUBElod", 3, false, false) { } }
    class TexCUBEProj : TexBase { public TexCUBEProj() : base("texCUBEproj", 3, false, false) { } }
    #endregion

    #endregion
}
