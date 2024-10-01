using Chromatics.Enums;
using Chromatics.Layers.DynamicLayers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class LayerProcessorFactory
    {
        private static LayerProcessorFactory _instance;
        private readonly Dictionary<BaseLayerType, LayerProcessor> _baseProcessors = new Dictionary<BaseLayerType, LayerProcessor>();
        private readonly Dictionary<EffectLayerType, LayerProcessor> _effectProcessors = new Dictionary<EffectLayerType, LayerProcessor>();
        private readonly Dictionary<DynamicLayerType, LayerProcessor> _dynamicProcessors = new Dictionary<DynamicLayerType, LayerProcessor>();

        private LayerProcessorFactory() { }

        public static LayerProcessorFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LayerProcessorFactory();
                }
                return _instance;
            }
        }

        public LayerProcessor GetProcessor(BaseLayerType type)
        {
            if (!_baseProcessors.ContainsKey(type))
            {
                _baseProcessors[type] = CreateBaseProcessor(type);
            }
            return _baseProcessors[type];
        }

        public LayerProcessor GetProcessor(EffectLayerType type)
        {
            if (!_effectProcessors.ContainsKey(type))
            {
                _effectProcessors[type] = CreateEffectProcessor(type);
            }
            return _effectProcessors[type];
        }

        public LayerProcessor GetProcessor(DynamicLayerType type)
        {
            if (!_dynamicProcessors.ContainsKey(type))
            {
                _dynamicProcessors[type] = CreateDynamicProcessor(type);
            }
            return _dynamicProcessors[type];
        }

        private LayerProcessor CreateBaseProcessor(BaseLayerType type)
        {
            return type switch
            {
                BaseLayerType.Static => StaticProcessor.Instance,
                BaseLayerType.ReactiveWeather => ReactiveWeatherProcessor.Instance,
                BaseLayerType.BattleStance => BaseBattleStanceProcessor.Instance,
                BaseLayerType.JobClasses => JobClassesProcessor.Instance,
                _ => throw new ArgumentException("Unknown BaseLayerType")
            };
        }

        private LayerProcessor CreateEffectProcessor(EffectLayerType type)
        {
            return type switch
            {
                EffectLayerType.None => NoneProcessor.Instance,
                EffectLayerType.DutyFinderBell => DutyFinderBellProcessor.Instance,
                EffectLayerType.DamageFlash => DamageFlashProcessor.Instance,
                EffectLayerType.GoldSaucerVegas => GoldSaucerVegasProcessor.Instance,
                EffectLayerType.CutsceneAnimation => CutsceneAnimationProcessor.Instance,
                _ => throw new ArgumentException("Unknown EffectLayerType")
            };
        }

        private LayerProcessor CreateDynamicProcessor(DynamicLayerType type)
        {
            return type switch
            {
                DynamicLayerType.None => NoneProcessor.Instance,
                DynamicLayerType.Highlight => HighlightProcessor.Instance,
                DynamicLayerType.Keybinds => KeybindsProcessor.Instance,
                DynamicLayerType.EnmityTracker => EnmityTrackerProcessor.Instance,
                DynamicLayerType.TargetHP => TargetHPProcessor.Instance,
                DynamicLayerType.TargetCastbar => TargetCastbarProcessor.Instance,
                DynamicLayerType.HPTracker => HPTrackerProcessor.Instance,
                DynamicLayerType.MPTracker => MPTrackerProcessor.Instance,
                DynamicLayerType.JobGaugeA => JobGaugeAProcessor.Instance,
                DynamicLayerType.JobGaugeB => JobGaugeBProcessor.Instance,
                DynamicLayerType.ExperienceTracker => ExperienceTrackerProcessor.Instance,
                DynamicLayerType.BattleStance => DynamicBattleStanceProcessor.Instance,
                DynamicLayerType.Castbar => CastbarProcessor.Instance,
                DynamicLayerType.JobClassesHighlight => JobClassesHighlightProcessor.Instance,
                DynamicLayerType.ReactiveWeatherHighlight => ReactiveWeatherHighlightProcessor.Instance,
                _ => throw new ArgumentException("Unknown DynamicLayerType")
            };
        }

        public void DisposeProcessor(BaseLayerType type)
        {
            if (_baseProcessors.ContainsKey(type))
            {
                _baseProcessors[type].Dispose();
                _baseProcessors.Remove(type);
            }
        }

        public void DisposeProcessor(EffectLayerType type)
        {
            if (_effectProcessors.ContainsKey(type))
            {
                _effectProcessors[type].Dispose();
                _effectProcessors.Remove(type);
            }
        }

        public void DisposeProcessor(DynamicLayerType type)
        {
            if (_dynamicProcessors.ContainsKey(type))
            {
                _dynamicProcessors[type].Dispose();
                _dynamicProcessors.Remove(type);
            }
        }

        public void DisposeAll()
        {
            foreach (var processor in _baseProcessors.Values)
            {
                Debug.WriteLine($"Disposing: {processor.GetType().Name}");
                processor.Dispose();
            }
            foreach (var processor in _effectProcessors.Values)
            {
                Debug.WriteLine($"Disposing: {processor.GetType().Name}");
                processor.Dispose();
            }
            foreach (var processor in _dynamicProcessors.Values)
            {
                Debug.WriteLine($"Disposing: {processor.GetType().Name}");
                processor.Dispose();
            }

            _baseProcessors.Clear();
            _effectProcessors.Clear();
            _dynamicProcessors.Clear();
        }
    }

}
