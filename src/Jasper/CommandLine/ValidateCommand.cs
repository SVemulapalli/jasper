﻿using System;
using Jasper.Messaging.Model;
using LamarCompiler;
using Oakton;

namespace Jasper.CommandLine
{
    [Description("Validate the configuration and environment for this Jasper application")]
    public class ValidateCommand : OaktonCommand<JasperInput>
    {
        public override bool Execute(JasperInput input)
        {
            Console.WriteLine("Bootstrapping the system and running all checks...");
            using (var runtime = input.BuildRuntime(StartMode.Lightweight))
            {
                Console.WriteLine("Generating code for all the message handlers, this might take a bit...");
                Console.WriteLine();
                Console.WriteLine();

                var rules = runtime.CodeGeneration;
                var generatedAssembly = new GeneratedAssembly(rules);
                var handlers = runtime.Get<HandlerGraph>();
                foreach (var handler in handlers.Chains) handler.AssembleType(generatedAssembly, rules);
            }

            ConsoleWriter.Write(ConsoleColor.Green, "All systems good!");

            return true;
        }
    }
}
