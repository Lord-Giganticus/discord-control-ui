using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using LunarBind;

namespace ImGui.NET.SampleProgram
{
    public class LuaStuff
    {
        public static DiscordMember GetMember(DiscordGuild guild, ulong id)
        {
            return guild.GetMemberAsync(id).Result;
        }

        public static bool IsNull(object a)
        {
            return a == null;
        }
    }

    public class Convertion
    {
        public static float Float(string s)
        {
            if(float.TryParse(s,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var r))
            {
                return r;
            }
            return ulong.Parse("0");
        }
    }
}
