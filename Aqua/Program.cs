using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Commands;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Aqua
{
    public class Program
    {
        public CommandService commands;
        private DiscordSocketClient client;
        public static Color embedColor;
        public static Dictionary<ulong, Config> cfg;
        public IServiceProvider provider;

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.Title = "Aqua";

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Settings"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"\Settings");
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt"))
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt");

            var readCfg = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt");
            if (string.IsNullOrEmpty(readCfg))
            {
                cfg = new Dictionary<ulong, Config>();
            }
            else
                cfg = JsonConvert.DeserializeObject<Dictionary<ulong, Config>>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt"));

            embedColor = new Color(179, 17, 255);

            client = new DiscordSocketClient(new DiscordSocketConfig() { MessageCacheSize = 100 });
            commands = new CommandService();

            client.Log += Log;
            commands.Log += Log;

            var services = new ServiceCollection()
                 .AddSingleton(client)
                 .AddSingleton(commands);
                 //.AddSingleton(new AudioService());

            provider = services.BuildServiceProvider();

            client.MessageReceived += HandleCommand;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
            client.ReactionsCleared += ReactionsCleared;
            client.Ready += Ready;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            string token = "NDEwNDQ1NDI1OTU1NTY5NjY0.DbTRKQ.vRnDlH3WDi9dQC7OFXBcBj_F3H8";
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.FromResult<object>(null);
        }

        private async Task Ready()
        {
            await client.SetGameAsync("a.");
        }

        public async Task HandleCommand(SocketMessage m)
        {
            var message = m as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;

            var context = new CommandContext(client, message);

            if (!(message.HasStringPrefix("a.", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            var result = await commands.ExecuteAsync(context, argPos, provider);
            if (!result.IsSuccess)
                Console.WriteLine(result.ErrorReason);
        }

        #region ReactionAdded
        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel ch, SocketReaction reaction)
        {
            var msg = await cachedMessage.GetOrDownloadAsync();
            var context = new CommandContext(client, msg);

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            if (reaction.Emote.Name == "⭐")
            {
                if (!cfg[context.Guild.Id].Stars.Keys.Contains(msg.Id))
                {
                    //New starred message. Add it to the #starboard
                    EmbedBuilder em = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = msg.Author.GetAvatarUrl(),
                            Name = msg.Author.Username
                        },
                        Description = msg.Content,
                        Timestamp=msg.Timestamp,
                        Color = new Color(255, 225, 61)
                    };

                    string text = $"⭐ 1 <#{ch.Id}> ({msg.Id})";
                    var starboard = await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID);
                    var starref = await (starboard as IMessageChannel).SendMessageAsync(text, false, em.Build());

                    cfg[context.Guild.Id].Stars.Add(msg.Id, 1);
                    cfg[context.Guild.Id].StarRef.Add(msg.Id, starref.Id);

                    Config.Save(cfg);

                }
                else
                {
                    //Old starred message. Add one upvote/star to it
                    cfg[context.Guild.Id].Stars[msg.Id]++;

                    var starref = await ((await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID)) as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await (starref as IUserMessage).ModifyAsync(x => x.Content = $"⭐ {cfg[context.Guild.Id].Stars[msg.Id].ToString()} <#{ch.Id}> ({msg.Id})");

                    Config.Save(cfg);
                }
            }
        }
        #endregion

        #region ReactionRemoved
        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel ch, SocketReaction reaction)
        {
            var msg = await cachedMessage.GetOrDownloadAsync();
            var context = new CommandContext(client, msg);

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            if (reaction.Emote.Name == "⭐")
            {
                if (cfg[context.Guild.Id].Stars[msg.Id] == 1)
                {
                    //Remove from the starboard
                    var starref = await ((await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID)) as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await starref.DeleteAsync();

                    cfg[context.Guild.Id].StarRef.Remove(msg.Id);
                    cfg[context.Guild.Id].Stars.Remove(msg.Id);

                    Config.Save(cfg);
                }
                else if (cfg[context.Guild.Id].Stars[msg.Id] > 1)
                {
                    //Update amount
                    cfg[context.Guild.Id].Stars[msg.Id]--;

                    var starref = await ((await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID)) as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await (starref as IUserMessage).ModifyAsync(x => x.Content = $"⭐ {cfg[context.Guild.Id].Stars[msg.Id].ToString()} <#{ch.Id}> ({msg.Id})");
                }
            }
        }
        #endregion

        #region ReactionsCleared
        private async Task ReactionsCleared(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel ch)
        {
            var msg = await cachedMessage.GetOrDownloadAsync();
            var context = new CommandContext(client, msg);

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            var starref = await ((await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID)) as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
            await starref.DeleteAsync();

            cfg[context.Guild.Id].StarRef.Remove(msg.Id);
            cfg[context.Guild.Id].Stars.Remove(msg.Id);

            Config.Save(cfg);
        }
        #endregion
    }
}
