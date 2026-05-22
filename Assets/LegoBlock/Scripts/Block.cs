using Rayman;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace daydreams
{
    public class Block : MonoBehaviour
    {
        [Serializable]
        public class BasePiece
        {
            [Serializable]
            public struct Setting
            {
                public ShapeType Shape;
                public ShapeType HoleShape;
                public float Padding;
                public float Height;
                public float Roundness;
                public Color Color;
                public Color GradientColor;

                public static Setting Lerp(Setting a, Setting b, float t)
                {
                    Setting setting = a;
                    setting.Padding = Mathf.Lerp(a.Padding, b.Padding, t);
                    setting.Height = Mathf.Lerp(a.Height, b.Height, t);
                    setting.Roundness = Mathf.Lerp(a.Roundness, b.Roundness, t);
                    setting.Color = Color.Lerp(a.Color, b.Color, t);
                    setting.GradientColor = Color.Lerp(a.GradientColor, b.GradientColor, t);
                    return setting;
                }
            }

            public ShapeProvider BaseShape { get; private set; }
            public ShapeProvider HoleShape { get; private set; }

            public Vector3 BaseSize
            {
                get => BaseShape.Size;
                set => BaseShape.Size = value;
            }

            public Vector3 HoleSize
            {
                get => HoleShape.Size;
                set => HoleShape.Size = value;
            }

            public BasePiece(ShapeProvider baseShape, ShapeProvider holeShape, Transform parent, Setting setting)
            {
                BaseShape = baseShape;
                HoleShape = holeShape;
                BaseShape.transform.SetParent(parent);
                BaseShape.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                HoleShape.transform.SetParent(BaseShape.transform);
                HoleShape.transform.SetLocalPositionAndRotation(new Vector3(0f, -setting.Height, 0f), Quaternion.identity);

                UpdateSetting(setting);
            }

            public void UpdateSetting(Setting setting)
            {
                BaseShape.Shape = setting.Shape;
                BaseShape.Operation = OperationType.Union;
                BaseShape.Pivot.y = 0;
                BaseShape.Roundness = setting.Roundness;
                BaseShape.Color = setting.Color;
                BaseShape.GradientColor = setting.GradientColor;

                HoleShape.Shape = setting.HoleShape;
                HoleShape.Operation = OperationType.Subtract;
                HoleShape.Pivot.y = 0;
                HoleShape.Roundness = setting.Roundness;
                HoleShape.Blend = 0.01f;
                HoleShape.Color = setting.Color;
            }
        }

        [Serializable]
        public class BumpPiece
        {
            [Serializable]
            public struct Setting
            {
                public ShapeType BumpShape;
                public ShapeType HoleShape;
                public Vector3 Size;
                public float HoleScale;
                public float Roundness;
                public float Blend;
                public Color Color;

                public static Setting Lerp(Setting a, Setting b, float t)
                {
                    Setting setting = b;
                    setting.Size = Vector3.Lerp(a.Size, b.Size, t);
                    setting.HoleScale = Mathf.Lerp(a.HoleScale, b.HoleScale, t);
                    setting.Roundness = Mathf.Lerp(a.Roundness, b.Roundness, t);
                    setting.Blend = Mathf.Lerp(a.Blend, b.Blend, t);
                    setting.Color = Color.Lerp(a.Color, b.Color, t);
                    return setting;
                }
            }

            public ShapeProvider BumpShape { get; private set; }
            public ShapeProvider HoleShape { get; private set; }

            public BumpPiece(ShapeProvider bumpShape, ShapeProvider holeShape, Transform parent, Setting setting)
            {
                BumpShape = bumpShape;
                HoleShape = holeShape;

                BumpShape.transform.SetParent(parent);
                BumpShape.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                HoleShape.transform.SetParent(BumpShape.transform);
                HoleShape.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                UpdateSetting(setting);
            }

            public void UpdateSetting(Setting setting)
            {
                BumpShape.Shape = setting.BumpShape;
                BumpShape.Operation = OperationType.Union;
                BumpShape.Size = setting.Size;
                BumpShape.Pivot.y = 1;
                BumpShape.Roundness = setting.Roundness;
                BumpShape.Blend = setting.Blend;
                BumpShape.Color = setting.Color;

                HoleShape.Shape = setting.HoleShape;
                HoleShape.Operation = OperationType.Subtract;
                Vector3 holeSize = setting.HoleScale < 0.4f ? Vector3.zero
                    : Vector3.Scale(setting.Size, new Vector3(setting.HoleScale, 1f, setting.HoleScale));
                HoleShape.Size = holeSize;
                HoleShape.Pivot.y = 1;
                HoleShape.Roundness = setting.Roundness;
                HoleShape.Blend = setting.Blend;
                HoleShape.Color = setting.Color;
            }
        }

        public BasePiece.Setting BaseSetting;
        public BumpPiece.Setting BumpSetting;

        [SerializeField] private ShapeObject shapeObject;

        private BasePiece basePiece;
        private List<BumpPiece> bumpPieces = new();
        private BumpPiece[][] bumpGrid;
        private IObjectPool<ShapeProvider> pool;
        private Vector2Int currentBumpGridSize;

        public BasePiece Base => basePiece;
        public BumpPiece[][] BumpGrid => bumpGrid;

        private void Awake()
        {
            pool = new ObjectPool<ShapeProvider>(CreatePoolElement,
                TakeFromPool, ReturnedToPool, DestroyPoolElement, false);
        }

        public void InitializeBlock(Vector2Int bumpCount, Vector2 bumpSpacing)
        {
            SetupBlocks(bumpCount);

            Vector2 bumpSize = new(BumpSetting.Size.x, BumpSetting.Size.z);
            bumpSpacing += bumpSize * 2f;

            Vector2 baseSize = CalculateBaseSize(bumpCount, bumpSpacing, BaseSetting.Padding, bumpSize);
            basePiece.BaseShape.Size = new Vector3(baseSize.x, BaseSetting.Height, baseSize.y);
            float borderThickness = Mathf.Min(Base.BaseSize.x, Base.BaseSize.z) * 0.2f;
            float holeX = Mathf.Max(0f, Base.BaseSize.x - 2f * borderThickness);
            float holeZ = Mathf.Max(0f, Base.BaseSize.z - 2f * borderThickness);
            Base.HoleSize = new Vector3(holeX, Base.BaseSize.y, holeZ);
            Base.HoleShape.transform.localPosition = new Vector3(0f, -BaseSetting.Height * 1.5f, 0f);

            Vector2 bumpOffset = -baseSize + (Vector2.one * BaseSetting.Padding + bumpSize) * 0.5f + bumpSize * 0.5f;

            for (int i = 0; i < bumpCount.x; i++)
            {
                for (int j = 0; j < bumpCount.y; j++)
                {
                    Vector3 targetPosition = new Vector3(i * bumpSpacing.x + bumpOffset.x, BaseSetting.Roundness,
                        j * bumpSpacing.y + bumpOffset.y);
                    bumpGrid[i][j].BumpShape.transform.localPosition = targetPosition;
                }
            }


        }

        public Vector2 CalculateBaseSize(Vector2Int bumpCount, Vector2 bumpSpacing, float padding, Vector2 bumpSizeXZ)
        {
            Vector2 totalSize = new Vector2(
                (bumpCount.x - 1) * bumpSpacing.x,
                (bumpCount.y - 1) * bumpSpacing.y
            ) + Vector2.one * padding;
            return totalSize * 0.5f + bumpSizeXZ;
        }

        public void SetupBlocks(Vector2Int newBumpGridSize)
        {
            if (newBumpGridSize == currentBumpGridSize) return;

            // shapeObject.CleanupMaterial();

            if (basePiece == null)
                basePiece = new BasePiece(pool.Get(), pool.Get(), transform, BaseSetting);

            int newBumpCount = newBumpGridSize.x * newBumpGridSize.y;
            int currentBumpCount = currentBumpGridSize.x * currentBumpGridSize.y;
            if (newBumpCount < currentBumpCount)
            {
                int returnCount = currentBumpCount - newBumpCount;
                for (int i = 0; i < returnCount; i++)
                {
                    BumpPiece bump = bumpPieces[^1];
                    pool.Release(bump.HoleShape);
                    pool.Release(bump.BumpShape);
                    bumpPieces.RemoveAt(bumpPieces.Count - 1);
                }
            }
            else
            {
                for (int i = bumpPieces.Count; i < newBumpCount; i++)
                    bumpPieces.Add(new BumpPiece(pool.Get(), pool.Get(), basePiece.BaseShape.transform, BumpSetting));
            }

            bumpGrid = new BumpPiece[newBumpGridSize.x][];
            int index = 0;
            for (int i = 0; i < newBumpGridSize.x; i++)
            {
                bumpGrid[i] = new BumpPiece[newBumpGridSize.y];
                for (int j = 0; j < newBumpGridSize.y; j++)
                    bumpGrid[i][j] = bumpPieces[index++];
            }

            currentBumpGridSize = newBumpGridSize;

            RefreshShapeList();
            shapeObject.SetupMaterial();
        }

        public Vector3[][] GetBumpLocalPositions()
        {
            Vector3[][] localPositions = new Vector3[currentBumpGridSize.x][];
            for (int i = 0; i < bumpGrid.Length; i++)
            {
                localPositions[i] = new Vector3[bumpGrid[i].Length];
                for (int j = 0; j < bumpGrid[i].Length; j++)
                    localPositions[i][j] = bumpGrid[i][j].BumpShape.transform.localPosition;
            }
            return localPositions;
        }

        private ShapeProvider CreatePoolElement()
        {
            GameObject block = new GameObject();
            block.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return block.AddComponent<ShapeProvider>();
        }

        private void TakeFromPool(ShapeProvider bump)
        {
            bump.gameObject.SetActive(true);
            // shapeObject.AddShape(bump);
        }

        private void ReturnedToPool(ShapeProvider bump)
        {
            bump.transform.parent = null;
            bump.gameObject.SetActive(false);
            bump.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            bump.transform.localScale = Vector3.one;
            //shapeObject.RemoveShapeProvider(bump);
        }

        private void DestroyPoolElement(ShapeProvider bump)
        {
            Destroy(bump.gameObject);
        }

        private void RefreshShapeList()
        {
            var shapeProvidersField = typeof(ShapeObject).GetField("shapeProviders", BindingFlags.NonPublic | BindingFlags.Instance);
            if (shapeProvidersField != null)
            {
                var list = (List<ShapeProvider>)shapeProvidersField.GetValue(shapeObject);
                list.Clear();

                if (basePiece != null)
                {
                    list.Add(basePiece.BaseShape);
                    list.Add(basePiece.HoleShape);
                }

                foreach (var bump in bumpPieces)
                {
                    list.Add(bump.BumpShape);
                    list.Add(bump.HoleShape);
                }
            }
        }
    }
}
