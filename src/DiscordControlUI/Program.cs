using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ImGui.NET.SampleProgram;
using ImPlotNET;
using LunarBind;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using static ImGuiNET.ImGuiNative;

namespace ImGuiNET
{
    public static class LuaRandom
    {
        static Random random;

        public static int Range(int min = 0, int max = 10)
        {
            return random.Next(min, max);
        }

        public static void Start()
        {
            random = new Random((int)DateTime.Now.Ticks);
        }
    }

    class Program
    {
        #region Some Stuff that initializes the ui and shit
        [DllImport("User32.dll")]
        public static extern ushort GetAsyncKeyState(int ArrowKeys);

        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;

        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);

        static bool pressed;

        static bool Pressed(int key)
        {
            return GetAsyncKeyState(key) > 0;
        }

        static void Main(string[] args)
        {
            Refresh();
            LuaRandom.Start();

            //Get all Types to add in Lua
            //Using MoonSharp and LunarBind
            Assembly mscorlib = typeof(DiscordUser).Assembly;
            foreach (Type type in mscorlib.GetTypes())
            {
                if (type.FullName.Contains("<>")) continue;
                GlobalScriptBindings.AddGlobalType(type);
            }

            Assembly mscorlib2 = typeof(IniFile).Assembly;
            foreach (Type type in mscorlib2.GetTypes())
            {
                if (type.FullName.Contains("<") || type.FullName.Contains(">")) continue;
                GlobalScriptBindings.AddGlobalType(type);
            }

            GlobalScriptBindings.AddGlobalType(typeof(Task));
            GlobalScriptBindings.AddGlobalType(typeof(Task<>));

            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Discord Control UI (v1.0.0)"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
            Random random = new Random();

            // Main application loop

            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        static Bot bot = null;
        static IniFile botCfg = null;

        public static BotStatus status;

        static List<Project> projects = new List<Project>();

        static void StartBot()
        {
            status = BotStatus.OFF;

            status = BotStatus.STARTING;

            Console.WriteLine("Creating Bot Instance...");

            bot = new Bot(Cache.botPath);


            Console.WriteLine("Starting Bot");

            Task.Factory.StartNew(() => bot.Run().GetAwaiter().GetResult());
        }

        public static Screen curScreen;

        static bool _showCreateTxbx = false;
        static string prjName = "";

        static List<string> logs = new List<string>();

        #endregion

        #region Creating and loading projects

        static void Refresh()
        {
            System.Console.WriteLine("Refreshing Projects");
            projects.Clear();

            foreach (var proj in Directory.GetDirectories(Util.Dir("Projects")))
            {
                projects.Add(new Project(proj));
            }
        }

        static void CreateProject(string projectName)
        {
            if (projectName.StartsWith("--"))
                return;

            if(Directory.Exists(Util.Dir($"Projects/{projectName}")))
            {
                prjName = "--THAT PROJECT ALREADY EXISTS";
            }
            else
            {
                Util.Copy(Util.Dir($"Base/BaseProject"), Util.Dir($"Projects/{projectName}"));
                LoadProject(Util.Dir($"Projects/{projectName}"));
            }
        }

        static void LoadProject(string projectPath)
        {
            Cache.botPath = projectPath;
            botCfg = new IniFile($"{projectPath}/settings.ini");

            //load stuff

            Cache.botToken = botCfg.Read("token","Bot","TOKEN HERE");
            Cache.botPrefix = botCfg.Read("prefix","Bot","!");
            foreach(var act in botCfg.Read("activities", "Bot", "!help for help").Split(';').ToList())
            {
                Cache.botActivities.Add(new ActivityUIStuff { activity = act });
            }
            curStatusId = int.Parse(botCfg.Read("status_id","Bot","0"));
            curActivityId = int.Parse(botCfg.Read("activity_id","Bot","0"));

            RefreshFileList();

            curScreen = Screen.MAIN;
        }

        #endregion

        #region UI Stuff

        static float delta = 0f;

        public static void AddLog(string s)
        {
            logs.Add($"[{DateTime.Now.ToShortTimeString()}] {s}");
        }

        static void ShowCreateTextBox()
        {
            if (!_showCreateTxbx)
                return;

            ImGui.SetNextWindowPos(new Vector2(433, 8));
            ImGui.SetNextWindowSize(new Vector2(314, 103));
            ImGui.Begin("CREATE PROJECT");

            ImGui.Text("Enter Project Name");
            ImGui.InputText("Project Name", ref prjName, 256);

            if(ImGui.Button("CREATE",new Vector2(100, 20)))
            {
                CreateProject(prjName);
            }

            ImGui.End();
        }

        

        static unsafe void SelectProjectUI()
        {
            var IO = ImGui.GetIO();

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(500 + 2, IO.DisplaySize.Y));
            ImGui.Begin("Select Project");

            ImGui.Text("Create a new Project or select an existing one.");

            if(ImGui.Button("CREATE", new Vector2(IO.DisplaySize.X / 6, 50)))
            {
                _showCreateTxbx = true;
            }
            ImGui.SameLine();
            if(ImGui.Button("REFRESH", new Vector2(IO.DisplaySize.X / 6, 50)))
            {
                Refresh();
            }

            ImGui.Text("");
            ImGui.Text("All Available Projects:");

            foreach(var project in projects)
            {
                if(ImGui.Button(project.name, new Vector2(300, 20)))
                {
                    LoadProject(project.path);
                }
                ImGui.SameLine();
                if (ImGui.Button("DELETE", new Vector2(170, 20)))
                {
                    Directory.Delete(project.path,true);
                    Refresh();
                }
            }

            ImGui.End();

            ShowCreateTextBox();
        }

