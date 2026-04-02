using System.Collections;
using UnityEngine;
using Mediapipe.Unity;

namespace Mediapipe.Unity
{
    public class ARTextureSource : ImageSource
    {
        [SerializeField] private RenderTexture _arRenderTexture;

        public override int textureWidth => _arRenderTexture ? _arRenderTexture.width : UnityEngine.Screen.width;
        public override int textureHeight => _arRenderTexture ? _arRenderTexture.height : UnityEngine.Screen.height;
        public override bool isPrepared => _arRenderTexture != null;
        public override bool isPlaying => _arRenderTexture != null;
        public override bool isVerticallyFlipped => false;
        public override bool isFrontFacing => false;
        public override RotationAngle rotation => RotationAngle.Rotation0;
        public override string sourceName => "AR Camera Texture";
        public override string[] sourceCandidateNames => new[] { "AR Camera Texture" };
        public override ResolutionStruct[] availableResolutions => new[] { new ResolutionStruct(textureWidth, textureHeight, 30) };

        public override IEnumerator Play() { yield return null; }
        public override IEnumerator Resume() { yield return null; }
        public override void Pause() { }
        public override void Stop() { }
        public override void SelectSource(int sourceId) { }
        public override Texture GetCurrentTexture() => _arRenderTexture;
    }
}