using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyConf;

namespace VoatChat
{
    class Config : ConfigBase
    {
        [InitialProp]
        public string Username {get;set;}

        [InitialProp]
        public string Password { get; set; }

        [InitialProp]
        public string Subverse { get; set; }

        [InitialProp]
        public string Network { get; set; }

        [InitialProp]
        public string Nick { get; set; }

        [InitialProp]
        public string Channel { get; set; }
    }
}
