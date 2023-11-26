using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections;

public class BuildTask : ITask
{
    public IBuildEngine BuildEngine { get; set; }
    public ITaskHost HostObject { get; set; }
    [Required]
    public string? Input { get; set; }
    [Required]
    public string? Output { get; set; }
    [Required]
    public string? Temp { get; set; }

    public bool Execute()
    {
        try
        {
            FileInfo fileInfo = new(Temp);
            fileInfo.Directory.Create();
            File.Copy(Input, Temp, true);
            string InputPdb = GetPdbPath(Input);
            string TempPdb = GetPdbPath(Temp);
            string OutputPdb = GetPdbPath(Output);
            ReaderParameters ReaderParameters = new ReaderParameters();
            if (File.Exists(InputPdb))
            {
                File.Copy(InputPdb, TempPdb, true);
                ReaderParameters.ReadSymbols = true;
            }
            ReaderParameters.ReadWrite = true;
            AssemblyDefinition asAss = AssemblyDefinition.ReadAssembly(Temp, ReaderParameters);
            TypeSystem typeSystem = asAss.MainModule.TypeSystem;
            try
            {
                foreach (TypeDefinition type in asAss.MainModule.GetTypes())
                {
                    foreach (MethodDefinition method in type.GetMethods())
                    {
                        if(method.Body == null) continue;
                        try
                        {
                            var ins = method.Body.Instructions;
                            ins.Insert(0, Instruction.Create(OpCodes.Ldstr, "执行 " + method.FullName));
                            ins.Insert(1, Instruction.Create(OpCodes.Call, asAss.MainModule.Import(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));
                        }
                        catch { }
                    }
                }
            }
            catch { }
            WriterParameters WriterParameters = new WriterParameters();
            WriterParameters.WriteSymbols = ReaderParameters.ReadSymbols;
            asAss.Write(WriterParameters);
            asAss.Dispose();
            File.Copy(Temp, Output, true);
            if(ReaderParameters.ReadSymbols)
                File.Copy(TempPdb, OutputPdb, true);
            return true;
        }
        catch (Exception ex)
        {
            BuildEngine.LogErrorEvent(new("BuildTasks", "BuildTask0", Input, 0, 0, 0, 0, ex.StackTrace.ToString(), "", "BuildTask"));
            return false;
        }
    }
    string GetPdbPath(string p)
    {
        var d = Path.GetDirectoryName(p);
        var f = Path.GetFileNameWithoutExtension(p);
        var rv = f + ".pdb";
        if (d != null)
            rv = Path.Combine(d, rv);
        return rv;
    }
}
class ConsoleBuildEngine : IBuildEngine
{
    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        Console.WriteLine($"ERROR: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
    }

    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        Console.WriteLine($"WARNING: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
    }

    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        Console.WriteLine($"MESSAGE: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
    }

    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        Console.WriteLine($"CUSTOM: {e.Message}");
    }

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
        IDictionary targetOutputs) => throw new NotSupportedException();

    public bool ContinueOnError { get; }
    public int LineNumberOfTaskNode { get; }
    public int ColumnNumberOfTaskNode { get; }
    public string ProjectFileOfTaskNode { get; }
}
