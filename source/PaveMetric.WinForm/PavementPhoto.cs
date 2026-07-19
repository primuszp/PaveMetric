using System;
using System.Collections.Generic;

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
    }
}