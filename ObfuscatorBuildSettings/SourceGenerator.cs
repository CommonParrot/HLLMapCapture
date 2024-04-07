﻿using Microsoft.CodeAnalysis;

namespace ObfuscatorBuildSettings
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var password = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.Password", out var passwordInput)
            ? passwordInput : "";

            // Build up the source code
            string source = $@"// <auto-generated/>
            using System;
            using System.Reflection;
            using System.Security.Cryptography;
            using System.Text;

            namespace HLLMapCapture
            {{
                internal static partial class Obfuscator
                {{
                    private static partial string GetKey()
                    {{
                        string password = ""{password}"";
                        byte[] bytes = Encoding.UTF8.GetBytes(password);
                        MD5 mD5 = MD5.Create();
                        string hash = Convert.ToBase64String(mD5.ComputeHash(bytes));
                        return hash;
                    }}
                }}
            }}
            ";

            // Add the source code to the compilation
            context.AddSource($"Obfuscator.g.cs", source);
        }

        public void Initialize(GeneratorInitializationContext context){}
    }
}
