using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Discord;
using System.Diagnostics;
using Discord.WebSocket;
using System.IO;
using Newtonsoft.Json;

namespace Aqua
{
    public class AdminModule : ModuleBase
    {
        private static Random rng = new Random();

        #region eval
        /*[Command("eval", RunMode = RunMode.Async), Summary("Evaluates a piece of code.")]
        public async Task Eval([Remainder, Summary("The code.")] string input)
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            //Evaluate
            #region Evaluate
            Stopwatch sw = new Stopwatch();
            try
            {
                var i = input.Trim(new char[] { '`' });
                if (i.StartsWith("csharp") || i.StartsWith("cs"))
                    i = i.Replace("csharp", "").Replace("cs", "");
                #region options
                ScriptOptions options = ScriptOptions.Default;
                options = options.AddImports("System");
                options = options.AddImports("System.Linq");
                options = options.AddReferences(typeof(Discord.DiscordConfig).Assembly);
                options = options.AddReferences(typeof(Discord.EmbedBuilder).Assembly);
                //options = options.AddReferences(typeof(Discord.Addons.EmojiTools.EmojiExtensions).Assembly);
                options = options.AddReferences(typeof(Task).Assembly);
                options = options.AddImports("Discord");
                #endregion

                sw.Start();
                var res = await CSharpScript.EvaluateAsync(i, options, globals: this);
                sw.Stop();
                //if (res != null && res.ToString() != "")
                //{
                if (res == null || res.ToString() == "")
                    res = "Success. No results.";
                EmbedBuilder em = new EmbedBuilder()
                {
                    Title = "Evaluate",
                    Description = res.ToString(),
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"{sw.ElapsedMilliseconds}ms"
                    },
                    Color = Program.embedColor
                };
                await ReplyAsync("", false, em.Build());
                sw.Reset();
                //}
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine(ex.ToString());
                EmbedBuilder em = new EmbedBuilder()
                {
                    Title = "Error!",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"{sw.ElapsedMilliseconds}ms"
                    },
                    Color = new Color(255, 0, 0)
                };
                await ReplyAsync("", false, em.Build());
                sw.Reset();
            }
            #endregion
        }*/
        #endregion

        [Command("setgame")]
        [Alias("sg")]
        public async Task SetGame(string type, [Remainder] string text)
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            int status = 0;
            if (type.ToLower().Contains("streaming"))
                status = 1;
            if (type.ToLower().Contains("listening"))
                status = 2;
            if (type.ToLower().Contains("watching"))
                status = 3;
            await (Context.Client as DiscordSocketClient).SetGameAsync(text, null, ActivityType.Playing + status);
        }

        [Command("starboard")]
        public async Task Starboard()
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            if (Program.cfg.Keys.Contains(Context.Guild.Id))
            {
                if (Program.cfg[Context.Guild.Id].StarboardID == Context.Channel.Id)
                {
                    //Guild already exists and no change done to the starboard channel
                    #region Send embed: "This channel is already set as the starboard channel."
                    await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                    {
                        Description = "This channel is already set as the starboard channel.",
                        Color = new Color(255, 0, 0)
                    }.Build());
                    #endregion
                    return;
                }

                //Guild already exists, but different starboard channel; change existing starboard channel
                #region Change the starboard channel
                Program.cfg[Context.Guild.Id].StarboardID = Context.Channel.Id;
                #endregion

                Config.Save(Program.cfg);

