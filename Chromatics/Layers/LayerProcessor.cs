using Chromatics.Enums;
using Chromatics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public abstract class LayerProcessor
    {
        public abstract void Process(IMappingLayer layer);
    }

    public class NoneProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Static Base Layer Implementation
            //throw new NotImplementedException();
        }
    }

    public static class BaseLayerProcessorFactory
    {
        private static readonly Dictionary<BaseLayerType, LayerProcessor> layerProcessors = new Dictionary<BaseLayerType, LayerProcessor>
        {
            { BaseLayerType.Static, new StaticProcessor() },
            { BaseLayerType.ReactiveWeather, new ReactiveWeatherProcessor() },
            { BaseLayerType.BattleStance, new BaseBattleStanceProcessor() },
            { BaseLayerType.JobClasses, new JobClassesProcessor() }
        };

        public static Dictionary<BaseLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }

    public static class EffectLayerProcessorFactory
    {
        private static readonly Dictionary<EffectLayerType, LayerProcessor> layerProcessors = new Dictionary<EffectLayerType, LayerProcessor>
        {
            { EffectLayerType.None, new NoneProcessor() },
            { EffectLayerType.DutyFinderBell, new DutyFinderBellProcessor() },
            { EffectLayerType.DamageFlash, new DamageFlashProcessor() }
        };

        public static Dictionary<EffectLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }

    public static class DynamicLayerProcessorFactory
    {
        private static readonly Dictionary<DynamicLayerType, LayerProcessor> layerProcessors = new Dictionary<DynamicLayerType, LayerProcessor>
        {
            { DynamicLayerType.None, new NoneProcessor() },
            { DynamicLayerType.Highlight, new HighlightProcessor() },
            { DynamicLayerType.Keybinds, new KeybindsProcessor() },
            { DynamicLayerType.EnmityTracker, new EnmityTrackerProcessor() },
            { DynamicLayerType.TargetHP, new TargetHPProcessor() },
            { DynamicLayerType.TargetCastbar, new TargetCastbarProcessor() },
            { DynamicLayerType.HPTracker, new HPTrackerProcessor() },
            { DynamicLayerType.MPTracker, new MPTrackerProcessor() },
            { DynamicLayerType.JobGauge, new JobGaugeProcessor() },
            { DynamicLayerType.ExperienceTracker, new ExperienceTrackerProcessor() },
            { DynamicLayerType.BattleStance, new DynamicBattleStanceProcessor() },
            { DynamicLayerType.Castbar, new CastbarProcessor() },
            { DynamicLayerType.JobClass, new JobClassProcessor() },
        };

        public static Dictionary<DynamicLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }


}
