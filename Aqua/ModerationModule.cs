using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Diagnostics;
using Discord.WebSocket;
using Discord.Commands;

namespace Aqua
{
    /*public class ModerationModule : ModuleBase
    {
        [Command("welcomechannel", RunMode = RunMode.Async)]
        [Alias("wc")]
        [Summary("Sets the welcome channel.")]
        public async Task WelcomeChannel()
        {
            Config.CheckGuild(Program.cfg, Context.Guild.Id);
            Program.cfg[Context.Guild.Id].WelcomeID = Context.Channel.Id;

            await Context.Message.AddReactionAsync(Emote.Parse("<:khcrown:439016410358743042>"));
            await Task.Delay(3000);
            await Context.Message.DeleteAsync();
        }

        [Command("welcomemessage", RunMode = RunMode.Async)]
        [Alias("wm")]
        [Summary("Sets the welcome message.")]
        public async Task WelcomeMessage([Remainder] string content)
        {
            Config.CheckGuild(Program.cfg, Context.Guild.Id);
            Program.cfg[Context.Guild.Id].WelcomeMessage = content;

            await Context.Message.AddReactionAsync(Emote.Parse("<:khcrown:439016410358743042>"));
            await Task.Delay(3000);
            await Context.Message.DeleteAsync();
        }
    }*/
}
