namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Immutable context handed to an <see cref="ITrackRecorder"/> when it is initialized for a target. Carries
    /// the sampling configuration so recorders can size buffers and apply compression consistently.
    /// </summary>
    public readonly struct RecorderContext
    {
        /// <summary>Target sampling rate in frames per second.</summary>
        public readonly int Fps;

        /// <summary>Per-property epsilon below which consecutive samples are treated as identical.</summary>
        public readonly float PositionEpsilon;

        /// <summary>Rotation epsilon in degrees for keyframe reduction.</summary>
        public readonly float RotationEpsilon;

        /// <summary>Generic scalar epsilon for keyframe reduction of float properties.</summary>
        public readonly float ScalarEpsilon;

        /// <summary>When true recorders should pack floats to half precision where lossless-enough.</summary>
        public readonly bool UseHalfPrecision;

        /// <summary>Estimated capacity hint (frames) used to pre-size sample buffers.</summary>
        public readonly int CapacityHint;

        public RecorderContext(int fps, float positionEpsilon, float rotationEpsilon, float scalarEpsilon,
            bool useHalfPrecision, int capacityHint)
        {
            Fps = fps;
            PositionEpsilon = positionEpsilon;
            RotationEpsilon = rotationEpsilon;
            ScalarEpsilon = scalarEpsilon;
            UseHalfPrecision = useHalfPrecision;
            CapacityHint = capacityHint;
        }
    }
}
