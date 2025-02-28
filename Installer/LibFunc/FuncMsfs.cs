using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;

namespace CFIT.Installer.LibFunc
{
    public static class FuncMsfs
    {
        public static string MsfsStringPackagePath { get; } = "InstalledPackagesPath ";
        public static string MsfsStringPackageVersion { get; } = "package_version";
        public static string MsfsManifestFile { get; } = "manifest.json";
        public static string MsfsConfigFile { get; } = "UserCfg.opt";
        public static string MsfsAutoStartFile { get; } = "EXE.xml";
        public static string MsfsBinary2020 { get; } = "FlightSimulator";
        public static string MsfsBinary2024 { get; } = "FlightSimulator2024";

        public static Dictionary<Simulator, string[]> MsfsConfigPaths { get; } = new Dictionary<Simulator, string[]>()
        {
            {
                Simulator.MSFS2020,
                new string[] {
                    $@"{Sys.FolderAppDataLocal()}\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\{MsfsConfigFile}",
                    $@"{Sys.FolderAppDataRoaming()}\Microsoft Flight Simulator\{MsfsConfigFile}"
                }
            },
            {
                Simulator.MSFS2024,
                new string[] {
                    $@"{Sys.FolderAppDataLocal()}\Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\{MsfsConfigFile}",
                    $@"{Sys.FolderAppDataRoaming()}\Microsoft Flight Simulator 2024\{MsfsConfigFile}",
                }
            },
        };

        public static Dictionary<Simulator, string[]> MsfsAutoStartPaths { get; } = new Dictionary<Simulator, string[]>()
        {
            {
                Simulator.MSFS2020,
                new string[] {
                    $@"{Sys.FolderAppDataLocal()}\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\{MsfsAutoStartFile}",
                    $@"{Sys.FolderAppDataRoaming()}\Microsoft Flight Simulator\{MsfsAutoStartFile}"
                }
            },
            {
                Simulator.MSFS2024,
                new string[] {
                    $@"{Sys.FolderAppDataLocal()}\Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\{MsfsAutoStartFile}",
                    $@"{Sys.FolderAppDataRoaming()}\Microsoft Flight Simulator 2024\{MsfsAutoStartFile}",
                }
            },
        };

        public static bool IsRunning()
        {
            return Sys.GetProcessRunning(MsfsBinary2020) || Sys.GetProcessRunning(MsfsBinary2024);
        }

        public static bool CheckInstalledMsfs(Simulator simulator, out string[] packagePaths)
        {
            bool result = false;
            List<string> paths = new List<string>();
            try
            {
                foreach (var cfgPath in MsfsConfigPaths[simulator])
                {
                    if (File.Exists(cfgPath))
                    {
                        Logger.Debug($"cfgPath exists: {cfgPath}");
                        string packagePath = FindPackagePath(cfgPath);
                        Logger.Debug($"returned packagePath: {packagePath}");
                        if (!string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath))
                        {
                            Logger.Debug($"Path exists!");
                            paths.Add(packagePath);
                            result = true;
                        }
                        else
                            Logger.Debug($"Path does not exist!");
                    }
                    else
                        Logger.Debug($"cfgPath does not exist: {cfgPath}");
                }
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            packagePaths = paths.ToArray();
            return result;
        }

        public static bool CheckInstalledMsfs(Simulator simulator)
        {
            bool result = false;

            foreach (var cfgPath in MsfsConfigPaths[simulator])
            {
                if (File.Exists(cfgPath) || Directory.Exists(Path.GetDirectoryName(cfgPath)))
                    result = true;
            }

            return result;
        }

        public static string FindPackagePath(string confFile)
        {
            Logger.Debug($"Getting PackagePath from {confFile}");
            string[] lines = File.ReadAllLines(confFile);
            foreach (string line in lines)
            {
                if (line.StartsWith(MsfsStringPackagePath))
                {
                    Logger.Debug($"Found Match!");
                    return line.Replace("\"", "").Substring(MsfsStringPackagePath.Length) + "\\Community";
                }
            }

            return "";
        }

        public static bool CheckPackageVersion(string[] packagePaths, string packageName, string version)
        {
            foreach (string packagePath in packagePaths)
            {
                if (!CheckPackageVersion(packagePath, packageName, version))
                    return false;
            }

            return true;
        }

        public static bool CheckPackageVersion(string packagePath, string packageName, string version)
        {
            try
            {
                string file = $@"{packagePath}\{packageName}\{MsfsManifestFile}";
                if (File.Exists(file))
                {
                    string text = File.ReadAllText(file);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Logger.Debug($"Manifest empty!");
                        return false;
                    }
                    Logger.Debug($"Parsing '{file}' for Version '{version}'");
                    return JsonNode.Parse(text)?[MsfsStringPackageVersion]?.ToString() == version;
                }
                else
                    Logger.Debug($"The Package does not exist: {file}");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return false;
        }

        public static bool InstallPackageFromArchive(string[] packagePath, string archive, string moduleName, bool deleteOld = true, bool createFolder = false)
        {
            foreach (var path in packagePath)
            {
                if (!InstallPackageFromArchive(path, archive, moduleName, deleteOld, createFolder))
                    return false;
            }

            return true;
        }

