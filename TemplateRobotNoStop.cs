using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;

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

