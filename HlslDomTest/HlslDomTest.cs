////////////////////////////////////////////////////////////////////////
// HlslDom, Copyright (c) 2010, Maximilian Burke
// This file is distributed under the FreeBSD license. 
// See LICENSE.TXT for details.
////////////////////////////////////////////////////////////////////////
// This file includes basic tests for HlslDom functionality.
////////////////////////////////////////////////////////////////////////


using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Hlsl.Expressions;

namespace Hlsl
{
    class Test
    {
        public static Type CreateVSType(HlslProgram program)
        {
            Type f = TypeRegistry.GetFloatType();
            Type float4 = TypeRegistry.GetVectorType(f, 4);
            Type float3 = TypeRegistry.GetVectorType(f, 3);
            Type float2 = TypeRegistry.GetVectorType(f, 2);
            Type vsData = program.Types.GetStructType("vs_input", new StructField[] {
                    new StructField(float4, "position", new Semantic(Semantic.SemanticType.POSITION)),
                    new StructField(float2, "surfaceUV", new Semantic(Semantic.SemanticType.TEXCOORD, 0)),
                    new StructField(float2, "lightmapUV", new Semantic(Semantic.SemanticType.TEXCOORD, 1)),
                    new StructField(float3, "normal", new Semantic(Semantic.SemanticType.NORMAL)),
                    new StructField(float4, "color", new Semantic(Semantic.SemanticType.COLOR))
                });

            return vsData;
        }

        public static string EmptyProgramTest()
        {
            try
            {
                using (HlslProgram program = new HlslProgram())
                {
                    return program.EmitRawShaderCode();
                }
            }
            catch (ShaderDomException)
            {
            }

            return null;
        }


        public static string ArgAssignedToOutputTest()
        {
            using (HlslProgram program = new HlslProgram())
            {
                Type vsData = CreateVSType(program);

                UserDefinedFunction udf = new UserDefinedFunction("vs_main");
                Value argValue = udf.AddArgument(vsData);

                DeclExpr output = new DeclExpr(vsData, argValue);
                udf.AddExpr(output);
                udf.AddExpr(new ReturnExpr(output));

                program.AddFunction(udf);
                return program.EmitRawShaderCode();
            }
        }

        public static string SimpleStructMemberTest()
        {
            using (HlslProgram program = new HlslProgram())
            {
                Type vsData = CreateVSType(program);

                UserDefinedFunction udf = new UserDefinedFunction("vs_main");
                Value argValue = udf.AddArgument(vsData);

                DeclExpr output = new DeclExpr(vsData);
                udf.AddExpr(output);
                udf.AddExpr(new AssignmentExpr(
                    new StructMemberExpr(output.Value, "position").Value, 
                    new StructMemberExpr(argValue, "position").Value));

                StructMemberExpr[] otherMembers = new StructMemberExpr[] {
                    new StructMemberExpr(output.Value, 1),
                    new StructMemberExpr(output.Value, 2),
                    new StructMemberExpr(output.Value, 3),
                    new StructMemberExpr(output.Value, 4),
                };

                foreach (StructMemberExpr SME in otherMembers)
                    udf.AddExpr(new AssignmentExpr(SME.Value, new LiteralExpr(SME.Value.ValueType).Value));

                udf.AddExpr(new ReturnExpr(output));

                program.AddFunction(udf);
                return program.EmitRawShaderCode();
            }
        }

        public static string SimpleFunctionCallTest()
        {
            using (HlslProgram program = new HlslProgram())
            {
                Type vsData = CreateVSType(program);
                Type f1 = TypeRegistry.GetFloatType();
                Type f4 = TypeRegistry.GetVectorType(f1, 4);

                DeclExpr wvpMatrixDecl = new DeclExpr(TypeRegistry.GetMatrixType(f4, 4), "WorldViewProjection");
                program.AddGlobal(wvpMatrixDecl);

                UserDefinedFunction udf = new UserDefinedFunction("vs_main");
                Value argValue = udf.AddArgument(vsData);

                DeclExpr output = new DeclExpr(vsData);
                udf.AddExpr(output);

                // Initialize the position element -- multiply input position by WVP matrix.
                Function fn = program.GetFunctionByName("mul");
                CallExpr wvpMul = new CallExpr(fn, new Expr[] { new StructMemberExpr(argValue, "position"), wvpMatrixDecl });
                udf.AddExpr(new AssignmentExpr(new StructMemberExpr(output.Value, "position").Value, wvpMul.Value));

                // Initialize the rest of the struct to zero.
                StructMemberExpr[] otherMembers = new StructMemberExpr[] {
                    new StructMemberExpr(output.Value, 1),
                    new StructMemberExpr(output.Value, 2),
                    new StructMemberExpr(output.Value, 3),
                    new StructMemberExpr(output.Value, 4),
                };

                udf.AddExpr(new CommentExpr("Ensuring that a valid comment is being emitted."));
                udf.AddExpr(new CommentExpr("Even one that consists of multiple lines."));
                udf.AddExpr(new CommentExpr(string.Format("Or embedded newlines.{0}Like this!", Environment.NewLine)));
                udf.AddExpr(new CommentExpr("Or this.\n(not a proper newline.)"));

                foreach (StructMemberExpr SME in otherMembers)
                    udf.AddExpr(new AssignmentExpr(SME.Value, new LiteralExpr(SME.Value.ValueType).Value));

                udf.AddExpr(new ReturnExpr(output));

                program.AddFunction(udf);
                return program.EmitRawShaderCode();
            }
        }

        public static int Validate(string shaderCode, string entryPoint, Microsoft.Xna.Framework.Graphics.ShaderProfile profile)
        {
            CompiledShader CS = ShaderCompiler.CompileFromSource(shaderCode, 
                new CompilerMacro[] { }, 
                null, 
                CompilerOptions.None, 
                entryPoint, 
                profile, 
                TargetPlatform.Windows);

            if (!CS.Success)
            {
                Console.WriteLine("Failed to compile shader:");
                Console.WriteLine("{0}", shaderCode);
                Console.WriteLine("-------------------------");
                Console.WriteLine("{0}", CS.ErrorsAndWarnings);
            }

            return CS.Success ? 0 : 1;
        }

        public static int Validate(string shaderCode)
        {
            return Validate(shaderCode, "vs_main", Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_3_0);
        }

        public static int Main(string[] args)
        {
            int result = 0;

            result += EmptyProgramTest() == "" ? 0 : 1;
            result += Validate(ArgAssignedToOutputTest());
            result += Validate(SimpleStructMemberTest());
            result += Validate(SimpleFunctionCallTest());

            Console.WriteLine("*** {0} tests failed ***", result);
            return result;
        }
    }
}
