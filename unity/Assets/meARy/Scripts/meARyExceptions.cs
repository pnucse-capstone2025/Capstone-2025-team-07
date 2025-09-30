using System;


namespace meARy
{
    public class CannotCaptureScreenException : Exception
    {
        public CannotCaptureScreenException() { }
        public CannotCaptureScreenException(string message) : base(message) { }
    }

    public class NoPoseDetectedException : Exception
    {
        public NoPoseDetectedException() { }
        public NoPoseDetectedException(string message) : base(message) { }
    }
    public class NoRaycastResultException : Exception
    {
        public NoRaycastResultException() { }
        public NoRaycastResultException(string message) : base(message) { }
    }
    public class CannotConvertPoseToGeospatialPoseExpcetion : Exception
    {
        public CannotConvertPoseToGeospatialPoseExpcetion() { }
        public CannotConvertPoseToGeospatialPoseExpcetion(string message) : base(message) { }
    }
    public class CannotConvertRequestToImageException : Exception
    {
        public CannotConvertRequestToImageException() { }
        public CannotConvertRequestToImageException(string message) : base(message){}
    }
}