        static int curStatusId = 0;
        static int curActivityId = 0;

        static string[] statuses = new string[] { "Online", "Idle", "Do not Disturb", "Invisible" };

        static void UpdateActivity()
        {
            if(bot != null && status == BotStatus.STARTED && Cache.botActivities.Count > 0 && curActivityId >= 0 && curActivityId <= Cache.botActivities.Count-1)
            {
                var activity = new DiscordActivity { ActivityType = ActivityType.Playing, Name = Cache.botActivities[curActivityId].activity };

                UserStatus status = UserStatus.Online;

                switch (statuses[curStatusId])
                {
                    case "Online":
                        status = UserStatus.Online;
                        break;
                    case "Idle":
                        status = UserStatus.Idle;
                        break;
                    case "Do not Disturb":
                        status = UserStatus.DoNotDisturb;
                        break;
                    case "Invisible":
                        status = UserStatus.Invisible;
                        break;
                    default:
                        status = UserStatus.Online;
                        break;
                }
                bot.Client.UpdateStatusAsync(activity,status);
            }
        }
        
        static void DrawSettings()
        {
            if(status == BotStatus.STARTED)
            {
                var io = ImGui.GetIO();

                delta += io.DeltaTime;

                if (delta > 4.5f)
                {
                    delta = 0;

                    if (curActivityId < Cache.botActivities.Count - 1)
                    {
                        curActivityId++;
                    }
                    else
                    {
                        curActivityId = 0;
                    }

                    UpdateActivity();
                }
            }

            ImGui.SetNextWindowPos(new Vector2(505, 4));
            ImGui.SetNextWindowSize(new Vector2(773, 309));
            ImGui.Begin("Settings");

            //ImGui.PushFont(fonts["default"]);

            ImGui.InputText("Bot Token",ref Cache.botToken,5000);
            ImGui.InputText("Bot Prefix",ref Cache.botPrefix,24);

            ImGui.Combo("Status", ref curStatusId,statuses,4);

            ImGui.Text("");

            //Display Activities
            ImGui.Text("Activities: ");
            if (ImGui.Button("ADD", new Vector2(100, 30))) {
                Cache.botActivities.Add(new ActivityUIStuff { activity = "New Activity" });
            }

            for(int i = 0; i < Cache.botActivities.Count; i++)
            {
                ImGui.InputText($"###ActivityStuffID{i}",ref Cache.botActivities[i].activity,256);
                ImGui.SameLine();
                if (ImGui.Button("REMOVE"))
                {
                    Cache.botActivities.RemoveAt(i);
                }
            }

            if(curActivityId >= 0)
            {
                string act = Cache.botActivities.Count > 0 && curActivityId <= Cache.botActivities.Count - 1 ? Cache.botActivities[curActivityId].activity : "No Activity";

                ImGui.InputInt($"###ActivityIdStuff", ref curActivityId);
                ImGui.Text($"Starting Activity Id ({act})");
            }
            else
            {
                ImGui.InputInt($"###ActivityIdStuff", ref curActivityId);
                ImGui.Text($"Starting Activity Id (No Activity)");
            }
            

            if (ImGui.Button("APPLY"))
            {
                botCfg.Write("token", Cache.botToken,"Bot");
                botCfg.Write("prefix", Cache.botPrefix, "Bot");
                botCfg.Write("activity_id", curActivityId.ToString(), "Bot");
                string statusStuff = "";

                for (int i = 0; i < Cache.botActivities.Count; i++)
                {
                    statusStuff += i == Cache.botActivities.Count - 1 ? Cache.botActivities[i].activity : $"{Cache.botActivities[i].activity};";
                }

                botCfg.Write("activities",statusStuff,"Bot");
                botCfg.Write("status_id",curStatusId.ToString(),"Bot");
            }

            ImGui.End();
        }

