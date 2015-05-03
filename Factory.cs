using System;
using System.Reflection;
using LiveSplit.UI.Components;
using LiveSplit.Model;

namespace LiveSplit.EscapeGoat2
{
    public class Factory : IComponentFactory
    {
        public string ComponentName {
            get { return "Escape Goat 2 Autosplitter v" + this.Version.ToString(); }
        }

        public string Description {
            get { return "Autosplitter for Escape Goat 2"; }
        }

        public ComponentCategory Category {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state) {
            return new EscapeGoat2Component(state);
        }

        public string UpdateName {
            get { return this.ComponentName; }
        }

        public string UpdateURL {
            get { return "https://raw.githubusercontent.com/AdamPrimer/LiveSplit.EscapeGoat2/master/"; }
        }

        public Version Version {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL {
            get { return this.UpdateURL + "Components/LiveSplit.EscapeGoat2.Updates.xml"; }
        }
    }
}
