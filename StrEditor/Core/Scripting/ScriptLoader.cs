using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GRF.FileFormats.StrFormat;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using TokeiLibrary;
using Utilities.Extension;

namespace StrEditor.Core.Scripting {
	public class ScriptLoader {
		public class ScriptCompileResult {
			public bool Success { get; set; }
			public EmitResult CompileResult { get; set; }
			public string DllOutput { get; set; }
		}

		public class LoadScriptResult {
			public bool Success { get; set; }
			public string ErrorMessage { get; set; }
			public EmitResult EmitResult { get; set; }
			public IStrScript StrScript { get; set; }
			public string ScriptPath { get; set; }
			public string OriginalScriptPath { get; set; }
			public string OriginalDllPath { get; set; }

			public static LoadScriptResult Fail(string message, string scriptPath) {
				LoadScriptResult result = new LoadScriptResult();
				result.Success = false;
				result.ErrorMessage = message;
				result.ScriptPath = scriptPath;
				return result;
			}

			public static LoadScriptResult Fail(EmitResult emitResult, string scriptPath) {
				LoadScriptResult result = new LoadScriptResult();
				result.Success = false;
				result.ErrorMessage = "Failed to compile script.";
				result.EmitResult = emitResult;
				result.ScriptPath = scriptPath;
				return result;
			}
		}

		private static List<PortableExecutableReference> _references;
		private static object _loadReferenceLock = new object();

		public static List<PortableExecutableReference> GetReferences() {
			LoadReferences();
			return _references;
		}

		private static void LoadReferences() {
			lock (_loadReferenceLock) {
				if (_references == null) {
					_references = new List<PortableExecutableReference>();

					var refs = AppDomain.CurrentDomain
						.GetAssemblies()
						.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)).ToList();

					foreach (var refName in Assembly.GetExecutingAssembly().GetReferencedAssemblies()) {
						if (refs.Any(a => a.GetName().FullName == refName.FullName))
							continue;

						try {
							var loaded = Assembly.Load(refName);
							if (!loaded.IsDynamic && !string.IsNullOrEmpty(loaded.Location)) {
								refs.Add(loaded);
							}
						}
						catch {
							// Some references might not resolve (satellite assemblies, etc.)
						}
					}

					foreach (var refAssembly in refs) {
						PortableExecutableReference metaReference;

						//if (File.Exists(refAssembly.Location.ReplaceExtension(".xml"))) {
						//	metaReference = MetadataReference.CreateFromFile(refAssembly.Location, documentation: XmlDocumentationProvider.CreateFromFile(refAssembly.Location.ReplaceExtension(".xml")));
						//}
						//else {
						metaReference = MetadataReference.CreateFromFile(refAssembly.Location);
						//}

						_references.Add(metaReference);
					}
				}
			}
		}

		public static ScriptCompileResult CompileFromText(string scriptText, string dllOutput) {
			scriptText = scriptText.ReplaceAll("using GRF.System", "using GRF.GrfSystem");
			var syntaxTree = CSharpSyntaxTree.ParseText(scriptText);

			ScriptCompileResult compileResult = new ScriptCompileResult();

			LoadReferences();

			var compilation = CSharpCompilation.Create(
				"DynamicAssembly",
				new[] { syntaxTree },
				_references,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			EmitResult result = null;

			using (var ms = new MemoryStream()) {
				result = compilation.Emit(ms);
				if (result.Success && dllOutput != null) {
					ms.Seek(0, SeekOrigin.Begin);
					File.WriteAllBytes(dllOutput, ms.ToArray());
				}
			}

			compileResult.Success = result.Success;
			compileResult.CompileResult = result;
			compileResult.DllOutput = dllOutput;
			return compileResult;
		}

		public static LoadScriptResult LoadScriptFromAssembly(string assemblyPath, string sourcePath) {
			Assembly assembly = Assembly.LoadFile(assemblyPath);
			object o = assembly.CreateInstance("Scripts.Script");
			sourcePath = sourcePath ?? assemblyPath;

			if (o == null)
				return LoadScriptResult.Fail("Couldn't instantiate the script object. Type not found?", sourcePath);

			IStrScript actScript = o as IStrScript;

			if (actScript == null)
				return LoadScriptResult.Fail("Couldn't instantiate the script object. Type not found?", sourcePath);

			LoadScriptResult result = new LoadScriptResult();
			result.Success = true;
			result.StrScript = actScript;
			return result;
		}

		internal static void DummyCompile() {
			CompileFromText(Encoding.Default.GetString(ApplicationManager.GetResource("dummy_script.cs")), null);
		}
	}
}
