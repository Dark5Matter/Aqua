﻿using System;
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

            /*if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Settings"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Settings");
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Settings\cfg.txt"))
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"Settings\cfg.txt");

            var readCfg = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt");
            if (string.IsNullOrEmpty(readCfg))
            {
                cfg = new Dictionary<ulong, Config>();
            }
            else
                cfg = JsonConvert.DeserializeObject<Dictionary<ulong, Config>>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt"));*/
            //cfg = new Dictionary<ulong, Config>();
            var readCfg = Properties.Settings.Default._config;
            if (string.IsNullOrEmpty(readCfg))
                cfg = new Dictionary<ulong, Config>();
            else
                cfg = JsonConvert.DeserializeObject<Dictionary<ulong, Config>>(readCfg);

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

            client.UserUpdated += UserUpdated;
            //client.GuildMemberUpdated += GuildMemberUpdated;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            string token = Properties.Settings.Default._key;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            if (arg1.Status == arg2.Status)
                return;
            var tracker = client.GetGuild(307628394684612610).GetChannel(446732405504606208) as IMessageChannel;
            await tracker.SendMessageAsync(string.Empty, embed:
                new EmbedBuilder()
                {
                    Color = embedColor,
                    Title = "Status Update",
                    Description = string.Format("{0}\nWent from being {1} to {2}.", $"{arg1.Username}#{arg1.Discriminator}", arg1.Status.ToString(), arg2.Status.ToString()),
                    Timestamp = DateTime.Now
                }.Build());
        }

        private async Task UserUpdated(SocketUser arg1, SocketUser arg2)
        {
            var tracker = client.GetGuild(307628394684612610).GetChannel(446732405504606208) as IMessageChannel;
            await tracker.SendMessageAsync(string.Empty, embed:
                new EmbedBuilder()
                {
                    Color = embedColor,
                    Title = "User Update",
                    Description = string.Format("Username: {0} -> {1}", $"{arg1.Username}#{arg1.Discriminator}", $"{arg2.Username}#{arg2.Discriminator}"),
                    ImageUrl = arg2.GetAvatarUrl(),
                    Timestamp = DateTime.Now
                }.WithAuthor(arg1).Build());
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.FromResult<object>(null);
        }

        private async Task Ready()
        {
            await client.SetGameAsync("a.help");
            await (client.GetGuild(307628394684612610).GetChannel(435065798730711051) as IMessageChannel).SendMessageAsync("Deployed! " + DateTime.Now.ToLongTimeString());
        }

        public async Task HandleCommand(SocketMessage m)
        {
            var message = m as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;

            var context = new CommandContext(client, message);
            
            if (message.Channel is IDMChannel && message.Channel.Id != (await client.GetUser(210150851606609921).GetOrCreateDMChannelAsync()).Id)
            { await (await client.GetUser(210150851606609921).GetOrCreateDMChannelAsync()).SendMessageAsync($"Message from: {message.Author.Mention} ({message.Author.Username}#{message.Author.Discriminator} {message.Author.Id})\n\n{message.Content}");
                return; }

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
            
            var starboard = await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID);

            try
            {
                if (ch.Id == starboard.Id || reaction.User.Value.Id == msg.Author.Id)
                    return;
            }
            catch { }

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            if (reaction.Emote.Name == "⭐")
            {
                if (!cfg[context.Guild.Id].Stars.Keys.Contains(msg.Id))
                {
                    //New starred message. Add it to the #starboard
                    EmbedBuilder em = BuildStar(msg);

                    string text = $"⭐ 1 <#{ch.Id}> ({msg.Id})";
                    var starref = await (starboard as IMessageChannel).SendMessageAsync(text, false, em.Build());

                    cfg[context.Guild.Id].Stars.Add(msg.Id, 1);
                    cfg[context.Guild.Id].StarRef.Add(msg.Id, starref.Id);

                    Config.Save(cfg);

                }
                else
                {
                    //Old starred message. Add one upvote/star to it
                    cfg[context.Guild.Id].Stars[msg.Id]++;

                    var starref = await (starboard as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await (starref as IUserMessage).ModifyAsync(x => x.Content = $"⭐ {cfg[context.Guild.Id].Stars[msg.Id].ToString()} <#{ch.Id}> ({msg.Id})");

                    Config.Save(cfg);
                }
            }
        }
        #endregion
        #region BuildStar
        private EmbedBuilder BuildStar(IMessage message)
        {
            var eb = new EmbedBuilder()
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .WithColor(new Color(255, 255, 61))
                .WithTimestamp(message.Timestamp);

            var attachment = message.Attachments.FirstOrDefault();
            var embed = message.Embeds.FirstOrDefault();

            if (attachment != null && attachment.Width.HasValue && attachment.Width.Value != 0)
                eb.WithImageUrl(attachment.Url);

            else if (embed != null)
            {
                if (embed.Image.HasValue)
                    eb.WithImageUrl(embed.Image.Value.Url);

                else if (!string.IsNullOrWhiteSpace(embed.Url)/* && Globals.ImageFormats.Any(embed.Url.EndsWith)*/)
                    eb.WithImageUrl(embed.Url);

                else if (embed.Thumbnail.HasValue)
                    eb.WithImageUrl(embed.Thumbnail.Value.Url);
            }

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(eb.Description) && string.IsNullOrWhiteSpace(eb.ImageUrl))
                return null;

            return eb;
        }
        #endregion

        #region ReactionRemoved
        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel ch, SocketReaction reaction)
        {
            var msg = await cachedMessage.GetOrDownloadAsync();

            var context = new CommandContext(client, msg);

            var starboard = await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID);

            try
            {
                if (ch.Id == starboard.Id || reaction.User.Value.Id == msg.Author.Id)
                    return;
            }
            catch { }

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            if (reaction.Emote.Name == "⭐")
            {
                if (cfg[context.Guild.Id].Stars[msg.Id] == 1)
                {
                    //Remove from the starboard
                    var starref = await (starboard as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await starref.DeleteAsync();

                    cfg[context.Guild.Id].StarRef.Remove(msg.Id);
                    cfg[context.Guild.Id].Stars.Remove(msg.Id);

                    Config.Save(cfg);
                }
                else if (cfg[context.Guild.Id].Stars[msg.Id] > 1)
                {
                    //Update amount
                    cfg[context.Guild.Id].Stars[msg.Id]--;

                    var starref = await (starboard as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
                    await (starref as IUserMessage).ModifyAsync(x => x.Content = $"⭐ {cfg[context.Guild.Id].Stars[msg.Id].ToString()} <#{ch.Id}> ({msg.Id})");

                    Config.Save(cfg);
                }
            }
        }
        #endregion

        #region ReactionsCleared
        private async Task ReactionsCleared(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel ch)
        {
            var msg = await cachedMessage.GetOrDownloadAsync();

            var context = new CommandContext(client, msg);

            var starboard = await context.Guild.GetChannelAsync(cfg[context.Guild.Id].StarboardID);

            try
            {
                if (ch.Id == starboard.Id)
                    return;
            }
            catch { }

            if (!cfg.Keys.Contains(context.Guild.Id))
                return;

            var starref = await (starboard as IMessageChannel).GetMessageAsync(cfg[context.Guild.Id].StarRef[msg.Id]);
            await starref.DeleteAsync();

            cfg[context.Guild.Id].StarRef.Remove(msg.Id);
            cfg[context.Guild.Id].Stars.Remove(msg.Id);

            Config.Save(cfg);
        }
        #endregion
    }
}
