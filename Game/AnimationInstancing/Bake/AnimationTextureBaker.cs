using System.Collections.Generic;
using UnityEngine;
using static AnimationInstancing_v2.AnimationBaker;

namespace AnimationInstancing_v2
{
    public class AnimationTextureBaker
    {
        private static readonly int[] stardardTextureSize = { 64, 128, 256, 512, 1024 };

        public static AnimationTextureData GenerateAnimationTextureData(
            List<AnimationInfo> animInfoList,
            List<AnimationPoseData> animPoseDataList,
            int boneCount)
        {
            var animationTextureData = CreateAnimationTextureData(animInfoList, boneCount);

            FillAnimationTexture(animationTextureData, animInfoList, animPoseDataList);

            return animationTextureData;
        }

        public static AnimationTextureData CreateAnimationTextureData(
            List<AnimationInfo> infoList, 
            int boneCount)
        {
            var textureBlockWidth = 4;
            var textureBlockHeight = boneCount;
            var frames = new int[infoList.Count];
            for (var i = 0; i != infoList.Count; ++i)
            {
                frames[i] = infoList[i].totalFrame;
            }

            var textureWidth = CalculateTextureSize(out int count, frames, textureBlockWidth, textureBlockHeight);
            Debug.Assert(textureWidth > 0);

            var bakedBoneTextures = new Texture2D[count];
            var format = TextureFormat.RGBAHalf;
            for (int i = 0; i != count; ++i)
            {
                int width = count > 1 && i < count ? stardardTextureSize[^1] : textureWidth;
                bakedBoneTextures[i] = new Texture2D(width, width, format, false)
                {
                    filterMode = FilterMode.Point,
                    name = $"{textureWidth}"
                };
            }

            return new AnimationTextureData
            {
                textureBlockWidth = textureBlockWidth,
                textureBlockHeight = textureBlockHeight,
                bakedBoneTextures = bakedBoneTextures,
            };
        }

        // calculate the texture count and every size
        public static int CalculateTextureSize(
            out int textureCount,
            int[] frames,
            int blockHeight,
            int blockWidth)
        {
            int textureWidth = stardardTextureSize[0];

            int count = 1;
            for (int i = stardardTextureSize.Length - 1; i >= 0; --i)
            {
                int size = stardardTextureSize[i];
                int blockCountEachLine = size / blockWidth;
                int x = 0, y = 0;
                int k = 0;
                for (int j = 0; j != frames.Length; ++j)
                {
                    int frame = frames[j];
                    int currentLineEmptyBlockCount = (size - x) / blockWidth % blockCountEachLine;
                    bool check = x == 0 && y == 0;
                    x = (x + frame % blockCountEachLine * blockWidth) % size;
                    if (frame > currentLineEmptyBlockCount)
                    {
                        y += (frame - currentLineEmptyBlockCount) / blockCountEachLine * blockHeight;
                        y += currentLineEmptyBlockCount > 0 ? blockHeight : 0;
                    }

                    if (y + blockHeight > size)
                    {
                        x = y = 0;
                        ++count;
                        k = j--;
                        if (check)
                        {
                            if (i == stardardTextureSize.Length - 1)
                            {
                                //Debug.LogError("There is certain animation's frame larger than a texture.");
                                textureCount = 0;
                                return -1;
                            }
                            else
                                break;
                        }
                    }
                }

                bool suitable = false;
                if (count > 1 && i == stardardTextureSize.Length - 1)
                {
                    for (int m = 0; m != stardardTextureSize.Length; ++m)
                    {
                        size = stardardTextureSize[m];
                        x = y = 0;
                        for (int n = k; n < frames.Length; ++n)
                        {
                            int frame = frames[n];
                            int currentLineEmptyBlockCount = (size - x) / blockWidth % blockCountEachLine;
                            x = (x + frame % blockCountEachLine * blockWidth) % size;
                            if (frame > currentLineEmptyBlockCount)
                            {
                                y += (frame - currentLineEmptyBlockCount) / blockCountEachLine * blockHeight;
                                y += currentLineEmptyBlockCount > 0 ? blockHeight : 0;
                            }
                            if (y + blockHeight <= size)
                            {
                                suitable = true;
                                break;
                            }
                        }
                        if (suitable)
                        {
                            textureWidth = size;
                            break;
                        }
                    }
                }
                else if (count > 1)
                {
                    textureWidth = stardardTextureSize[i + 1];
                    count = 1;
                    suitable = true;
                }

                if (suitable)
                {
                    break;
                }
            }
            textureCount = count;
            return textureWidth;
        }

