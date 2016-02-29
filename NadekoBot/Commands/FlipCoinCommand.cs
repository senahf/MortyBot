using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Legacy;
using NadekoBot.Extensions;
using System.Drawing;

namespace NadekoBot {
    class FlipCoinCommand : DiscordCommand {

        private Random _r;
        public FlipCoinCommand() : base() {
            _r = new Random();
        }

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {

            if (e.GetArg("count") == "") {
                if (_r.Next(0, 2) == 1)
                    await e.Channel.SendFile("heads.png", Properties.Resources.heads.ToStream(System.Drawing.Imaging.ImageFormat.Png));
                else
                    await e.Channel.SendFile("tails.png", Properties.Resources.tails.ToStream(System.Drawing.Imaging.ImageFormat.Png));
            } else {
                int result;
                if (int.TryParse(e.GetArg("count"), out result)) {
                    if (result > 10)
                        result = 10;
                    Image[] imgs = new Image[result];
                    for (int i = 0; i < result; i++) {
                        imgs[i] = _r.Next(0, 2) == 0 ?
                                    Properties.Resources.tails :
                                    Properties.Resources.heads;
                    }
                    await e.Channel.SendFile($"{result} coins.png", imgs.Merge().ToStream(System.Drawing.Imaging.ImageFormat.Png));
                    return;
                }

                await e.Channel.SendMessage("Invalid number");
            }

        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("$flip")
                .Description("Flips coin(s) - heads or tails, and shows an image.\n**Usage**: `$flip` or `$flip 3`")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());
        }
    }
}
