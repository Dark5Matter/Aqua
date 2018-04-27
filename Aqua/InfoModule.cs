using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Diagnostics;
using Discord.WebSocket;

namespace Aqua
{
    public class InfoModule : ModuleBase
    {
        private readonly CommandService _commands;
        private readonly IServiceProvider _map;

        public InfoModule(IServiceProvider map, CommandService commands)
        {
            _commands = commands;
            _map = map;
        }

        [Command("help")]
        [Alias("h")]
        [Summary("Lists all of the public commands.")]
        public async Task Help(string path = "")
        {
            var e = new EmbedBuilder();

            if (path == "")
            {
                e = new EmbedBuilder()
                {
                    Title = "Help - Module list",
                    Color = Program.embedColor,
                    Footer = new EmbedFooterBuilder() { Text = "Use 'help <module>' to list a module's commands." }
                };
                foreach (var mod in _commands.Modules.Where(x => x.Parent == null)) if (!mod.Name.Contains("Admin")) AddHelp(mod, ref e);
            }
            else
            {
                var mod = _commands.Modules.FirstOrDefault(x => x.Name.Replace("Module", "").ToLower() == path.ToLower());
                if (mod == null)
                {
                    // Command, show command info.
                    var cmd = _commands.Commands.Where(x => x.Name.ToLower() == path.ToLower()).FirstOrDefault();
                    if (cmd == null) { await ReplyAsync("Invalid module/command."); return; }
                    cmd.CheckPreconditionsAsync(Context, _map).GetAwaiter().GetResult();

                    e = new EmbedBuilder()
                    {
                        Title = "Help - " + cmd.Name,
                        Color = Program.embedColor,
                        Footer = new EmbedFooterBuilder() { Text = "Module: " + cmd.Module.Name.Replace("Module", "") }
                    };
                    e.AddField(f =>
                    {
                        f.Name = $"__**Aliases**__";
                        f.Value = (cmd.Aliases.Count > 1 ? string.Join(", ", cmd.Aliases.Select(x => $"`{x}`")) : $"`{cmd.Name}`");
                    });
                    e.AddField(f =>
                    {
                        f.Name = $"__**Usage**__";
                        f.Value = $"`{GetPrefix(cmd)} {GetAliases(cmd)}`";
                    });
                    e.AddField(f =>
                    {
                        f.Name = $"__**Summary**__";
                        f.Value = cmd.Summary;
                    });
                }
                else
                {
                    e = new EmbedBuilder()
                    {
                        Title = "Help - " +mod.Name.Replace("Module", ""),
                        Color = Program.embedColor,
                        Description = $"{mod.Summary}\n" +
                        (!string.IsNullOrEmpty(mod.Remarks) ? $"({mod.Remarks})\n" : "") +
                        (mod.Aliases.Count>1 ? $"Prefixes: {string.Join(", ", mod.Aliases)}\n" : "") +
                        (mod.Submodules.Any() ? $"Submodules: {mod.Submodules.Select(x => x.Name.Replace("Module", ""))}\n" : "") + " "
                    };
                    AddCommands(mod, ref e);
                }
            }

            await Context.Channel.SendMessageAsync("", embed: e.Build());
        }

        public void AddHelp(ModuleInfo module, ref EmbedBuilder builder)
        {
            foreach (var sub in module.Submodules) AddHelp(sub, ref builder);
            builder.AddField(f =>
            {
                f.Name = $"__**{module.Name.Replace("Module", "")}**__";
                f.Value = (module.Submodules.Any() ? $"Submodules: {module.Submodules.Select(x => x.Name.Replace("Module", ""))}\n" : "") +
                $"\n" +
                string.Join(", ", module.Commands.Select(x => $"`{x.Name}`"));
            });
        }

        public void AddCommands(ModuleInfo module, ref EmbedBuilder builder)
        {
            foreach (var command in module.Commands)
            {
                command.CheckPreconditionsAsync(Context, _map).GetAwaiter().GetResult();
                AddCommand(command, ref builder);
            }
        }

        public void AddCommand(CommandInfo command, ref EmbedBuilder builder)
        {
            builder.AddField(f =>
            {
                f.Name = $"__**{command.Name}**__";
                f.Value = $"{command.Summary}\n" +
                (!string.IsNullOrEmpty(command.Remarks) ? $"({command.Remarks})\n" : "") +
                (command.Aliases.Count>1 ? $"**Aliases:** {string.Join(", ", command.Aliases.Select(x => $"`{x}`"))}\n" : "") +
                $"**Usage:** `{GetPrefix(command)} {GetAliases(command)}`";
            });
        }

        public string GetAliases(CommandInfo command)
        {
            StringBuilder output = new StringBuilder();
            if (!command.Parameters.Any()) return output.ToString();
            foreach (var param in command.Parameters)
            {
                if (param.IsOptional)
                    output.Append($"[{param.Name} = {param.DefaultValue}] ");
                else if (param.IsMultiple)
                    output.Append($"|{param.Name}| ");
                else if (param.IsRemainder)
                    output.Append($"...{param.Name} ");
                else
                    output.Append($"<{param.Name}> ");
            }
            return output.ToString();
        }
        public string GetPrefix(CommandInfo command)
        {
            var output = GetPrefix(command.Module);
            output += $"{command.Aliases.FirstOrDefault()} ";
            return output;
        }
        public string GetPrefix(ModuleInfo module)
        {
            string output = "";
            if (module.Parent != null) output = $"{GetPrefix(module.Parent)}{output}";
            if (module.Aliases.Any())
                output += string.Concat(module.Aliases.FirstOrDefault(), " ");
            return output;
        }
    }
}
