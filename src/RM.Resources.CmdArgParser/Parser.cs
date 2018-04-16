﻿
namespace RM.Resources.CmdArgParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Parser
    {

        public static List<string> Parse(Action<CmdArgConfigurator> configAction)
        {
            var config = new CmdArgConfiguration();
            var configurator = new CmdArgConfigurator(config);
            try
            {
                configAction(configurator);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Exception thrown in config action passed to CmdArgParser. Parse method. See inner exception for more details", e);
            }
            var args = Environment.GetCommandLineArgs();
            var extraArgs = new List<string>();
            if (args.Length > 0)
            {
                var argsWithoutLocation = args.Skip(1).ToArray();
                if (argsWithoutLocation.Length > 0)
                {
                    foreach (var argument in argsWithoutLocation)
                    {
                        var argumentLC = argument.ToLower();
                        var argParsed = false;
                        foreach (var parameter in config.parameters)
                        {
                            foreach (var key in parameter.GetKeys())
                            {
                                if (argumentLC.StartsWith(key))
                                {
                                    var rightSide = argument.Substring(key.Length);
                                    if (string.IsNullOrEmpty(rightSide))
                                    {
                                        parameter.GetValue()(string.Empty);
                                        argParsed = true;
                                    }
                                    else if (rightSide.StartsWith(":"))
                                    {
                                        var value = rightSide.Substring(1);
                                        parameter.GetValue()(value);
                                        argParsed = true;
                                    }
                                }
                            }
                        }
                        if (!argParsed)
                        {
                            extraArgs.Add(argument);
                        }
                    }
                }
            }
            if (extraArgs.Count > 0 && config.ShowHelpOnExtraArguments)
            {
                Console.WriteLine("Unrecognized arguments: ");
                foreach (var extraArg in extraArgs)
                {
                    Console.WriteLine("Key: {0}", extraArg);
                }
            }
            return extraArgs;
        }
    }
}