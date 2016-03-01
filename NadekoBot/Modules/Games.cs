﻿using System;
using System.Linq;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Commands;
using Newtonsoft.Json.Linq;
using System.IO;
//🃏
//🏁
namespace NadekoBot.Modules
{
    class Games : DiscordModule
    {
        private string[] _8BallAnswers;
        private Random _r = new Random();

        public Games() : base() {
            commands.Add(new Trivia());
            commands.Add(new SpeedTyping());
            commands.Add(new PollCommand());
            commands.Add(new ClashOfClans());

            _8BallAnswers = JArray.Parse(File.ReadAllText("data/8ball.json")).Select(t => t.ToString()).ToArray();
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(">choose")
                  .Description("Chooses a thing from a list of things\n**Usage**: >choose Get up;Sleep;Sleep more")
                  .Parameter("list", Discord.Commands.ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("list");
                      if (string.IsNullOrWhiteSpace(arg))
                          return;
                      var list = arg.Split(';');
                      if (list.Count() < 2)
                          return;
                      await e.Channel.SendMessage(list[new Random().Next(0, list.Length)]);
                  });

                cgb.CreateCommand(">8ball")
                    .Description("Ask the 8ball a yes/no question.")
                    .Parameter("question",Discord.Commands.ParameterType.Unparsed)
                    .Do(async e => {
                        string question = e.GetArg("question");
                        if (string.IsNullOrWhiteSpace(question))
                            return;
                        try {
                            await e.Channel.SendMessage(
                                $":question: **Question**: `{question}` \n:crystal_ball: **8Ball Answers**: `{_8BallAnswers[new Random().Next(0, _8BallAnswers.Length)]}`");
                        }
                        catch { }
                    });

                cgb.CreateCommand(">")
                    .Description("Attack a person. Supported attacks: 'splash', 'strike', 'burn', 'surge'.\n**Usage**: > strike @User")
                    .Parameter("attack_type",Discord.Commands.ParameterType.Required)
                    .Parameter("target",Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        var usrType = GetType(usr.Id);
                        string response = "";
                        int dmg = GetDamage(usrType, e.GetArg("attack_type").ToLowerInvariant());
                        response = e.GetArg("attack_type") + (e.GetArg("attack_type") == "splash" ? "es " : "s ") + $"{usr.Mention}{GetImage(usrType)} for {dmg}\n";
                        if (dmg >= 65)
                        {
                            response += "It's super effective!";
                        }
                        else if (dmg <= 35) {
                            response += "Ineffective!";
                        }
                        await e.Channel.SendMessage($"{ e.User.Mention }{GetImage(GetType(e.User.Id))} {response}");
                    });

                cgb.CreateCommand("poketype")
                    .Parameter("target", Discord.Commands.ParameterType.Required)
                    .Description("Gets the users element type. Use this to do more damage with strike!")
                    .Do(async e =>
                    {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("No such person.");
                        }
                        var t = GetType(usr.Id);
                        await e.Channel.SendMessage($"{usr.Name}'s type is {GetImage(t)} {t}");
                    });
            });
        }
        /*

            🌿 or 🍃 or 🌱 Grass
⚡ Electric
❄ Ice
☁ Fly
🔥 Fire
💧 or 💦 Water
⭕ Normal
🐛 Insect
🌟 or 💫 or ✨ Fairy
    */
        string GetImage(PokeType t) {
            switch (t) {
                case PokeType.WATER:
                    return "💦";
                case PokeType.GRASS:
                    return "🌿";
                case PokeType.FIRE:
                    return "🔥";
                case PokeType.ELECTRICAL:
                    return "⚡️";
                default:
                    return "⭕️";
            }
        }

        private int GetDamage(PokeType targetType, string v)
        {
            var rng = new Random();
            switch (v)
            {
                case "splash": //water
                    if (targetType == PokeType.FIRE)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.ELECTRICAL)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "strike": //grass
                    if (targetType == PokeType.ELECTRICAL)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.FIRE)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "burn": //fire
                case "flame":
                    if (targetType == PokeType.GRASS)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.WATER)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "surge": //electrical
                case "electrocute":
                    if (targetType == PokeType.WATER)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.GRASS)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                default:
                    return 0;
            }
        }

        private PokeType GetType(ulong id) {
            if (id == 113760353979990024)
                return PokeType.FIRE;

            var remainder = id % 10;
            if (remainder < 3)
                return PokeType.WATER;
            else if (remainder >= 3 && remainder < 5)
            {
                return PokeType.GRASS;
            }
            else if (remainder >= 5 && remainder < 8)
            {
                return PokeType.FIRE;
            }
            else {
                return PokeType.ELECTRICAL;
            }
        }

        private enum PokeType
        {
            WATER, GRASS, FIRE, ELECTRICAL
        }
    }
}
