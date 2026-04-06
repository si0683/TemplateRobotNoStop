using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots
{
    [Bot("TemplateRobotNoStop")]
    public class TemplateRobotNoStop : BotPanel
    {
        public TemplateRobotNoStop(string name, StartProgram startProgram) : base(name, startProgram)
        {

        }
    }

}

