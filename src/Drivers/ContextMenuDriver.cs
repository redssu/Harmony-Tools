using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
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

            var drivers = new List<IContextMenuDriver>()
            {
                new DialogueDriver(),
                new StxDriver(),
                new SpcDriver(),
                new SrdDriver(),
                new DatDriver(),
                new FontDriver(),
                new WrdDriver(),
                new CpkDriver(),
            };

            try
            {
                var htFileRoot = Registry.ClassesRoot.CreateSubKey(@"*\shell\HarmonyTools");
                var htDirRoot = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\HarmonyTools");
                var htDirBgRoot = Registry.ClassesRoot.CreateSubKey(@"Directory\Background\shell\HarmonyTools");

                htFileRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htFileRoot.SetValue("MUIVerb", "Harmony Tools");
                htFileRoot.SetValue("Position", "Top");
                htFileRoot.SetValue("subcommands", "");

                htDirRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htDirRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirRoot.SetValue("Position", "Top");
                htDirRoot.SetValue("subcommands", "");

                htDirBgRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htDirBgRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirBgRoot.SetValue("Position", "Top");
                htDirBgRoot.SetValue("subcommands", "");

                var htFileShell = htFileRoot.CreateSubKey("shell");
                var htDirShell = htDirRoot.CreateSubKey("shell");
                var htDirBgShell = htDirBgRoot.CreateSubKey("shell");

                foreach (var driver in drivers)
                {
                    var contextMenuItems = driver.GetContextMenu().ToList();

                    foreach (var item in contextMenuItems)
                    {
                        var htShell = item.ApplyTo.IsDirectory ? htDirShell : htFileShell;

                        htShell.RegisterHTCommand(
                            item.SubKeyID,
                            item.Name,
                            Path.Combine(installationPath, item.Icon),
                            $"{binaryPath} {item.Command}"
                        );
                    }
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                throw new ContextMenuException("You do not have permission to register the context menu.");
            }

            /*
            try
            {
                RegistryKey HarmonyTools = Registry.ClassesRoot.CreateSubKey(
                    @"Directory\Background\shell\HarmonyTools"
                );

                HarmonyTools.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                HarmonyTools.SetValue("MUIVerb", "Harmony Tools");
                HarmonyTools.SetValue("Position", "Top");
                HarmonyTools.SetValue("subcommands", "");

                RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey("shell");

                /**
                 * UNPACKING
                 *\/

                // Unpack All Dialogue
                RegistryKey UnpackAllDialogueItem = HarmonyToolsShell.CreateSubKey(
                    "1_UnpackAllDialogue"
                );
                UnpackAllDialogueItem.SetValue("MUIVerb", texts[language]["UnpackAllDialogueName"]);
                UnpackAllDialogueItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllDialogueCommand = UnpackAllDialogueItem.CreateSubKey(
                    "command"
                );
                UnpackAllDialogueCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=DIALOGUE --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All STX
                RegistryKey UnpackAllSTXItem = HarmonyToolsShell.CreateSubKey("2_UnpackAllSTX");
                UnpackAllSTXItem.SetValue("MUIVerb", texts[language]["UnpackAllSTXName"]);
                UnpackAllSTXItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllSTXCommand = UnpackAllSTXItem.CreateSubKey("command");
                UnpackAllSTXCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=STX --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All DAT
                RegistryKey UnpackAllDATItem = HarmonyToolsShell.CreateSubKey("3_UnpackAllDAT");
                UnpackAllDATItem.SetValue("MUIVerb", texts[language]["UnpackAllDATName"]);
                UnpackAllDATItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllDATCommand = UnpackAllDATItem.CreateSubKey("command");
                UnpackAllDATCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack -format=DAT --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All SPC
                RegistryKey UnpackAllSPCItem = HarmonyToolsShell.CreateSubKey("4_UnpackAllSPC");
                UnpackAllSPCItem.SetValue("MUIVerb", texts[language]["UnpackAllSPCName"]);
                UnpackAllSPCItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-Icon.ico"
                );

                RegistryKey UnpackAllSPCCommand = UnpackAllSPCItem.CreateSubKey("command");
                UnpackAllSPCCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=SPC --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All SRD
                RegistryKey UnpackAllSRDItem = HarmonyToolsShell.CreateSubKey("5_UnpackAllSRD");
                UnpackAllSRDItem.SetValue("MUIVerb", texts[language]["UnpackAllSRDName"]);
                UnpackAllSRDItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-Icon.ico"
                );
                UnpackAllSRDItem.SetValue("CommandFlags", (uint)0x40, RegistryValueKind.DWord);

                RegistryKey UnpackAllSRDCommand = UnpackAllSRDItem.CreateSubKey("command");
                UnpackAllSRDCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=SRD --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // ----------

                /**
                 * PACKING
                 *\/

                // Pack All Dialogue
                RegistryKey PackAllDialogueItem = HarmonyToolsShell.CreateSubKey(
                    "6_PackAllDialogue"
                );
                PackAllDialogueItem.SetValue("MUIVerb", texts[language]["PackAllDialogueName"]);
                PackAllDialogueItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllDialogueCommand = PackAllDialogueItem.CreateSubKey("command");
                PackAllDialogueCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=DIALOGUE --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All STX
                RegistryKey PackAllSTXItem = HarmonyToolsShell.CreateSubKey("7_PackAllSTX");
                PackAllSTXItem.SetValue("MUIVerb", texts[language]["PackAllSTXName"]);
                PackAllSTXItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllSTXCommand = PackAllSTXItem.CreateSubKey("command");
                PackAllSTXCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=STX --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All DAT
                RegistryKey PackAllDATItem = HarmonyToolsShell.CreateSubKey("8_PackAllDAT");
                PackAllDATItem.SetValue("MUIVerb", texts[language]["PackAllDATName"]);
                PackAllDATItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllDATCommand = PackAllDATItem.CreateSubKey("command");
                PackAllDATCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=DAT --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All SPC
                RegistryKey PackAllSPCItem = HarmonyToolsShell.CreateSubKey("9_PackAllSPC");
                PackAllSPCItem.SetValue("MUIVerb", texts[language]["PackAllSPCName"]);
                PackAllSPCItem.SetValue("Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico");

                RegistryKey PackAllSPCCommand = PackAllSPCItem.CreateSubKey("command");
                PackAllSPCCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=SPC --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Error: You don't have permission to register the context menu.");
                Console.WriteLine("Tip: Try running this command prompt as administrator");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                return;
            }
            */
        }

        private void Unregister()
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

        private bool DoesKeyExists(string keyName)
        {
            var key = Registry.ClassesRoot.OpenSubKey(keyName, false);
            return key != null;
        }
    }
}
