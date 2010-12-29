////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////
// This module describes the HLSL type system, including all basic types
// like scalar bools/floats/ints, composite types like vectors and
// matrices, samplers, and user defined struct types.
////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;

namespace Hlsl
{
    /// <summary>
    /// StructField represents one field for a structure.
    /// </summary>
    class StructField
    {
        /// <summary>
        /// Constructs a StructField.
        /// </summary>
        /// <param name="type">Field type.</param>
        /// <param name="name">Field name.</param>
        /// <param name="semantic">Field semantic.</param>
        public StructField(Type type, string name, Semantic semantic)
        {
            FieldType = type;
            FieldName = name;
            FieldSemantic = semantic;
        }

        public Type FieldType;
        public string FieldName;
        public Semantic FieldSemantic;

        public static bool operator ==(StructField A, StructField B)
        {
            if (A.FieldType == B.FieldType 
                && A.FieldName == B.FieldName 
                && A.FieldSemantic == B.FieldSemantic)
                return true;

            return false;
        }

        public static bool operator !=(StructField A, StructField B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            StructField SF = obj as StructField;
            if (SF != null)
                return SF == this;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Abstract type class used as the base for all HlslDom types.
    /// </summary>
    abstract class Type
    {
        public abstract string TypeName();

        protected Type() { }

        public static bool operator ==(Type A, Type B)
        {
            object a = (object)A;
            object b = (object)B;

            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            return A.GetType() == B.GetType();
        }

        public static bool operator !=(Type A, Type B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            return this == (Type)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns the base element type, For example, the base type of a
        /// float4x4 matrix is a float.
        /// </summary>
        /// <returns></returns>
        public abstract Type GetScalarBaseType();

        /// <summary>
        /// Returns the type's dimension. In the case of matrix types this
        /// returns the number of rows in the type.
        /// </summary>
        public abstract int Dimension { get; }

        /// <summary>
        /// Returns the total number of elements in the type.
        /// </summary>
        public abstract int TotalElements { get; }
    }

    /// <summary>
    /// ScalarType is the base type for all scalar types, such as 1-dimensional
    /// bools, floats, ints, and uints.
    /// </summary>
    abstract class ScalarType : Type
    {
        protected ScalarType() { }

        public override Type GetScalarBaseType()
        {
            return this;
        }

        public override int Dimension
        {
            get { return 1; }
        }

        public override int TotalElements
        {
            get { return 1; }
        }
    }

    /// <summary>
    /// Boolean type.
    /// </summary>
    class BoolType : ScalarType
    {
        public BoolType() { }

        public override string TypeName()
        {
            return "bool";
        }
    }

    /// <summary>
    /// 32-bit signed integer type.
    /// </summary>
    class IntType : ScalarType
    {
        public IntType() { }

        public override string TypeName()
        {
            return "int";
        }
    }

    /// <summary>
    /// 32-bit unsigned integer type.
    /// </summary>
    class UIntType : ScalarType
    {
        public UIntType() { }

        public override string TypeName()
        {
            return "uint";
        }
    }

    /// <summary>
    /// 32-bit floating point type.
    /// </summary>
    class FloatType : ScalarType
    {
        public FloatType() { }

        public override string TypeName()
        {
            return "float";
        }
    }

    /// <summary>
    /// Texture sampler type.
    /// </summary>
    class SamplerType : Type
    {
        public SamplerType() { }

        public override string TypeName()
        {
            throw new NotImplementedException();
        }

        public override Type GetScalarBaseType()
        {
            throw new NotImplementedException();
        }

        public override int Dimension
        {
            get { throw new ShaderDomException("Invalid operation on SamplerType."); }
        }

        public override int TotalElements
        {
            get { throw new ShaderDomException("Invalid operation on SamplerType."); }
        }
    }

    /// <summary>
    /// Derived types are the base for non-scalar aggregate types, like
    /// vectors, matrices, or structs.
    /// </summary>
    abstract class DerivedType : Type
    {
        protected DerivedType() { }
    }

    /// <summary>
    /// Multi-element vector type.
    /// </summary>
    class VectorType : DerivedType
    {
        public readonly Type BaseType;
        int VectorDimension;

        /// <summary>
        /// Constructs a vector type. Do not use directly, obtain VectorType instances through the TypeRegistry.
        /// </summary>
        /// <param name="baseType">Base type, must be scalar.</param>
        /// <param name="dimension">Vector dimension (columns).</param>
        public VectorType(Type baseType, int dimension)
        {
            if (!(baseType is ScalarType))
                throw new ShaderDomException("Vector base type must be a scalar!");

            BaseType = baseType;
            VectorDimension = dimension;
        }

        public override string TypeName()
        {
            return BaseType.TypeName() + VectorDimension.ToString();
        }

        public override Type GetScalarBaseType()
        {
            return BaseType;
        }

        /// <summary>
        /// In the case of the vector, the total number of elements is equal to its dimension.
        /// </summary>
        public override int TotalElements
        {
            get { return Dimension; }
        }

        /// <summary>
        /// Returns the number of columns in the vector.
        /// </summary>
        public override int Dimension
        {
            get { return VectorDimension; }
        }
    }

    /// <summary>
    /// Multi-vector matrix type.
    /// </summary>
    class MatrixType : DerivedType
    {
        public readonly Type BaseType;
        int MatrixDimension;

        /// <summary>
        /// Constructor for MatrixTypes. Do not use directly, obtain MatrixType instances through the TypeRegistry.
        /// </summary>
        /// <param name="baseType">Matrix base type, must be a vector with the desired number of columns.</param>
        /// <param name="dimension">Number of rows.</param>
        public MatrixType(Type baseType, int dimension)
        {
            if (!(baseType is VectorType))
                throw new ShaderDomException("Matrix base type must be a vector!");

            BaseType = baseType;
            MatrixDimension = dimension;
        }

        public override string TypeName()
        {
            return BaseType.TypeName() + "x" + MatrixDimension.ToString();
        }

        public override Type GetScalarBaseType()
        {
            return ((DerivedType)BaseType).GetScalarBaseType();
        }

        /// <summary>
        /// Returns the number of rows.
        /// </summary>
        public override int Dimension
        {
            get { return MatrixDimension; }
        }

        /// <summary>
        /// Returns the number of rows multiplied by the number of columns.
        /// </summary>
        public override int TotalElements
        {
            get { return MatrixDimension * ((VectorType)BaseType).Dimension; }
        }
    }

    /// <summary>
    /// StructTypes are the user-defined aggregate types. 
    /// </summary>
    class StructType : DerivedType
    {
        public readonly string Name;
        public readonly StructField[] Fields;

        /// <summary>
        /// Constructs a StructType. Do not use directly, create StructType instances through the TypeRegistry.
        /// </summary>
        /// <param name="name">Structure name.</param>
        /// <param name="fields">Structure fields.</param>
        public StructType(string name, StructField[] fields)
        {
            Name = name;
            Fields = fields;
        }

        public override string TypeName()
        {
            return Name;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            SB.AppendFormat("struct {0} {{{1}", Name, System.Environment.NewLine);

            foreach (StructField SF in Fields)
                SB.AppendFormat("    {0} {1} : {2};{3}",
                    SF.FieldType.TypeName(),
                    SF.FieldName,
                    SF.FieldSemantic,
                    System.Environment.NewLine);

            SB.AppendLine("};");

            return SB.ToString();
        }

        public override Type GetScalarBaseType()
        {
            throw new ShaderDomException("Invalid operation on StructType.");
        }

        public override int Dimension
        {
            get { throw new ShaderDomException("Invalid operation on StructType."); }
        }

        public override int TotalElements
        {
            get { throw new ShaderDomException("Invalid operation on StructType."); }
        }
    }

    /// <summary>
    /// The type registry maintains and tracks all types, from primitive scalar and 
    /// vector/matrix types to samplers and struct types. All types must be managed
    /// via the type registry, all other direct creation of types will result in
    /// undefined behavior.
    /// </summary>
    class TypeRegistry
    {
        static BoolType sBoolType = new BoolType();
        static IntType sIntType = new IntType();
        static UIntType sUIntType = new UIntType();
        static FloatType sFloatType = new FloatType();
        static SamplerType sSamplerType = new SamplerType();

        static List<VectorType> sVectorTypes = new List<VectorType>();
        static List<MatrixType> sMatrixTypes = new List<MatrixType>();
        List<StructType> sStructTypes = new List<StructType>();

        /// <summary>
        /// Returns an instance of BoolType.
        /// </summary>
        /// <returns>BoolType instance.</returns>
        public static BoolType GetBoolType() { return sBoolType; }

        /// <summary>
        /// Returns an instance of IntType.
        /// </summary>
        /// <returns>IntType instance.</returns>
        public static IntType GetIntType() { return sIntType; }

        /// <summary>
        /// Returns an instance of UIntType.
        /// </summary>
        /// <returns>UIntType instance.</returns>
        public static UIntType GetUIntType() { return sUIntType; }

        /// <summary>
        /// Returns an instance of FloatType.
        /// </summary>
        /// <returns>FloatType instance.</returns>
        public static FloatType GetFloatType() { return sFloatType; }

        /// <summary>
        /// Returns an instance of SamplerType.
        /// </summary>
        /// <returns>SamplerType instance.</returns>
        public static SamplerType GetSamplerType() { return sSamplerType; }

        /// <summary>
        /// Returns an instance of VectorType.
        /// </summary>
        /// <param name="baseType">Vector element type.</param>
        /// <param name="dimension">Dimension.</param>
        /// <returns>VectorType instance.</returns>
        public static VectorType GetVectorType(Type baseType, int dimension)
        {
            foreach (VectorType VT in sVectorTypes)
            {
                if (VT.BaseType == baseType && VT.Dimension == dimension)
                    return VT;
            }

            VectorType newVT = new VectorType(baseType, dimension);
            sVectorTypes.Add(newVT);

            return newVT;
        }

        /// <summary>
        /// Returns an instance of MatrixType.
        /// </summary>
        /// <param name="baseType">Matrix element type.</param>
        /// <param name="dimension">Dimension.</param>
        /// <returns>MatrixType instance.</returns>
        public static MatrixType GetMatrixType(Type baseType, int dimension)
        {
            foreach (MatrixType MT in sMatrixTypes)
            {
                if (MT.BaseType == baseType && MT.Dimension == dimension)
                    return MT;
            }

            MatrixType newVT = new MatrixType(baseType, dimension);
            sMatrixTypes.Add(newVT);

            return newVT;
        }

        /// <summary>
        /// Look up a specified StructType. StructTypes are bound to an 
        /// instance of an HlslProgram as they are not defined within the 
        /// scope of the language.
        /// </summary>
        /// <param name="name">StructType name.</param>
        /// <returns>StructType instance if it exists in the system, null otherwise.</returns>
        public StructType GetStructType(string name)
        {
            foreach (StructType ST in sStructTypes)
            {
                if (ST.Name == name)
                    return ST;
            }

            return null;
        }

        /// <summary>
        /// Look up a specified StructType, and creates it with the specified
        /// list of struct fields if an instance doesn't already exist.
        /// </summary>
        /// <param name="name">StructType name.</param>
        /// <param name="fields">List of fields for the created structure.</param>
        /// <returns>StructType instance.</returns>
        public StructType GetStructType(string name, StructField[] fields)
        {
            StructType ST = GetStructType(name);

            if (ST != null)
            {
                bool allFieldsMatch = ST.Fields.Length == fields.Length;

                if (allFieldsMatch)
                {
                    for (int i = 0; i < ST.Fields.Length; ++i)
                    {
                        if (ST.Fields[i] != fields[i])
                        {
                            allFieldsMatch = false;
                            break;
                        }
                    }
                }

                if (allFieldsMatch)
                    return ST;
                else
                    throw new ShaderDomException("Redefinition of existing struct type!");
            }

            StructType newST = new StructType(name, fields);
            sStructTypes.Add(newST);

            return newST;
        }

        /// <summary>
        /// Get a non-mutable collection of all struct types within the current HlslProgram.
        /// </summary>
        /// <returns>Non-mutable collection of all struct types in the current HlslProgram.</returns>
        public ReadOnlyCollection<StructType> GetAllStructTypes()
        {
            return new ReadOnlyCollection<StructType>(sStructTypes);
        }
    }
}
