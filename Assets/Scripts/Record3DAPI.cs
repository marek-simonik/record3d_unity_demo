using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

// Warninig: experimental, pre-pre-alpha quality
namespace Record3D
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct DeviceHandlesInfo
    {
        public IntPtr ptr;
        public UInt32 size;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FrameInfo
    {
        // Frame
        public IntPtr depthFrameBufferPtr;
        public IntPtr rgbFrameBufferPtr;
        public Int32 depthFrameBufferSize;
        public Int32 rgbFrameBufferSize;
        public Int32 frameWidth;
        public Int32 frameHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct FrameMetadata
    {
        public Int32 width;
        public Int32 height;
        public Int32 numComponentsPerPositionTexturePixel;
        public Int32 numComponentsPerColorTexturePixel;
    };

    public struct Record3DDevice
    {
        public Int32 handle;
    }

    public class Record3DDeviceStream
    {
        // Delegates
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void _OnStreamStopped();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void _OnNewFrame(FrameInfo frameInfo);


        // Private imports
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private const string LIBRARY_NAME = "librecord3d_unity_streaming.dylib";

#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const string LIBRARY_NAME = "record3d_unity_streaming.dll";

#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        private const string LIBRARY_NAME = "librecord3d_unity_streaming.so";

#else
#error "Unsupported platform!"
#endif

        [DllImport(LIBRARY_NAME)]
        private static extern FrameMetadata GetFrameMetadata();

        [DllImport(LIBRARY_NAME)]
        private static extern DeviceHandlesInfo ListAllDeviceHandles();

        [DllImport(LIBRARY_NAME)]
        private static extern void FinishDeviceInfoHandling(DeviceHandlesInfo devInfo);

        [DllImport(LIBRARY_NAME)]
        private static extern bool StartStreaming(Record3DDevice device, _OnNewFrame onNewFrameCallback, _OnStreamStopped onStreamStoppedCallback);


        // Private vars
        public static byte[] rgbBuffer;
        public static float[] positionsBuffer;
        public static int frameWidth;
        public static int frameHeight;


        // Public interface
        public static FrameMetadata frameMetadata => GetFrameMetadata();

        public static List<Record3DDevice> GetAvailableDevices()
        {
            // Obtain and select device handle
            DeviceHandlesInfo deviceHandlesInfo = ListAllDeviceHandles();
            Int32[] deviceHandles = new Int32[deviceHandlesInfo.size];
            IntPtr pointer = deviceHandlesInfo.ptr;
            Marshal.Copy(pointer, deviceHandles, 0, (Int32)deviceHandlesInfo.size);
            FinishDeviceInfoHandling(deviceHandlesInfo);

            return deviceHandles.ToList().Select(x => new Record3DDevice() { handle = x }).ToList();
        }

        public static bool StartStream(Record3DDevice device/*, OnNewFrame onNewFrameCallback, OnStreamStopped onStreamStoppedCallback*/)
        {
            var _frameMetadata = frameMetadata;
            int rgbBuffLength = _frameMetadata.width * _frameMetadata.height * _frameMetadata.numComponentsPerColorTexturePixel;
            int posBuffLength = _frameMetadata.width * _frameMetadata.height * _frameMetadata.numComponentsPerPositionTexturePixel;

            rgbBuffer = new byte[rgbBuffLength];
            positionsBuffer = new float[posBuffLength];

            _OnNewFrame newFrameCallback = (data) =>
            {
                frameWidth = data.frameWidth;
                frameHeight = data.frameHeight;
                Marshal.Copy(data.rgbFrameBufferPtr, rgbBuffer, 0, rgbBuffer.Length);
                Marshal.Copy(data.depthFrameBufferPtr, positionsBuffer, 0, positionsBuffer.Length);
            };

            _OnStreamStopped streamStoppedCallback = () =>
            {
            };

            bool connectionEstablished = StartStreaming(device, newFrameCallback, streamStoppedCallback);
            return connectionEstablished;
        }
    }
}