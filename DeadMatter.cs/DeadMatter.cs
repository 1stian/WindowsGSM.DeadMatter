using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;

namespace WindowsGSM.Plugins
{
    public class DeadMatter : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.DeadMatter", // WindowsGSM.XXXX
            author = "1stian",
            description = "WindowsGSM plugin that adds support for Dead Matter Dedicated server.",
            version = "1.0",
            url = "https://github.com/1stian/WindowsGSM.DeadMatter", // Github repository link (Best practice)
            color = "#e84646" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => false;
        public override string AppId => "1110990"; // Game server appId, Dead Matter is 1110990

        // - Standard Constructor and properties
        public DeadMatter(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => "deadmatterServer.exe"; // Game server start path
        public string FullName = "Deadmatter"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new UT3(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "7777"; // Default port
        public string QueryPort = "27016"; // Default query port
        public string Defaultmap = "dead"; // Default map name
        public string Maxplayers = "36"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            // Creating config path
            string configDir = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), @"deadmatter\Saved\Config\WindowsServer");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Ini files
            string gameINI = Path.Combine(configDir, "Game.ini");
            string engineINI = Path.Combine(configDir, "Engine.ini");

            // Game ini
            var sbGame = new StringBuilder();
            sbGame.AppendLine("[/Script/DeadMatter.SurvivalBaseGameState]");
            sbGame.AppendLine($"ServerName={_serverData.ServerName}");
            sbGame.AppendLine("+SuperAdmins=<your steam id>");
            sbGame.AppendLine(@"ServerTags=""Key: Pair""");
            sbGame.AppendLine(@"ServerTags=""PvP: Yes""");
            sbGame.AppendLine(@"ServerTags=""RP: Roleplaying""");
            sbGame.AppendLine("MOTD=Welcome");
            sbGame.AppendLine("[/Script/Engine.GameSession]");
            sbGame.AppendLine($"MaxPlayers={_serverData.ServerMaxPlayer}");

            // Port related - Game ini
            sbGame.AppendLine("[/Script/deadmatter.ServerInfoProxy]");
            sbGame.AppendLine($"SteamQueryIP={_serverData.ServerIP}");
            sbGame.AppendLine($"SteamQueryPort={_serverData.ServerQueryPort}");
            File.WriteAllText(gameINI, sbGame.ToString());

            // Engine ini
            var sbEng = new StringBuilder();
            sbEng.AppendLine("[OnlineSubsystemSteam]");
            sbEng.AppendLine($"GameServerQueryPort={_serverData.ServerQueryPort}");
            sbEng.AppendLine("[URL]");
            sbEng.AppendLine($"Port={_serverData.ServerPort}");
            File.WriteAllText(engineINI, sbEng.ToString());
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string runPath = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), "deadmatter\\Binaries\\Win64\\deadmatterServer-Win64-Shipping.exe");

            string param = "-log";
            param += $" {_serverData.ServerParam}";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = runPath,
                    Arguments = param.ToString()
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p) => await Task.Run(() => { p.Kill(); });
    }
}
