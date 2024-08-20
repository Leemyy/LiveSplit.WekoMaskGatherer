using LiveSplit.Model;
using LiveSplit.UI.Components;
using Voxif.AutoSplitter;
using Voxif.IO;

[assembly: ComponentFactory(typeof(Factory))]
namespace LiveSplit.WekoMaskGatherer {
    public partial class WekoMaskGathererComponent : Component {
        protected override EGameTime GameTimeType => EGameTime.Loading;

        private WekoMaskGathererMemory memory;

        public WekoMaskGathererComponent(LiveSplitState state) : base(state) {
#if DEBUG
            logger = new ConsoleLogger();
#else
            logger = new  FileLogger("_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log");
#endif
            logger.StartLogger();

            memory = new WekoMaskGathererMemory(logger);

            settings = new TreeSettings(state, StartSettings, ResetSettings, OptionsSettings);

            remainingSplits = new RemainingDictionary(logger);
        }

        public override void Dispose() {
            memory.Dispose();
            memory = null;
            base.Dispose();
        }
    }
}