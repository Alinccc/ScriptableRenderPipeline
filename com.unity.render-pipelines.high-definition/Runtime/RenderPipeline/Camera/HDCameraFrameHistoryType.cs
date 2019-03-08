namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum HDCameraFrameHistoryType
    {
        ColorBufferMipChain,
        VolumetricLighting,
        Exposure,
        TemporalAntialiasing,
        DepthOfFieldCoC,
#if ENABLE_RAYTRACING
        RaytracedAmbientOcclusion,
        RaytracedAreaShadow,
        RaytracedReflection,
#endif
        Count
    }
}
