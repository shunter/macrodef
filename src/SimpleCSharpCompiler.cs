using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NAnt.Core;
using NAnt.Core.Util;

namespace Macrodef
{
    // adapted from <script> task
    internal class SimpleCSharpCompiler
    {
        private readonly CodeDomProvider _provider;

        public SimpleCSharpCompiler()
        {
            _provider = CreateCodeDomProvider("Microsoft.CSharp.CSharpCodeProvider", "System");
        }

        public string GetSourceCode(CodeCompileUnit compileUnit)
        {
            var sw = new StringWriter(CultureInfo.InvariantCulture);

            _provider.GenerateCodeFromCompileUnit(compileUnit, sw, null);

            return sw.ToString();
        }

        private CompilerParameters CreateCompilerOptions()
        {
            var options = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = false, // <script> task uses true - and hence doesn't work properly (second script that contains task defs fails)!
                };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!string.IsNullOrEmpty(asm.Location))
                    {
                        options.ReferencedAssemblies.Add(asm.Location);
                    }
                }
                catch (NotSupportedException)
                {
                    // Ignore - this error is sometimes thrown by asm.Location 
                    // for certain dynamic assemblies
                }
            }

            return options;
        }

        public Assembly CompileAssembly(CodeCompileUnit compileUnit)
        {
            var options = CreateCompilerOptions();
            var results = _provider.CompileAssemblyFromDom(options, compileUnit);

            if (results.Errors.Count > 0)
            {
                var errors = new StringBuilder();
                errors.AppendLine("Errors:");
                
                foreach (CompilerError err in results.Errors)
                {
                    errors.AppendLine(err.ToString());
                }

                errors.Append(GetSourceCode(compileUnit));

                throw new BuildException(errors.ToString());
            }

            return results.CompiledAssembly;
        }

        private CodeDomProvider CreateCodeDomProvider(string typeName, string assemblyName)
        {
            var providerAssembly = Assembly.Load(assemblyName);
            
            if (providerAssembly == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ResourceUtils.GetString("NA2037"), assemblyName));
            }

            var providerType = providerAssembly.GetType(typeName, true, true);
            var provider = Activator.CreateInstance(providerType);
            
            if (!(provider is CodeDomProvider))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ResourceUtils.GetString("NA2038"), providerType.FullName));
            }

            return (CodeDomProvider) provider;
        }
    }
}
