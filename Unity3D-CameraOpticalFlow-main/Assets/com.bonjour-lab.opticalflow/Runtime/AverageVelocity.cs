using UnityEngine;
using Bonjour.Vision.Ressources;

namespace Bonjour.Vision
{
    public class AverageVelocity : MonoBehaviour
    {
        [Header("Ressource")]
        public AverageVelocityRessourceSet averageVelocityRessource;

        [Header("Average params")]
        [Tooltip("Is average computed on Trail or Optical Flow")] public bool isComputedOnTrail = false;
        [Tooltip("Define the resolution on which we will compute the average")] public int resolution = 4;
        [Range(0, 1)]
        [Tooltip("Define the range for min velocity")] public float threshold = 0.1f;

        [HideInInspector] public Vector3 averageVelocity;       // stores the computed average velocity.
        private Vector3[] averageArray  = new Vector3[1];       // an array used to pass data between the compute shader and the script

        private OpticalFlow of;                                 // Optical Flow component
        private OFTrailSystemUpdater trail;                     // OFTrail component

        private RenderTexture sourceCopy;                       // RenderTexture to copy into

        //compute shader params
        private ComputeShader compute;
        private int kernelHandle;
        private ComputeBuffer averageVelocityBuffer;
        [Tooltip("Log the size of the buffer")] public bool logBufferSize;

        private void Start(){
            InitBuffers();
        }

        // calculate the average velocity each frame
        private void Update(){
            ComputeAverageVelocity();
        }

        // initializes the average velocity buffer and sets up the compute shader
        private void InitBuffers(){
            //init average vel + buffer
            averageVelocity         = Vector2.zero;
            averageArray[0]         = averageVelocity;
            averageVelocityBuffer   = new ComputeBuffer(1, sizeof(float) * 3);
            averageVelocityBuffer.SetData(averageArray);

            //Get OF or Trail
            of      = this.GetComponent<OpticalFlow>();
            trail   = this.GetComponent<OFTrailSystemUpdater>();
            
            //Init Compute Buffer
            compute         = Instantiate(averageVelocityRessource.averageVelocityCS);
            kernelHandle    = compute.FindKernel("CSMain");
            compute.SetBuffer(kernelHandle, "_AverageVelocity", averageVelocityBuffer);
        }

        // calculates the average velocity based on the selected data source (trail or optical flow)
        private void ComputeAverageVelocity(){
            if(trail && trail.GetOFTrail() == null) return; //Trail is created at first loop so we need to jump this frame (lazy implementation ;))

            if(sourceCopy == null){
                //Lazzy RT creation to avoid getting a RT size of 0 due to Start() order
                sourceCopy = new RenderTexture(of.opticalFlowWidth/resolution, of.opticalFlowHeight/resolution, 24, RenderTextureFormat.ARGBFloat);
                compute.SetTexture(kernelHandle, "_Source", sourceCopy);
                compute.SetVector("_Resolution", new Vector2(sourceCopy.width, sourceCopy.height));
                if(logBufferSize) Debug.Log($"Average buffer source size set at: {sourceCopy.width}×{sourceCopy.height}");
            }

            Graphics.Blit(isComputedOnTrail ? trail.GetOFTrail() : of.GetOpticalFlowMap(), sourceCopy);
            
            compute.SetFloat("_Threshold", threshold);
            compute.Dispatch(kernelHandle, 1, 1, 1);

            averageVelocityBuffer.GetData(averageArray);
            averageVelocity = (Vector3) averageArray[0];
        }

        private void OnDisable()
        {
            if (averageVelocityBuffer != null)
            {
                averageVelocityBuffer.Release();
            }
        }

        public Vector3 GetAverageVelocity()
        {
            return averageVelocity;
        }
    }
}