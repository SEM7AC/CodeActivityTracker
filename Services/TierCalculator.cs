using CodeActivityTracker.Model;

namespace CodeActivityTracker.Services
    {
    public class TierCalculator
        {
        private readonly TierNameProvider _nameProvider = new();

        // ============================
        // PUBLIC API
        // ============================
        public TierResult Calculate(double typingPct, double idePct, double debugPct,double idlePct)
            {
            int typingTier = CalculateTypingTier(typingPct);
            int ideTier = CalculateIdeTier(idePct);
            int debugTier = CalculateDebugTier(debugPct);
            int idleTier = CalculateIdleTier(idlePct);

            return new TierResult
                {
                TypingTier = typingTier,
                TypingLabel = _nameProvider.GetTypingName(typingTier),

                IdeTier = ideTier,
                IdeLabel = _nameProvider.GetIdeName(ideTier),

                DebugTier = debugTier,
                DebugLabel = _nameProvider.GetDebugName(debugTier),

                IdleTier = idleTier,
                IdleLabel = _nameProvider.GetIdleName(idleTier)
                };
            }

        // ============================
        // TIER CALCULATION LOGIC
        // ============================
        private int CalculateTypingTier(double pct)
            {
            if (pct >= 25) return 1;   // locked in
            if (pct >= 15) return 2;   // good flow
            if (pct >= 8) return 3;   // normal typing
            if (pct >= 3) return 4;   // light typing
            return 5;                  // barely typed
            }
        private int CalculateIdeTier(double pct)
            {
            if (pct >= 60) return 1;
            if (pct >= 40) return 2;
            if (pct >= 20) return 3;
            if (pct >= 10) return 4;
            return 5;
            }
        private int CalculateDebugTier(double pct)
            {
            if (pct >= 15) return 1;   // heavy debugging
            if (pct >= 8) return 2;   // active debugging
            if (pct >= 4) return 3;   // light debugging
            if (pct >= 1) return 4;   // barely debugging
            return 5;                  // no debugging
            }
        private int CalculateIdleTier(double pct)
            {
            if (pct <= 35) return 1;   // locked in
            if (pct <= 50) return 2;   // normal coding
            if (pct <= 65) return 3;   // distracted
            if (pct <= 80) return 4;   // drifting
            return 5;                  // gone
            }
        }
    }
