﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImGui.NET.SampleProgram
{
    public static class Cache
    {
        public static string botToken = "";
        public static string botPrefix = "";
        public static string botPath = "";
        public static List<ActivityUIStuff> botActivities = new List<ActivityUIStuff>();
    }

    public class ActivityUIStuff
    {
        public string activity;
    }
}