        static string createCommandName = "";
        static string curFilePath = "--";
        public static string curText = "AAAAAAAAAAAA";

        static void DrawCodeEditor()
        {
            ImGui.SetNextWindowPos(new Vector2(506, 315));
            ImGui.SetNextWindowSize(new Vector2(621, 402));
            ImGui.Begin("Code Editor");
            //ImGui.PushFont(fonts["default"]);

            ImGui.InputText("Command Name", ref createCommandName, 64);
            ImGui.SameLine();
            if(ImGui.Button("Create",new Vector2(70, 20)))
            {
                File.Copy(Util.Dir("Base/BaseCommand.lua"),$"{Cache.botPath}/commands/{createCommandName}.lua");
                RefreshFileList();
            }

            //ImGui.PushFont(fonts["text_editor"]);

            ImGui.InputTextMultiline("Text Editor", ref curText, 6500000, new Vector2(621, 340), ImGuiInputTextFlags.AllowTabInput);

            //Control
            if (Pressed(0xA2))
            {
                //S
                if (Pressed(0x53))
                {
                    if (!pressed)
                    {
                        if (!File.Exists(curFilePath))
                        {
                            AddLog($"Can't Save file {curFilePath} file doesn't exist");
                        }
                        else
                        {
                            File.WriteAllText(curFilePath, curText);
                            AddLog("Saved");
                        }
                        pressed = true;
                    }
                }
                else
                {
                    pressed = false;
                }
            }
            else
            {
                pressed = false;
            }

            ImGui.End();
        }

        static List<FileItem> fileItems = new List<FileItem>();

        static void RefreshFileList()
        {
            fileItems.Clear();

            fileItems.Add(new FileItem($"{Cache.botPath}/events.lua"));

            foreach(var f in Directory.GetFiles($"{Cache.botPath}/commands/"))
            {
                if (!f.ToLower().EndsWith(".lua")) continue;
                fileItems.Add(new FileItem(f));
            }
        }

        static void DrawFileList()
        {
            ImGui.SetNextWindowPos(new Vector2(1127, 315));
            ImGui.SetNextWindowSize(new Vector2(151, 402));
            ImGui.Begin("File List");

            if (ImGui.Button("Refresh"))
            {
                RefreshFileList();
            }

            ImGui.Text("__________");

            foreach(var f in fileItems)
            {
                if (ImGui.Button(f.name,new Vector2(100,20)))
                {
                    curText = File.ReadAllText(f.path);
                    curFilePath = f.path;
                }
            }

            ImGui.End();
        }

        static unsafe void MainUI()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 711));
            ImGui.SetNextWindowPos(new Vector2(4, 4));
            ImGui.Begin("Main");

            //ImGui.PushFont(fonts["default"]);

            //Bot Status Button.
            {
                if (status == BotStatus.OFF)
                {
                    if(ImGui.Button("Start Bot", new Vector2(150, 50)))
                    {
                        StartBot();
                    }
                }
                else if (status == BotStatus.STARTING)
                {
                    ImGui.Button("Starting...", new Vector2(150, 50));
                }
                else
                {
                    if(ImGui.Button("Stop Bot", new Vector2(150, 50)))
                    {
                        bot.Client.DisconnectAsync();
                        status = BotStatus.OFF;
                        AddLog("Stopped Bot.");
                    }
                }
            }

            ImGui.Text("Console:");
            ImGui.Text("___________________________________________________________________________________________________");
            //Bot Console
            {
                ImGui.BeginGroup();

                foreach(var l in logs)
                {
                    ImGui.Text(l);
                }

                ImGui.EndGroup();
            }

            ImGui.End();

            DrawSettings();
            DrawCodeEditor();
            DrawFileList();
        }

        //Main UI
        private static unsafe void SubmitUI()
        {
            if(curScreen == Screen.SELECT_PROJECT)
            {
                SelectProjectUI();
            }
            else
            {
                MainUI();
            }
        }

        #endregion
    }

    enum BotStatus
    {
        OFF,STARTING,STARTED
    }

    enum Screen
    {
        SELECT_PROJECT, MAIN
    }

    class Project
    {
        public string name, path;

        public Project(string path)
        {
            name = Path.GetFileName(path);
            this.path = path;
        }
    }

    class FileItem
    {
        public string path, name;

        public FileItem(string path)
        {
            name = Path.GetFileName(path);
            this.path = path;
        }
    }
}
