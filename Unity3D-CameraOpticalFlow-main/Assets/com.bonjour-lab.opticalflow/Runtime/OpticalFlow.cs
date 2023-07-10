using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using Bonjour.Vision.Ressources;

// ? Quid to set an iterable system for the denoiser and/or gaussian ?
namespace Bonjour.Vision
{
    public class OpticalFlow : MonoBehaviour
    {
        public enum BlurType
        {
            NONE = 0,
            LOW = 5,
            MEDIUM = 7,
            HIGH = 9,
            ULTRA = 13
        };

        public struct Float2
        {
            public float x;
            public float y;

            public Float2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [Header("Ressource")]
        public OpticalFlowRessourceSet opticalFlowRessource;

        //Compute shader
        [Header("OpticalFlow Params")]
        [Tooltip("Defines the scale resolution from the source")] public int resolution = 1;
        private int kernelHandle;
        [Tooltip("Defines the compute Shader to use for Optical flow")] private ComputeShader compute;
        private RenderTexture opticalFlow;
        [HideInInspector] public int opticalFlowWidth;
        [HideInInspector] public int opticalFlowHeight;

        [Range(0, 1)]
        [Tooltip("Lambda deviation on the gradient magnitude")] public float lambda = 0.01f;
        [Range(0, 1)]
        [Tooltip("Velocity threshold")] public float threshold = 0.01f;
        [Tooltip("Scale up velocity")] public Vector2 scale = new Vector2(1.0f, 1.0f);
        private Material gaussianBlur;
        [Tooltip("Define the type of blur")] public BlurType blurType = BlurType.ULTRA;
        [Tooltip("Define the amount of blur for the output")] [Range(1f, 100f)] public float blurSize = 25f;

        [Header("Source images")]
        [Tooltip("Source image to analyze")] public RenderTexture sourceToAnalyze;
        [Tooltip("Horizontal mirror on the source image")] public bool mirrorHor = false;
#pragma warning disable CS0649 // Field 'OpticalFlow.denoiserShader' is never assigned to, and will always have its default value null
        [Tooltip("Define a denoiser methods for source")] private Shader denoiserShader;
#pragma warning restore CS0649 // Field 'OpticalFlow.denoiserShader' is never assigned to, and will always have its default value null
        private bool useDenoiser;
        private Material denoiser;
        [HideInInspector] public RenderTexture current;
        [HideInInspector] public RenderTexture previous;
        private Vector2 rtScale, rtOffset;

        [Header("Debug")]
        [Tooltip("Show GUITexture for debug")] public bool showOpticalFlowMap;
        [Tooltip("Scale GUITexture")] public float debugMapScale = .25f;
        private RenderTexture debugView;
        private Material debugViewer;
        [Tooltip("Log the size of the buffer")] public bool logBufferSize;

        public Camera cameraToRaycast;

        // Create a string builder to store the text
        private StringBuilder sb = new();

        // Add frame counter
        private int frame = 0;

        private HighOpticFlow highOF;

        private void Start()
        {
            sb.AppendLine("Frame, Hit Game Object, Screen pos X, Screen Pos Y, Flow X, Flow Y, Flow Mag, Optic Flow Area");
            InitOpticalFlow();
        }

        private void Update()
        {
            ComputeOpticalFlow();
            HashSet<GameObject> detectedObjects = GetDetectedObjects();
            HighlightDetectedObjects(detectedObjects);
        }

        public void InitOpticalFlow()
        {
            InitSources();
            InitDenoiser();
            InitGaussianBlur();
            InitBuffers();
            InitDebugViewer();
        }

        private void InitBuffers()
        {
            opticalFlow = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 24, RenderTextureFormat.ARGBFloat);
            opticalFlow.filterMode = FilterMode.Trilinear;
            opticalFlow.wrapMode = TextureWrapMode.Clamp;
            opticalFlow.enableRandomWrite = true;
            opticalFlow.Create();

            // Bind variable to CS
            compute = Instantiate(opticalFlowRessource.opticalFlowCS);
            kernelHandle = compute.FindKernel("CSMain");
            compute.SetTexture(kernelHandle, "_OpticalFlowMap", opticalFlow);
            compute.SetVector("_Size", new Vector2((float)opticalFlow.width, (float)opticalFlow.height));
            compute.SetTexture(kernelHandle, "_Previous", previous);
            compute.SetTexture(kernelHandle, "_Current", current);
        }

        private void InitSources()
        {
            opticalFlowWidth = sourceToAnalyze.width / resolution;
            opticalFlowHeight = sourceToAnalyze.height / resolution;

            current = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 0);
            previous = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 0);

            CheckMirror();

