using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Modules;

namespace NadekoBot.Modules
{
    class Verification : DiscordModule
    {
        public Verification()
        {
            //new Thread(x => CheckVerify(e)).Start();
        }
        private async void CheckVerify()
        {

        }
        public override void Install(ModuleManager manager)
        {
            
        }
    }
}
