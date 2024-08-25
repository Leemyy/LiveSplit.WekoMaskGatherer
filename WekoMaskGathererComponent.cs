using LiveSplit.Model;
using LiveSplit.UI.Components;
using System.Collections.Generic;
using Voxif.AutoSplitter;
using Voxif.IO;

[assembly: ComponentFactory(typeof(Factory))]
namespace LiveSplit.WekoMaskGatherer {
    public partial class WekoMaskGathererComponent : Component {
        protected override EGameTime GameTimeType => EGameTime.Loading;

        private WekoMaskGathererMemory _memory;

        public WekoMaskGathererComponent(LiveSplitState state) : base(state) {
#if DEBUG
            logger = new ConsoleLogger();
#else
            logger = new  FileLogger("_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log");
#endif
            logger.StartLogger();

            _memory = new WekoMaskGathererMemory(logger);

            settings = new TreeSettings(state, StartSettings, ResetSettings, OptionsSettings);

            _completedSplits = new HashSet<(string, string)>();
            _remainingSplits = new RemainingDictionary(logger);
            _seenActors = new HashSet<FNameEntryId>();
        }

        public override void Dispose() {
            _memory.Dispose();
            _memory = null!;
            base.Dispose();
        }
    }
}