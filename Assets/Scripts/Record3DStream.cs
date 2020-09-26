using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Record3D;
using System;
using System.IO;
using Unity.Collections;
using UnityEngine.VFX; // Fix for newer versions of Unity


// Warninig: experimental, pre-pre-alpha quality
//[ExecuteInEditMode]
public class Record3DStream : MonoBehaviour
{
    [SerializeField]
    private VisualEffect streamEffect;

    [SerializeField]
    private int deviceIndex = 0;

    private Texture2D positionTex;
    private Texture2D colorTex;

    private bool isConnected = false;


    void Start()
	{
        if (streamEffect == null)
        {
            Debug.LogWarning("Visual Effect not assigned, assign it to Record3DStream Script.");
        }

        var frameMetadata = Record3DDeviceStream.frameMetadata;
        ReinitializeTextures(frameMetadata.width, frameMetadata.height);

        StartStreaming(deviceIndex);
    }

    void ReinitializeTextures(int width, int height)
    {
        Destroy(positionTex);
        Destroy(colorTex);
        positionTex = null;
        colorTex = null;
        Resources.UnloadUnusedAssets();

        positionTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
        {
            filterMode = FilterMode.Point
        };

        colorTex = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point
        };

        int numParticles = width * height;
        streamEffect.SetInt("Number of Particles", numParticles);
        streamEffect.SetTexture("Particle Position Texture", positionTex);
        streamEffect.SetTexture("Particle Color Texture", colorTex);
    }

    void StartStreaming(int devIdx)
    {
        var allAvailableDevices = Record3DDeviceStream.GetAvailableDevices();
        if (devIdx >= allAvailableDevices.Count)
        {
            Debug.LogError(string.Format("You selected device #{0} for streaming although only {1} devices was/were found. Please select appropriate device index (device IDs start from 0).", devIdx, allAvailableDevices.Count));
            return;
        }
        else
        {
            Debug.Log(string.Format("Device #{0} selected for streaming.", devIdx));
        }
        
        var selectedDevice = allAvailableDevices[devIdx];
        bool streamingSuccessfullyStarted = Record3DDeviceStream.StartStream(selectedDevice);

        isConnected = streamingSuccessfullyStarted;

        if (streamingSuccessfullyStarted)
        {
            Debug.Log(string.Format("Started Streaming with device #{0}.", selectedDevice));
        }
        else
        {
            Debug.LogError(string.Format("Could not start streaming with device #{0}. Ensure your iPhone/iPad is connected via USB, Record3D is running in USB Streaming Mode and that you have pressed the red toggle button. For more details read the Stick Note in VFX Graph Editor.", selectedDevice));
            return;
        }
    }

    private void Update()
    {
        if (isConnected)
        {
            if ( positionTex.width != Record3DDeviceStream.frameWidth || positionTex.height != Record3DDeviceStream.frameHeight )
            {
                Debug.Log(String.Format("REINITIALIZING TEXTURES {0}x{1}", Record3DDeviceStream.frameWidth, Record3DDeviceStream.frameHeight));
                ReinitializeTextures(Record3DDeviceStream.frameWidth, Record3DDeviceStream.frameHeight);
            }

            var positionTexBufferSize = positionTex.width * positionTex.height * sizeof(float);
            NativeArray<float>.Copy(Record3DDeviceStream.positionsBuffer, positionTex.GetRawTextureData<float>(), positionTexBufferSize);
            positionTex.Apply(false, false);

            const int numRGBChannels = 3;
            var colorTexBufferSize = colorTex.width * colorTex.height * numRGBChannels * sizeof(byte);
            NativeArray<byte>.Copy(Record3DDeviceStream.rgbBuffer, colorTex.GetRawTextureData<byte>(), colorTexBufferSize);
            colorTex.Apply(false, false);
        }
    }
}
