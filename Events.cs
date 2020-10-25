using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;

namespace MovieNightBot
{
    static class Events
    {
        public async static Task OnClientReady(object sender, ReadyEventArgs e)
        {
            Console.WriteLine("bot is ready");
        }
    }
}