                #region Send embed: "Set!"
                await Context.Message.DeleteAsync();
                var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Set!",
                    Color = Program.embedColor
                }.Build());
                await Task.Delay(5000);
                await _.DeleteAsync();
                #endregion
            }
            else
            {
                //New guild; add it to cfg
                #region Add new entry to cfg
                Program.cfg.Add(Context.Guild.Id, new Config()
                {
                    StarboardID = Context.Channel.Id,
                    StarRef = new Dictionary<ulong, ulong>(),
                    Stars = new Dictionary<ulong, int>()
                });
                #endregion

                Config.Save(Program.cfg);

                #region Send embed: "Set!"
                await Context.Message.DeleteAsync();
                var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Set!",
                    Color = Program.embedColor
                }.Build());
                await Task.Delay(5000);
                await _.DeleteAsync();
                #endregion
            }
        }

        [Command("backup")]
        public async Task Backup()
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            #region Back up
            var file = @"C:\Users\User\Desktop\DiscordBackups\" + $"#{Context.Channel.Name}@{Context.Guild.Name}.txt";

            if (File.Exists(file))
                File.Delete(file);

            List<BackupMessage> backupMsgs = new List<BackupMessage>();
            foreach (var msg in await Context.Channel.GetMessagesAsync(5000).Flatten().ToList())
            {
                IEmbed embed;
                try { embed = msg.Embeds.Where(x => x.Description.Length > 1).FirstOrDefault(); } catch { embed = null; }
                backupMsgs.Add(new BackupMessage()
                {
                    Id = msg.Id,
                    UserId = msg.Author.Id,
                    Username = msg.Author.Username,
                    Discriminator = msg.Author.Discriminator,
                    AvatarUrl = msg.Author.GetAvatarUrl(),
                    Content = msg.Content,
                    Embed = embed ?? null,
                    Timestamp = msg.Timestamp
                });
            }
            backupMsgs.Reverse();

            File.WriteAllText(file, JsonConvert.SerializeObject(backupMsgs));
            #endregion

            #region Send embed: "Done!"
            await Context.Message.DeleteAsync();
            var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
            {
                Description = "Done!",
                Color = Program.embedColor
            }.Build());
            await Task.Delay(5000);
            await _.DeleteAsync();
            #endregion
        }

        [Command("restore")]
        public async Task Restore(ulong id, string token, string channelname = "DEFAULT_NAME", string guildname = "DEFAULT_NAME", int delay = 0)
        {

            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            Discord.Webhook.DiscordWebhookClient webhook = new Discord.Webhook.DiscordWebhookClient(id, token);

            #region Restore
            var file = @"C:\Users\User\Desktop\DiscordBackups\" + $"#{(channelname == "DEFAULT_NAME" ? Context.Channel.Name : channelname)}@{(guildname == "DEFAULT_NAME" ? Context.Guild.Name : guildname)}.txt";

            if (!File.Exists(file))
                return;

            var msgs = JsonConvert.DeserializeObject<List<BackupMessage>>(File.ReadAllText(file));

            foreach (var m in msgs)
            {
                if (m.Embed != null)
                    await webhook.SendMessageAsync((m.Content is null || m.Content.Length < 1) ? "ﾠ" : m.Content, embeds: new Embed[] {
                        new EmbedBuilder()
                        {
                            Author = m.Embed.Author.HasValue?new EmbedAuthorBuilder(){
                                IconUrl = m.Embed.Author.Value.IconUrl,
                                Name = m.Embed.Author.Value.Name,
                                Url = m.Embed.Author.Value.Url
                            }:null,
                            Color = m.Embed.Color,
                            Description = m.Embed.Description,

                        }.Build()
                    }, username: m.Username, avatarUrl: m.AvatarUrl);
                else
                    await webhook.SendMessageAsync((m.Content is null || m.Content.Length < 1) ? "ﾠ" : m.Content, username: m.Username, avatarUrl: m.AvatarUrl);
                await Task.Delay(delay);
            }
            #endregion

            #region Send embed: "Done!"
            await Context.Message.DeleteAsync();
            var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
            {
                Description = "Done!",
                Color = Program.embedColor
            }.Build());
            await Task.Delay(5000);
            await _.DeleteAsync();
            #endregion
        }

        [Command("prune")]
        public async Task Prune(int amount)
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(amount + 1).Flatten().ToList());
        }

        [Command("synchronize", RunMode = RunMode.Async)]
        public async Task Synchronize()
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            #region Send embed: "Done!"
            await Context.Message.DeleteAsync();
            var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
            {
                Description = "Done!",
                Color = Program.embedColor
            }.Build());
            await Task.Delay(5000);
            await _.DeleteAsync();
            #endregion
        }

        [Command("bye")]
        public async Task Bye()
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
            {
                Description = "Bye!",
                Color = Program.embedColor
            }.Build());
            await Context.Guild.LeaveAsync();
        }

        [Command("exit", RunMode=RunMode.Async)]
        public async Task Exit()
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            var _ = await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
            {
                Description = "Disconnecting",
                Color = Program.embedColor
            }.Build());
            await Task.Delay(2000);
            Environment.Exit(0);
        }
    }
}