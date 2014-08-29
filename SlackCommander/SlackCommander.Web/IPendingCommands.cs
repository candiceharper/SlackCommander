﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackCommander.Web
{
    public interface IPendingCommands
    {
        void Add(string id, SlashCommand command);
        SlashCommand Get(string id);
    }
}
