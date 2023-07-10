using UnityEngine;
using Bonjour.Vision.Ressources;

namespace Bonjour.Vision
{
    public class HighOpticFlow : MonoBehaviour
    {
        public HighOpticFlowRessourceSet highOpticFlowRessource;

        [Tooltip("Define the resolution on which we will compute the average")] public int resolution = 4;
        [Range(0, 1)]
        [Tooltip("Define the range for min velocity")] public float threshold = 0.1f;

        [HideInInspector] public Vector2[] highFlowPos;     // an array that will store the extracted high flow pos values
        [HideInInspector] public Vector2[] highFlowXY;      // an array that will store the extracted high flow XY values

        private OpticalFlow of;

        //RenderTexture to copy into
        private RenderTexture sourceCopy;

        public ComputeShader compute;
        public int maxHighFlowPixels = 1000;
        private int kernelHandle;
        private ComputeBuffer highFlowPosBuffer;
        private ComputeBuffer highFlowXYBuffer;                                 // a ComputeBuffer used to pass data between the compute shader and the script.

        void Start()
        {
            InitBuffers();
        }

        private void Update()
        {
            ComputeHighFlowPos();
        }

        private void InitBuffers()
        {
            highFlowPos = new Vector2[maxHighFlowPixels];
            highFlowXY = new Vector2[maxHighFlowPixels];

            highFlowPosBuffer = new ComputeBuffer(maxHighFlowPixels, sizeof(float) * 2);
            highFlowXYBuffer = new ComputeBuffer(maxHighFlowPixels, sizeof(float) * 2);

            of = GetComponent<OpticalFlow>();

            compute = Instantiate(highOpticFlowRessource.highOpticFlowCS);
            kernelHandle = compute.FindKernel("CSMain");
            compute.SetBuffer(kernelHandle, "_HighFlowPixels", highFlowPosBuffer);
            compute.SetBuffer(kernelHandle, "_HighFlowXY", highFlowXYBuffer);
        }

        private void ComputeHighFlowPos()
        {
            if (sourceCopy == null)
            {
                sourceCopy = new RenderTexture(of.opticalFlowWidth / resolution, of.opticalFlowHeight / resolution, 24, RenderTextureFormat.ARGBFloat);
                compute.SetTexture(kernelHandle, "_Source", sourceCopy);
                compute.SetVector("_Resolution", new Vector2(sourceCopy.width, sourceCopy.height));
            }

            Graphics.Blit(of.GetOpticalFlowMap(), sourceCopy);

            compute.SetFloat("_Threshold", threshold);
            compute.Dispatch(kernelHandle, 1, 1, 1);

            highFlowPosBuffer.GetData(highFlowPos);
            highFlowXYBuffer.GetData(highFlowXY);
        }

        private void OnDisable()
        {
            // Release the compute buffer when the script is disabled
            if (highFlowPosBuffer != null)
            {
                highFlowPosBuffer.Release();
                highFlowPosBuffer = null;
            }

            if (highFlowXYBuffer != null)
            {
                highFlowXYBuffer.Release();
                highFlowXYBuffer = null;
            }
        }

        public Vector2[] GetHighFlowPos()
        {
            return highFlowPos;
        }

        public Vector2[] GetHighFlowXY()
        {
            return highFlowXY;
        }
    }
}

