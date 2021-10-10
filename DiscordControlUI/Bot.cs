using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MoonSharp;
using MoonSharp.Interpreter;
using LunarBind;
using LunarBind.Runners;
using ImGuiNET;
using ImGui.NET.SampleProgram;

namespace ImGui.NET.SampleProgram
{
    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }

        public List<HookedScriptRunner> commands = new List<HookedScriptRunner>();

        HookedScriptRunner events;

        string botPath;

        void RefreshCommands()
        {
            commands.Clear();

            foreach (var f in Directory.GetFiles($"{botPath}/commands/"))
            {
                if (!f.ToLower().EndsWith(".lua")) continue;

                Console.WriteLine($"Found {Path.GetFileName(f)}");

                var txt = File.ReadAllText(f);
              
                txt += $"\nRegisterHook(Execute,'Execute')";
                txt += $"\nRegisterHook(CommandName,'CommandName')";
                txt += $"\nRegisterHook(CommandDescription,'CommandDescription')";

                
                var lua = new HookedScriptRunner();

                lua.LoadScript(txt);

                commands.Add(lua);
            }
        }

        void RefreshEvents()
        {
            Console.WriteLine("Creating LUA instance.");
            var _event = new HookedScriptRunner();

            Console.WriteLine("Reading File");
            var _txt2 = File.ReadAllText($"{botPath}/events.lua");

            Console.WriteLine("Hooking functions");
            _txt2 += $"\nRegisterHook(OnMessage,'OnMessage')";
            _txt2 += $"\nRegisterHook(OnReady,'OnReady')";

            Console.WriteLine("Loading Script");
            _event.LoadScript(_txt2);

            Console.WriteLine("Loaded EVENTS.LUA script.");

            events = _event;
        }

        public Bot(string botPath)
        {
            this.botPath = botPath;
            Console.WriteLine("Getting all commands.");
            RefreshCommands();
            Console.WriteLine("Loading Events");
            RefreshEvents();

            Console.WriteLine("ENDED");
        }

        public async Task Run()
        {
            var configuration = new DiscordConfiguration
            {
                Token = Cache.botToken,
                TokenType = TokenType.Bot
            };

            Client = new DiscordClient(configuration);

            Client.Ready += OnClientReady;
            Client.MessageCreated += OnMessage;

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task OnMessage(DiscordClient c, MessageCreateEventArgs e)
        {
            if (!Directory.Exists($"{Cache.botPath}/guilds/{e.Message.Channel.Guild.Id}/"))
                Util.Copy(Util.Dir("Base/BaseGuild"), $"{Cache.botPath}/guilds/{e.Message.Channel.Guild.Id}/");

            //event first, then command handling
            events.Execute("OnMessage", new object[] { c, e });

            if (e.Message.Content.StartsWith(Cache.botPrefix))
            {
                var args = e.Message.Content.Split().ToList();

                var cmdName = args[0].Remove(0, Cache.botPrefix.Length);

                args.RemoveAt(0);

                
                foreach (var command in commands)
                {
                    if (command.Execute("CommandName").String.ToLower() == cmdName.ToLower())
                    {
                        var context = new Context
                        {
                            channel = e.Message.Channel,
                            guild = e.Message.Channel.Guild,
                            user = e.Message.Author,
                            member = e.Guild.Members[e.Message.Author.Id],
                            guildCfg = new IniFile($"{Cache.botPath}/guilds/{e.Message.Channel.Guild.Id}/main.ini")
                        };

                        Program.AddLog($"User {e.Message.Author.Username} issued command {command.Execute("CommandName").String.ToUpper()}");

                        command.Execute("Execute", new object[] { context, args.ToArray() });
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task OnClientReady(DiscordClient c, ReadyEventArgs e)
        {
            Console.WriteLine("Started Bot.");
            Program.AddLog("Started Bot");

            Program.status = BotStatus.STARTED;

            return Task.CompletedTask;
        }
    }

    public class Context
    {
        public DiscordUser user;
        public DiscordMember member;
        public DiscordGuild guild;
        public DiscordChannel channel;

        public IniFile guildCfg;

        public void SendMessage(string content)
        {
            channel.SendMessageAsync(content);
        }
    }
}
