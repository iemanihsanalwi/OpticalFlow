using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bonjour.Vision.Ressources
{
    [CreateAssetMenu(fileName = "HighOpticFlowParams", menuName = "ScriptableObject/HighOpticFlow Ressource Set")]
    public sealed class HighOpticFlowRessourceSet : ScriptableObject
    {
        [Tooltip("Define the compute shader to use for finding high optic flow")] public ComputeShader highOpticFlowCS;
    }
}