        private static void FillAnimationTexture(
            AnimationTextureData animationTextureData,
            List<AnimationInfo> infoList,
            List<AnimationPoseData> animPoseDataList)
        {
            if (animPoseDataList.Count <= 1) return;

            var bakedBoneTexture = animationTextureData.bakedBoneTextures;
            var textureBlockWidth = animationTextureData.textureBlockWidth;
            var textureBlockHeight = animationTextureData.textureBlockHeight;

            int pixelx = 0;
            int pixely = 0;
            int bakedTextureIndex = 0;
            int preNameCode = animPoseDataList[0].stateName;
            int count = animPoseDataList.Count;
            for (int i = 0; i != count; ++i)
            {
                var matrixData = animPoseDataList[i];
                if (matrixData.poseMatrices == null)
                    continue;
                if (preNameCode != matrixData.stateName)
                {
                    preNameCode = matrixData.stateName;
                    int totalFrames = count - i;
                    for (int j = i; j != count; ++j)
                    {
                        if (preNameCode != animPoseDataList[j].stateName)
                        {
                            totalFrames = j - i;
                            break;
                        }
                    }

                    int width = bakedBoneTexture[bakedTextureIndex].width;
                    int height = bakedBoneTexture[bakedTextureIndex].height;
                    int y = pixely;
                    int currentLineBlockCount = (width - pixelx) / textureBlockWidth % (width / textureBlockWidth);
                    totalFrames -= currentLineBlockCount;
                    if (totalFrames > 0)
                    {
                        int framesEachLine = width / textureBlockWidth;
                        y += totalFrames / framesEachLine * textureBlockHeight;
                        y += currentLineBlockCount > 0 ? textureBlockHeight : 0;
                        if (height < y + textureBlockHeight)
                        {
                            ++bakedTextureIndex;
                            pixelx = 0;
                            pixely = 0;
                            Debug.Assert(bakedTextureIndex < bakedBoneTexture.Length);
                        }
                    }

                    foreach (var info in infoList)
                    {
                        if (info.animationNameHash == matrixData.stateName)
                        {
                            info.startFrameIndex = pixelx / textureBlockWidth + pixely / textureBlockHeight * bakedBoneTexture[bakedTextureIndex].width / textureBlockWidth;
                            info.textureIndex = bakedTextureIndex;
                        }
                    }
                }
                if (matrixData.poseMatrices != null)
                {
                    Debug.Assert(pixely + textureBlockHeight <= bakedBoneTexture[bakedTextureIndex].height);
                    var color = Convert2Color(matrixData.poseMatrices);
                    bakedBoneTexture[bakedTextureIndex].SetPixels(pixelx, pixely, textureBlockWidth, textureBlockHeight, color);
                    matrixData.frameIndex = pixelx / textureBlockWidth + pixely / textureBlockHeight * bakedBoneTexture[bakedTextureIndex].width / textureBlockWidth;
                    pixelx += textureBlockWidth;
                    if (pixelx + textureBlockWidth > bakedBoneTexture[bakedTextureIndex].width)
                    {
                        pixelx = 0;
                        pixely += textureBlockHeight;
                    }
                    if (pixely + textureBlockHeight > bakedBoneTexture[bakedTextureIndex].height)
                    {
                        Debug.Assert(animPoseDataList[i + 1].stateName != matrixData.stateName);
                        ++bakedTextureIndex;
                        pixelx = 0;
                        pixely = 0;
                        Debug.Assert(bakedTextureIndex < bakedBoneTexture.Length);
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        private static Color[] Convert2Color(Matrix4x4[] boneMatrix)
        {
            var color = new Color[boneMatrix.Length * 4];
            int index = 0;
            foreach (var obj in boneMatrix)
            {
                color[index++] = obj.GetRow(0);
                color[index++] = obj.GetRow(1);
                color[index++] = obj.GetRow(2);
                color[index++] = obj.GetRow(3);
            }
            return color;
        }
    }
}