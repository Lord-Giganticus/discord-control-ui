using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using LunarBind;
using LunarBind.Runners;

namespace ImGui.NET.SampleProgram
{
    public static class BotConsole
    {
        public static void Log(string s)
        {
            Program.AddLog(s);
        }
    }
}
