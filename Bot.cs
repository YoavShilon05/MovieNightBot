using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;


namespace MovieNightBot
{
    class Bot
    {
        public DiscordClient client { get; private set; }
        public CommandsNextExtension commands { get; private set; }
        public InteractivityExtension interactivity { get; private set; }

        public async Task Run(string token)
        {

            var botConfig = new DiscordConfiguration
            {
                Token = token,
            };

            client = new DiscordClient(botConfig);

            var CommandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes =  new string[] {"mv."}
            };

            commands = client.UseCommandsNext(CommandsConfig);

            interactivity = client.UseInteractivity(new InteractivityConfiguration() { Timeout= Timeout.InfiniteTimeSpan});

            await client.ConnectAsync();

            //register commands
            commands.RegisterCommands<Commands>();

            //register events
            client.Ready += Events.OnClientReady;

            //Extend bot duration Indefinitely.
            await Task.Delay(-1);

        }
    }
}
