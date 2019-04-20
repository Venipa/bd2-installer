using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ionic.Zip;

namespace BDIv2.App
{
    public class Installer
    {
        public static Installer Create()
        {
            return new Installer();
        }
        private string CurrentPath { get => AppDomain.CurrentDomain.BaseDirectory; }
        private string DiscordPath { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"); }
        private string BetterDiscordPath { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterDiscord2"); }
        private string LatestAppPath { get; set; }
        private SimpleLogger log = new SimpleLogger();
        private string getLatestAppPath()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine("Error: " + (e.ExceptionObject as Exception).Message);
                log.Error((e.ExceptionObject as Exception).ToString());
            };
            var latestPath = Directory.GetDirectories(this.DiscordPath, "app-*", SearchOption.TopDirectoryOnly).ToList().OrderBy(x =>
            {
                Debug.WriteLine(String.Format("Checking {0}", x));
                var match = Regex.Match(@Path.GetDirectoryName(x).Split('-').Last(), @"\d+");
                int version;
                if (match.Success && int.TryParse(match.Value, out version))
                {
                }
                else
                {
                    return 0;
                }
                return version;
            }).Last();
            Debug.WriteLine(latestPath);
            return latestPath;
        }
        private string getIndexJs()
        {
            if (this.LatestAppPath == null || !Directory.Exists(this.LatestAppPath))
            {
                return null;
            }
            var pth = Path.Combine(this.LatestAppPath, "resources", "app", "index.js");
            Console.WriteLine($"index.js: {File.Exists(pth).ToString()}");
            return pth;

        }
        private string getPackageJson()
        {
            if (this.LatestAppPath == null || !Directory.Exists(this.LatestAppPath))
            {
                return null;
            }
            var pth = Path.Combine(this.LatestAppPath, "resources", "app", "package.json");
            Console.WriteLine($"package.json: {File.Exists(pth).ToString()}");
            return pth;

        }
        private string getAppRoot()
        {
            if (this.LatestAppPath == null || !Directory.Exists(this.LatestAppPath))
            {
                return null;
            }
            var pth = Path.Combine(this.LatestAppPath, "resources", "app");
            Console.WriteLine($"app/: {Directory.Exists(pth).ToString()}");
            return pth;
        }
        public Installer()
        {

            Console.WriteLine();
            Task.Run(() =>
            {
                this.prompt("This installer currently only supports installing on the Stable Version of Discord, do you want to install (discord will startup once done) ?").Wait();
                this._checkPath();
                try
                {
                    this.Inject();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}\nInstallation faulted", ex.Message);
                    log.Error(ex.ToString());
                }
            });

            
        }
        private void _checkPath()
        {
            var path = this.getLatestAppPath();
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Could not find latest discord app path...\nWaiting for Input...");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Latest Discord App Path: {0}", path);
            }
            var rapp = Path.Combine(path, "resources", "app");
            if (!Directory.Exists(rapp))
            {
                Directory.CreateDirectory(rapp);
            }
            this.LatestAppPath = path;
        }
        async Task Inject()
        {
            Console.WriteLine("Injecting...");
            File.WriteAllText(this.getPackageJson(), JsonConvert.SerializeObject(new PackageJsonModel()
            {
                Description = "BetterDiscord",
                Name = "betterdiscord",
                EntryFile = "index.js",
                isPrivate = true
            }));
            string appBd = BetterDiscordPath;
            try
            {
                Process.GetProcesses().ToList().Where(x =>
                {
                    try
                    {
                        return x.MainModule.FileName == Path.Combine(LatestAppPath, "Discord.exe");
                    } catch (Exception ex)
                    {
                        log.Error(ex.ToString());
                        return false;
                    }
                }).ToList().ForEach(x =>
                {
                    try
                    {
                        x.Kill();
                    } catch { }
                });
            } catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
            if (!Directory.Exists(appBd))
            {
                Directory.CreateDirectory(appBd);
            }
            ZipFile bd = ZipFile.Read(Path.Combine(CurrentPath, "core.zip"));
            bd.ExtractProgress += (s, e) =>
            {
                if (e.EntriesTotal > 0 && e.EntriesExtracted > 0)
                {
                    Console.WriteLine("{0} - {1,-15}: {2}", Path.GetFileName(e.ArchiveName), $"{e.EntriesExtracted} / {e.EntriesTotal}", e.CurrentEntry.FileName);
                }
            };
            bd.ZipError += (s, e) =>
            {
                Console.WriteLine($"Error Occured: {e.Exception.Message}");
            };
            bd.ExtractAll(appBd, ExtractExistingFileAction.OverwriteSilently);
            File.WriteAllText(this.getIndexJs(), File.ReadAllText(Path.Combine(appBd, "stub.js")));
            File.WriteAllText(Path.Combine(this.getAppRoot(), "bd.json"), JsonConvert.SerializeObject(new
            {
                options = new
                {
                    autoInject = true,
                    commonCore = true,
                    commonData = true
                },
                paths = new
                {
                    core = Path.Combine(appBd, "core"),
                    client = Path.Combine(appBd, "client"),
                    editor = Path.Combine(appBd, "core"),
                    data = Path.Combine(appBd, "data"),
                }
            }));
            this.success();

        }
        async Task prompt(string input, params object[] param)
        {
            Console.Write(input + " (Y/N): ", param);
            ConsoleKey currentKey;
            while ((currentKey = Console.ReadKey().Key) != ConsoleKey.Y)
            {
                if (currentKey == ConsoleKey.N)
                {
                    Environment.Exit(0);
                    break;
                }
            }
            Console.WriteLine();
        }
        async Task success()
        {
            Process.Start(new ProcessStartInfo { Arguments = $"/C \"{Path.Combine(LatestAppPath, "Discord.exe")}\"", FileName = "cmd", WindowStyle = ProcessWindowStyle.Hidden });
            Console.WriteLine("Successfully installed BetterDiscord\nPress any key to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}
