using System;
using DSharpPlus;

namespace MovieNightBot
{
    static class Program
    {
        public static Bot bot;
        public static ulong[] verifiedUsers;

        public static Vote lastVote = null;

        static void Main(string[] args)
        {
            verifiedUsers = new ulong[] {
                188574182504792064, //Modkipod
                365864838397689876 //Spacie
            };
            
            IMDB.SetUp();
            bot = new Bot();
            bot.Run("NzQ1ODA1MDUyNDAwMjM4NjAz.Xz3HFA.CwXFiBx10cMMgYInHIqcHnjp_7Y").GetAwaiter().GetResult();
        }
    }
}

