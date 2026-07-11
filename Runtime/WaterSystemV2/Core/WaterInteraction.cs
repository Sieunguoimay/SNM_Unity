using System;
using System.Collections.Generic;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Global registry connecting objects to every active <see cref="WaterBody"/>.
    /// Objects register once (the <see cref="WaterDisturber"/> / <see cref="WaterFloater"/>
    /// components do it in OnEnable/OnDisable); each water body filters the
    /// lists by its own bounds every frame, so objects may move freely
    /// between waters and no wiring code is needed.
    /// </summary>
    public static class WaterInteraction
    {
        private static readonly List<IWaterDisturber> _disturbers = new();
        private static readonly List<IWaterFloater> _floaters = new();

        public static IReadOnlyList<IWaterDisturber> Disturbers => _disturbers;
        public static IReadOnlyList<IWaterFloater> Floaters => _floaters;

        /// <summary>Raised so trackers can drop per-object state (e.g. restore rigidbody drag).</summary>
        public static event Action<IWaterDisturber> DisturberRemoved;
        public static event Action<IWaterFloater> FloaterRemoved;

        public static void Register(IWaterDisturber disturber)
        {
            if (disturber != null && !_disturbers.Contains(disturber))
                _disturbers.Add(disturber);
        }

        public static void Unregister(IWaterDisturber disturber)
        {
            if (_disturbers.Remove(disturber))
                DisturberRemoved?.Invoke(disturber);
        }

        public static void Register(IWaterFloater floater)
        {
            if (floater != null && !_floaters.Contains(floater))
                _floaters.Add(floater);
        }

        public static void Unregister(IWaterFloater floater)
        {
            if (_floaters.Remove(floater))
                FloaterRemoved?.Invoke(floater);
        }

        /// <summary>Bulk registration for installer-style wiring (kept for parity with V1's SetDisturbers/SetBuoyants).</summary>
        public static void RegisterAll(IEnumerable<IWaterDisturber> disturbers)
        {
            foreach (var d in disturbers) Register(d);
        }

        public static void RegisterAll(IEnumerable<IWaterFloater> floaters)
        {
            foreach (var f in floaters) Register(f);
        }
    }
}
