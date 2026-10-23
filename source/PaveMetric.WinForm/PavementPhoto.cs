using System;
using System.Collections.Generic;
using System.Linq;

namespace PaveMetric
{
    [Serializable]
    public class PavementPhoto
    {
        private PerspectiveCorrection _perspectiveCorrection = new PerspectiveCorrection();
        private List<SurfaceError> _errors = new List<SurfaceError>();

        public List<SurfaceError> Errors => _errors;

        public PerspectiveCorrection PerspectiveCorrection
        {
            get => _perspectiveCorrection;
            set => _perspectiveCorrection = value;
        }

        public string PhotoFileName { get; set; }
        public double Section { get; set; }

        /// <summary>Replace geometry and reproject every defect atomically, preserving its photo footprint.</summary>
        public bool TryReplaceCorrection(PerspectiveCorrection replacement, out string reason)
        {
            reason = null;
            if (replacement == null || !replacement.Normalized || !replacement.HasUsableCoordinates)
            {
                reason = "Az új perspektívageometria érvénytelen.";
                return false;
            }
            if (Errors.Count > 0 && !PerspectiveCorrection.HasUsableCoordinates)
            {
                reason = "A korábbi geometria alapján a rögzített hibák képi helye nem határozható meg.";
                return false;
            }
            var updated = new List<SurfaceError>();
            foreach (SurfaceError error in Errors)
            {
                Pos[] outline = error.GetImageOutline(PerspectiveCorrection, Section);
                if (outline.Length < 3) { reason = "Hiányos hibakörvonal."; return false; }
                var mapped = new List<Pos>();
                foreach (Pos point in outline)
                {
                    Pos real = replacement.GetRealPosition(point);
                    Pos back = replacement.GetScreenPosition(real);
                    if (!double.IsFinite(real.X + real.Y + point.X + point.Y + back.X + back.Y)
                        || Math.Abs(back.X - point.X) > 0.5 || Math.Abs(back.Y - point.Y) > 0.5)
                    {
                        reason = "Egy rögzített hiba az új geometria mérhető tartományán kívülre kerülne. " +
                            "A geometria és a hibák nem módosultak.";
                        return false;
                    }
                    mapped.Add(real);
                }
                var next = error.Clone();
                next.Left = mapped.Min(p => p.X) - replacement.PavementWidth / 2;
                next.Right = mapped.Max(p => p.X) - replacement.PavementWidth / 2;
                next.StartSection = mapped.Min(p => p.Y) + Section;
                next.EndSection = mapped.Max(p => p.Y) + Section;
                next.ImageOutline = outline.Select(p => new Pos(p.X, p.Y)).ToList();
                bool hadArea = error.Right > error.Left && error.EndSection > error.StartSection;
                if (error.Right < error.Left || error.EndSection < error.StartSection
                    || (hadArea && (next.Right <= next.Left || next.EndSection <= next.StartSection)))
                {
                    reason = "Az átvetítés érvénytelen hibahatárokat eredményezne.";
                    return false;
                }
                updated.Add(next);
            }
            for (int i = 0; i < Errors.Count; i++) Errors[i].CopyBoundsFrom(updated[i]);
            PerspectiveCorrection = replacement;
            return true;
        }
    }
}
