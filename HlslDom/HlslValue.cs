////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////
// This file defines the Value type. HlslDom Values represent a paired
// type + tag and can be used as an the input to expressions and calls
// to functions.
////////////////////////////////////////////////////////////////////////

using System.Text;

namespace Hlsl
{
    /// <summary>
    /// Value semantic.
    /// </summary>
    struct Semantic
    {
        public SemanticType Type;
        public int Index;

        public enum SemanticType
        {
            NONE,

            // Vertex shader semantics
            BINORMAL,
            BLENDINDICES,
            BLENDWEIGHT,
            COLOR,
            NORMAL,
            POSITION,
            POSITIONT,
            PSIZE,
            TANGENT,
            TEXCOORD,
            FOG,
            TESSFACTOR,
            INDEX,

            // Pixel shader specific semantics
            VFACE,
            VPOS,
            DEPTH
        }

        public Semantic(SemanticType type)
        {
            Type = type;
            Index = -1;
        }

        public Semantic(SemanticType type, int index)
        {
            Type = type;
            Index = index;
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder(Type.ToString());
            if (Index >= 0)
                SB.Append(Index);

            return SB.ToString();
        }

        public static bool operator ==(Semantic A, Semantic B)
        {
            return A.Type == B.Type && A.Index == B.Index;
        }

        public static bool operator !=(Semantic A, Semantic B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            return this == (Semantic)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// HlslDom Values represent a pairing of types with a referenceable name and
    /// optional semantic.
    /// </summary>
    class Value
    {
        public Type ValueType { get; private set; }
        public string Name;
        public Semantic Semantic;

        public Value(Type valueType, string name)
        {
            ValueType = valueType;
            Name = name;
        }

        public Value(Type valueType, string name, Semantic semantic)
        {
            ValueType = valueType;
            Name = name;
            Semantic = semantic;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
