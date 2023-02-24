using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;
using Microsoft.Win32;

namespace HarmonyTools.Drivers
{
    [SupportedOSPlatform("windows")]
    internal sealed class ContextMenuDriver : IDriver
    {
        public string CommandName => "context-menu";
        public string CommandDescription => "Manages custom context menu";

        private readonly string binaryPath;
        private readonly string installationPath;

        public ContextMenuDriver()
        {
            binaryPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            installationPath = Path.GetDirectoryName(binaryPath)!;

            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new ContextMenuException("Could not get binary path.");
            }
        }

        public Command GetCommand()
        {
            var command = new Command(CommandName, CommandDescription);

            var registerCommand = new Command("register", "Registers context menu");
            var unregisterCommand = new Command("unregister", "Unregisters context menu");

            registerCommand.SetHandler(Register);
            unregisterCommand.SetHandler(Unregister);

            command.AddCommand(registerCommand);
            command.AddCommand(unregisterCommand);

            return command;
        }

        private void Register()
        {
            // csharpier-ignore-start
            Console.WriteLine("WARNING: Note that you should not delete or move HarmonyTools binary file.");
            Console.WriteLine("         Otherwise, context menu will not work properly.");
            Console.WriteLine();
            Console.WriteLine("         If you really need to move it somewhere else, you should unregister context menu first.");
            Console.WriteLine("         Then, move the binary file and register context menu again.");
            // csharpier-ignore-end

            Console.WriteLine($"Info: Setting \"{installationPath}\" as installation path.");
            Console.WriteLine($"Info: Setting \"{binaryPath}\" as binary path.");

            if (
                DoesKeyExists(@"*\shell\HarmonyTools")
                || DoesKeyExists(@"Directory\shell\HarmonyTools")
                || DoesKeyExists(@"Directory\Background\shell\HarmonyTools")
            )
            {
                throw new ContextMenuException("HarmonyTools context menu is already registered.");
            }

            var iconsPath = Path.Combine(installationPath, "Icons");

            var drivers = new List<IContextMenuDriver>()
            {
                new DialogueDriver(),
                new StxDriver(),
                new SpcDriver(),
                new SrdDriver(),
                new DatDriver(),
                new FontDriver(),
                new CpkDriver()
            };

            var contextMenuItems = drivers.SelectMany(driver => driver.GetContextMenu()).ToList();

            var fileItems = contextMenuItems
                .Where(item => item.ApplyTo.IsFile && !item.IsBatch)
                .OrderBy(item => item.Group)
                .ToList();
            var dirItems = contextMenuItems
                .Where(item => item.ApplyTo.IsDirectory && !item.IsBatch)
                .OrderBy(item => item.Group)
                .ToList();
            var dirBgItems = contextMenuItems.Where(item => item.IsBatch).OrderBy(item => item.Group).ToList();

            try
            {
                var htFileRoot = Registry.ClassesRoot.CreateSubKey(@"*\shell\HarmonyTools");
                var htDirRoot = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\HarmonyTools");
                var htDirBgRoot = Registry.ClassesRoot.CreateSubKey(@"Directory\Background\shell\HarmonyTools");

                htFileRoot.SetValue("Icon", Path.Combine(iconsPath, "Harmony-Tools-Icon.ico"));
                htFileRoot.SetValue("MUIVerb", "Harmony Tools");
                htFileRoot.SetValue("Position", "Top");
                htFileRoot.SetValue("subcommands", "");

                htDirRoot.SetValue("Icon", Path.Combine(iconsPath, "Harmony-Tools-Icon.ico"));
                htDirRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirRoot.SetValue("Position", "Top");
                htDirRoot.SetValue("subcommands", "");

                htDirBgRoot.SetValue("Icon", Path.Combine(iconsPath, "Harmony-Tools-Icon.ico"));
                htDirBgRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirBgRoot.SetValue("Position", "Top");
                htDirBgRoot.SetValue("subcommands", "");

                var htFileShell = htFileRoot.CreateSubKey("shell");
                var htDirShell = htDirRoot.CreateSubKey("shell");
                var htDirBgShell = htDirBgRoot.CreateSubKey("shell");

                uint previousGroup = 0;
                uint subKeyIndex = 0;

                foreach (var item in fileItems)
                {
                    htFileShell.RegisterHTCommand(
                        subKeyID: CreateRegistryID(subKeyIndex, item),
                        name: item.Name,
                        icon: Path.Combine(iconsPath, item.Icon),
                        command: $"{binaryPath} {item.Command}",
                        hasSeparatorAbove: item.Group != previousGroup
                    );

                    previousGroup = item.Group;
                    subKeyIndex++;
                }

                previousGroup = 0;
                subKeyIndex = 0;

                foreach (var item in dirItems)
                {
                    htDirShell.RegisterHTCommand(
                        subKeyID: CreateRegistryID(subKeyIndex, item),
                        name: item.Name,
                        icon: Path.Combine(iconsPath, item.Icon),
                        command: $"{binaryPath} {item.Command}",
                        hasSeparatorAbove: item.Group != previousGroup
                    );

                    previousGroup = item.Group;
                    subKeyIndex++;
                }

                previousGroup = 0;
                subKeyIndex = 0;

                foreach (var item in dirBgItems)
                {
                    htDirBgShell.RegisterHTCommand(
                        subKeyID: CreateRegistryID(subKeyIndex, item),
                        name: item.Name,
                        icon: Path.Combine(iconsPath, item.Icon),
                        command: $"{binaryPath} {item.Command}",
                        hasSeparatorAbove: item.Group != previousGroup
                    );

                    previousGroup = item.Group;
                    subKeyIndex++;
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Unregister();
                throw new ContextMenuException("You do not have permission to register the context menu.");
            }
        }

        private void Unregister()
        {
            try
            {
                if (DoesKeyExists(@"*\shell\HarmonyTools"))
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\HarmonyTools");
                }

                if (DoesKeyExists(@"Directory\shell\HarmonyTools"))
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\HarmonyTools");
                }

                if (DoesKeyExists(@"Directory\Background\shell\HarmonyTools"))
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\Background\shell\HarmonyTools");
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                throw new ContextMenuException("You do not have permission to unregister the context menu.");
            }
        }

        private bool DoesKeyExists(string keyName) => Registry.ClassesRoot.OpenSubKey(keyName, false) != null;

        private string CreateRegistryID(uint subKeyIndex, IContextMenuEntry item) =>
            subKeyIndex.ToString().PadLeft(2, '0') + "_" + item.SubKeyID;
    }
}
