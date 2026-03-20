using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemHandle
    {
        private readonly Texture _previewTexture;
        private readonly Action cleanupCallback;

        public GrassDisturberTracker Tracker { get; }

        public GrassTrampleSystemHandle(
            Texture previewTexture,
            GrassDisturberTracker tracker,
            Action cleanupCallback)
        {
            _previewTexture = previewTexture;
            Tracker = tracker;
            this.cleanupCallback = cleanupCallback;
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            Tracker.SetExternalDisturbers(disturbers);
        }

        public void Cleanup()
        {
            cleanupCallback();
        }

        /// <summary>
        /// Returns one side of the ping-pong buffer for preview/debug only.
        /// May be up to one frame behind the actual result.
        /// </summary>
        public Texture GetPreviewTexture()
        {
            return _previewTexture;
        }
    }
}
