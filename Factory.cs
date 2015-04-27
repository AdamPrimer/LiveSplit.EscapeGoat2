using System.Reflection;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

namespace LiveSplit.EscapeGoat2Autosplitter
{
    public class Factory : IComponentFactory
    {
        public string ComponentName {
            get { return "Escape Goat 2 Autosplitter"; }
        }

        public string Description {
            get { return "Autosplitter for Escape Goat 2"; }
        }

        public ComponentCategory Category {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state) {
            return new EscapeGoat2Component();
        }

        public string UpdateName {
            get { return ""; }
        }

        public string UpdateURL {
            get { return "http://livesplit.org/update/"; }
        }

        public Version Version {
            get { return new Version(); }
        }

        public string XMLURL {
            get { return "http://livesplit.org/update/Components/noupdates.xml"; }
        }
    }
}
