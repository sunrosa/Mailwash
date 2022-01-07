﻿// Warning: This bot is designed to be private, used only within one guild per runtime instance.
namespace Leirosa
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        public static Discord.WebSocket.DiscordSocketClient Client {get; set;}
        public static Discord.Commands.CommandService Commands {get; set;}
        public static Dictionary<string, string> Config {get; set;}

        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public async Task MainAsync()
        {
            {
                var config = new NLog.Config.LoggingConfiguration();
                var logfile = new NLog.Targets.FileTarget("logfile"){FileName="log.log"};
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

                config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);

                NLog.LogManager.Configuration = config;
            }

            _log.Info("Setup logger. Beginning of MainAsync().");

            #if DEBUG
            _log.Info("Running in DEBUG.");
            #elif RELEASE
            _log.Info("Running in RELEASE.");
            #endif

            _log.Debug("Creating client...");
            Client = new Discord.WebSocket.DiscordSocketClient(new Discord.WebSocket.DiscordSocketConfig(){
                LogLevel = Discord.LogSeverity.Debug,
                AlwaysDownloadUsers = true,
                GatewayIntents = Discord.GatewayIntents.All, // VERY IMPORTANT. SHIT DOESN'T WORK WITHOUT THIS. The Discord API basically neglects to send us shit (most notably SocketGuildUsers), unless we have all intents SPECIFIED in our config.
                LogGatewayIntentWarnings = false
            });
            Client.Log += Log;

            _log.Debug("Parsing workspace config...");
            Config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("config.json"));

            var token = Config["token"];

            _log.Debug("Logging in...");
            await Client.LoginAsync(Discord.TokenType.Bot, token);
            await Client.StartAsync();

            Client.Ready += Ready; // Call Ready() when the client is ready.

            await Task.Delay(-1); // Block the thread to prevent the program from closing (infinite wait)
        }

        private async Task Ready()
        {
            _log.Debug("Client ready. Initializing CommandService...");
            Commands = new Discord.Commands.CommandService();
            _log.Debug("CommandService ready. Initializing CommandHandler...");
            var command_handler = new CommandHandler(Client, Commands);
            _log.Debug("Installing commands...");
            await command_handler.InstallCommandsAsync();

            if (bool.Parse(Config["use_custom_status"]))
            {
                _log.Debug("Setting custom status...");
                await Client.SetActivityAsync(new Discord.Game(Config["status"])); // Apparently you can't set custom statuses for bots, so this is the best we can do.
            }
        }

        private Task Log(Discord.LogMessage msg)
        {
            switch (msg.Severity)
            {
                case Discord.LogSeverity.Debug:
                case Discord.LogSeverity.Verbose:
                    _log.Debug(msg.ToString());
                break;
                case Discord.LogSeverity.Info:
                    _log.Info(msg.ToString());
                break;
                case Discord.LogSeverity.Warning:
                    _log.Warn(msg.ToString());
                break;
                case Discord.LogSeverity.Error:
                    _log.Error(msg.ToString());
                break;
                case Discord.LogSeverity.Critical:
                    _log.Fatal(msg.ToString());
                break;
                default:
                    _log.Info(msg.ToString());
                break;
            }

            return Task.CompletedTask;
        }
    }
}