            Graphics.Blit(sourceToAnalyze, current, rtScale, rtOffset);
            Graphics.Blit(current, previous);
        }

        private void InitDebugViewer()
        {
            debugView = new RenderTexture(sourceToAnalyze.width, sourceToAnalyze.height, 24, RenderTextureFormat.ARGB32);
            debugView.filterMode = FilterMode.Trilinear;
            debugView.wrapMode = TextureWrapMode.Clamp;

            debugViewer = new Material(Shader.Find("Hidden/OFViewer"));
        }

        private void InitDenoiser()
        {
            if (denoiserShader != null)
            {
                useDenoiser = true;
                denoiser = new Material(opticalFlowRessource.denoiser);
            }
        }

        private void InitGaussianBlur()
        {
            gaussianBlur = new Material(Shader.Find("Hidden/GaussianBlur"));
        }

        private void CheckMirror()
        {
            rtScale = mirrorHor ? new Vector2(-1, 1) : new Vector2(1, 1);
            rtOffset = mirrorHor ? new Vector2(1, 0) : new Vector2(0, 0);
        }

        public void ComputeOpticalFlow()
        {
            CheckMirror();

            //Send source to current RT
            Graphics.Blit(sourceToAnalyze, current, rtScale, rtOffset);

            //Creat TMP RT for effect
            RenderTexture temp = RenderTexture.GetTemporary(opticalFlow.width, opticalFlow.height, 24, opticalFlow.format);

            //Denoise source if denoiser is set
            if (useDenoiser)
            {
                Graphics.Blit(current, temp, denoiser); //denoise current
                Graphics.Blit(temp, current); //send tmps to current
            }

            //Compute OF
            ComputeOpticalFlowCS();

            if (blurType != BlurType.NONE)
            {
                //Blur result
                float blurAmount = (blurSize / 100f) * (float)blurType;
                int pass = gaussianBlur.FindPass(blurType.ToString());

                gaussianBlur.SetFloat("_Sigma", blurSize);
                gaussianBlur.SetVector("_Dir", new Vector2(0, 1));
                gaussianBlur.SetFloat("_BlurSize", 1.0f / current.height);
                Graphics.Blit(opticalFlow, temp, gaussianBlur, pass); //Blur vertical

                gaussianBlur.SetVector("_Dir", new Vector2(1, 0));
                gaussianBlur.SetFloat("_BlurSize", 1.0f / current.width);
                Graphics.Blit(temp, opticalFlow, gaussianBlur, pass); //Blur horizontal
            }

            if (showOpticalFlowMap)
            {
                debugViewer.SetTexture("_OF", opticalFlow);
                Graphics.Blit(sourceToAnalyze, debugView, debugViewer);
            }


            //Set current as previous frame for next frame
            Graphics.Blit(current, previous);

            //Release TMP RT
            RenderTexture.ReleaseTemporary(temp);
        }

        private void ComputeOpticalFlowCS()
        {
            compute.SetFloat("_Lambda", lambda);
            compute.SetFloat("_Threshold", threshold);
            compute.SetVector("_Scale", scale);
            compute.Dispatch(kernelHandle, Mathf.CeilToInt((float)opticalFlow.width / 32), Mathf.CeilToInt((float)opticalFlow.height / 32), 1);
        }


        private void OnDisable()
        {
            if (opticalFlow != null)
            {
                opticalFlow.Release();
            }
            opticalFlow = null;

            if (previous != null) previous = null;

            if (denoiser != null) denoiser = null;
        }

        private void OnGUI()
        {
            if (showOpticalFlowMap && opticalFlow != null)
            {
                int w = Mathf.RoundToInt(opticalFlow.width * debugMapScale);
                int h = Mathf.RoundToInt(opticalFlow.height * debugMapScale);

                GUI.DrawTexture(new Rect(w * 0, Screen.height - h, w, h), debugView);
                GUI.DrawTexture(new Rect(w * 1, Screen.height - h, w, h), opticalFlow);
                // GUI.DrawTexture(new Rect(w*1, Screen.height - h, w, h), previous);
                // GUI.DrawTexture(new Rect(w*2, Screen.height - h, w, h), current);
            }
        }

        public RenderTexture GetOpticalFlowMap()
        {
            return this.opticalFlow;
        }

        public HashSet<GameObject> GetDetectedObjects()
        {
            List<GameObject> detectedObjects = new();
            HashSet<GameObject> uniqueDetectedObjects = new();

            // Increment frame
            frame += 1;

            highOF = GetComponent<HighOpticFlow>();

            Vector2[] highOpticFlowPos = highOF.GetHighFlowPos();   // Get flow positions
            Vector2[] highFlowXY = highOF.GetHighFlowXY();          // Get flowX and flowY

            // Iterate over the flow colors and extract the flow locations
            for (int i = 0; i < highOF.maxHighFlowPixels; i++)
            {
                Vector2 flow = highFlowXY[i];
                Vector2 pos = highOpticFlowPos[i];

                if (float.IsNaN(flow.x) || float.IsNaN(flow.y))
                    continue;

                // find magnitude
                float flowMag = flow.magnitude;

                if (flowMag > threshold)
                {
                    // Convert normalized location to screen space
                    Vector2 screenPos = new(pos.x, pos.y);

                    // Raycast from camera to screen position
                    RaycastHit hit;

                    if (Physics.Raycast(cameraToRaycast.ScreenPointToRay(screenPos), out hit))
                    {
                        GameObject hitObject = hit.collider.gameObject;

                        if (uniqueDetectedObjects.Contains(hitObject))  
                            continue;

                        uniqueDetectedObjects.Add(hitObject);
                        detectedObjects.Add(hitObject);

                        // Determine if the object falls into the red or green area
                        string flowArea = flow.x > flow.y ? "red" : "green";

                        // Add the detection information to the CSV data
                        sb.AppendLine(string.Format("{0}, {1}, ({2}, {3}), ({4}, {5}), {6}, {7}", 
                            frame, hitObject.name, screenPos.x, screenPos.y, flow.x, flow.y, flowMag, flowArea));
                    }
                }
            }

            // Write the detection information to a text file
            string filePath = @"D:\Unity\LOU Buildings\Data.csv";
            File.WriteAllText(filePath, sb.ToString());

            // Return unique detected objects
            return uniqueDetectedObjects;
        }

        public void HighlightDetectedObjects(HashSet<GameObject> detectedObjects)
        {
            foreach (var detectedObject in detectedObjects)
            {
                // Get the Outline component
                var outline = detectedObject.GetComponent<Outline>();
                if (outline != null)
                {
                    // Enable the outline
                    outline.enabled = true;
                }
            }
        }
    }
}