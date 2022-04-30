﻿using System;
using System.Linq;
using DotnetRuntimeBootstrapper.AppHost.Core;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils;

namespace DotnetRuntimeBootstrapper.AppHost.Cli;

public partial class Program
{
    private readonly TargetAssembly _targetAssembly;

    public Program(TargetAssembly targetAssembly) =>
        _targetAssembly = targetAssembly;

    private bool InstallMissingPrerequisites()
    {
        var missingPrerequisites = _targetAssembly.GetMissingPrerequisites();
        if (!missingPrerequisites.Any())
            return true;

        /*
        // Prompt installation
        if (ShowForm(new InstallationPromptForm(_targetAssembly, missingPrerequisites)) != DialogResult.Yes)
            return false;

        // Perform installation
        if (ShowForm(new InstallationForm(_targetAssembly, missingPrerequisites)) == DialogResult.Retry)
        {
            // Reboot is required
            if (MessageBox.Show(
                    @$"You need to restart Windows before you can run {_targetAssembly.Title}. " +
                    @"Would you like to do it now?",
                    @"Restart required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OperatingSystem.Reboot();
            }

            return false;
        }*/

        // Reset environment variables to update PATH and other variables
        // that may have been changed by the installation process.
        EnvironmentEx.ResetEnvironmentVariables();

        return true;
    }

    private int Run(string[] args)
    {
        try
        {
            // Attempt to run the target first without any checks (hot path)
            return _targetAssembly.Run(args);
        }
        // Possible exception causes:
        // - .NET host not found (DirectoryNotFoundException)
        // - .NET host failed to initialize (ApplicationException)
        catch
        {
            if (!InstallMissingPrerequisites())
            {
                // User canceled or reboot is required
                return 0xB007;
            }

            // Attempt to run the target again
            return _targetAssembly.Run(args);
        }
    }
}

public partial class Program
{
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Error.Report(e.ExceptionObject.ToString());

        try
        {
            return new Program(TargetAssembly.Resolve()).Run(args);
        }
        catch (Exception ex)
        {
            Error.Report(ex);
            return 0xDEAD;
        }
    }
}