        public static bool InstallPackageFromArchive(string packagePath, string archive, string moduleName, bool deleteOld = true, bool createFolder = false)
        {
            try
            {
                string modulePath = Path.Combine(packagePath, moduleName);
                string unblockDir = modulePath;
                if (Directory.Exists(modulePath) && deleteOld)
                {
                    Logger.Debug($"Deleting old Module at '{modulePath}'");
                    Directory.Delete(modulePath, true);
                }

                if (!createFolder)
                    modulePath = packagePath;
                Logger.Debug($"Extracting Archive '{archive}' to Directory '{modulePath}'");
                if (!FuncZip.ExtractZipFile(modulePath, archive, unblockDir))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return false;
        }

        public static bool AutoStartAddUpdate(Simulator sim, string binPath, string binFile, string appName)
        {
            int count = 0;
            foreach (var conf in MsfsAutoStartPaths[sim])
            {
                if (AutoStartAddUpdate(conf, binPath, binFile, appName))
                    count++;
            }

            return count > 0;
        }

        public static bool AutoStartAddUpdate(string confPath, string binPath, string binFile, string appName)
        {
            bool result = false;

            try
            {
                CheckAutoStartFile(confPath);
                if (!File.Exists(confPath))
                {
                    Logger.Information($"Auto-Start File not found: {confPath}");
                    return false;
                }
                Logger.Debug($"Searching EXE.xml for Binary '{binFile}': {confPath}");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(File.ReadAllText(confPath));

                bool found = false;
                XmlNode simbase = xmlDoc.ChildNodes[1];
                foreach (XmlNode outerNode in simbase.ChildNodes)
                {
                    if (outerNode.Name == "Launch.Addon" && outerNode.InnerText.Contains(binFile))
                    {
                        found = true;
                        Logger.Debug($"Updating Xml Node");
                        foreach (XmlNode innerNode in outerNode.ChildNodes)
                        {
                            if (innerNode.Name == "Disabled")
                                innerNode.InnerText = "False";
                            else if (innerNode.Name == "Path")
                                innerNode.InnerText = binPath;
                            else if (innerNode.Name == "CommandLine")
                                innerNode.InnerText = "";
                            else if (innerNode.Name == "ManualLoad")
                                innerNode.InnerText = "False";
                        }
                    }
                }

                if (!found)
                {
                    Logger.Debug($"Adding Xml Node");
                    XmlNode outerNode = xmlDoc.CreateElement("Launch.Addon");

                    XmlNode innerNode = xmlDoc.CreateElement("Disabled");
                    innerNode.InnerText = "False";
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("ManualLoad");
                    innerNode.InnerText = "False";
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("Name");
                    innerNode.InnerText = appName;
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("Path");
                    innerNode.InnerText = binPath;
                    outerNode.AppendChild(innerNode);

                    xmlDoc.ChildNodes[1].AppendChild(outerNode);
                }

                xmlDoc.Save(confPath);
                result = File.ReadAllText(confPath)?.Contains(binFile) == true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static void CheckAutoStartFile(string confPath)
        {
            string dir = Path.GetDirectoryName(confPath);
            if (!File.Exists(confPath) && Directory.Exists(dir))
            {
                Logger.Debug($"Creating empty Exe File '{confPath}'");
                string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SimBase.Document Type=""Launch"" version=""1,0"">
	<Descr>Launch</Descr>
	<Filename>EXE.xml</Filename>
	<Disabled>False</Disabled>
	<Launch.ManualLoad>False</Launch.ManualLoad>";
                File.WriteAllText(confPath, content, Encoding.UTF8);
            }
        }

        public static bool AutoStartRemove(Simulator sim, string binFile)
        {
            int count = 0;
            foreach (var conf in MsfsAutoStartPaths[sim])
            {
                if (AutoStartRemove(conf, binFile))
                    count++;
            }

            return count > 0;
        }

        public static bool AutoStartRemove(string confPath, string binFile)
        {
            bool result = false;

            try
            {
                if (!File.Exists(confPath))
                {
                    Logger.Debug($"Exe File '{confPath}' does not exist");
                    result = true;
                    return result;
                }
                Logger.Debug($"Searching EXE.xml for Binary '{binFile}': {confPath}");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(File.ReadAllText(confPath));

                XmlNode simbase = xmlDoc.ChildNodes[1];
                List<XmlNode> removeList = new List<XmlNode>();
                foreach (XmlNode outerNode in simbase.ChildNodes)
                {
                    if (outerNode.Name == "Launch.Addon" && outerNode.InnerText.Contains(binFile))
                        removeList.Add(outerNode);
                }

                Logger.Debug($"Found {removeList.Count} Entries to remove");
                if (removeList.Count > 0)
                {
                    foreach (XmlNode node in removeList)
                        xmlDoc.ChildNodes[1].RemoveChild(node);

                    xmlDoc.Save(confPath);
                }

                result = File.ReadAllText(confPath)?.Contains(binFile) == false;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }
    }
}
