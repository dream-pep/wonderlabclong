using System;
using System.Collections.Generic;
using System.Text;

namespace buildtasks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is a msbuild task.");
            BuildTask buildTask = new BuildTask();
            buildTask.BuildEngine = new ConsoleBuildEngine();
            buildTask.Input = "C:\\Users\\Ddggdd135\\source\\repos\\Blessing-Studio\\WonderLab.Override\\wonderlab\\bin\\Debug\\net8.0\\WonderLab.dll";
            buildTask.Output = buildTask.Input;
            buildTask.Temp = buildTask.Output + ".tmp";
            buildTask.Execute();
        }
    }
}
