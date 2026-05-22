using System.Collections;
using UnityEngine;

namespace daydreams
{
    public class BlockShowcaser : MonoBehaviour
    {
        private readonly Vector3[] colorPalette_1 =
            { new(0.8f, 0.5f, 0.4f), new(0.2f, 0.4f, 0.2f), new(2.0f, 1.0f, 1.0f), new(0.0f, 0.25f, 0.25f) };
        private readonly Vector3[] colorPalette_2 =
            { new(0.3f, 0.3f, 0.3f), new(0.4f, 0.4f, 0.4f), new(0.9f, 0.3f, 0.4f), new(0.9f, 0.7f, 0.2f) };
        
        [Header("Rotate")]
        [SerializeField] private bool rotateBlock = true;
        [SerializeField] private Vector3 targetDir = new(0, 0, 1);
        [SerializeField] private Vector3 rotateAxis = Vector3.one;
        [SerializeField] private float maxRotSpeed = 30f;
        [SerializeField] private float minRotSpeed = 3f;

        [Header("Random Change")]
        [SerializeField] private float randomChangeInterval = 3f;
        [SerializeField] private float lerpDuration = 1.5f;
        [SerializeField] private AnimationCurve lerpCurve;

        [SerializeField] private Block blockPrefab;
        [SerializeField] private Vector2Int bumpCountMin;
        [SerializeField] private Vector2Int bumpCountMax;
        [SerializeField] private Vector2 bumpSpacingMin;
        [SerializeField] private Vector2 bumpSpacingMax;
        [SerializeField] private Block.BasePiece.Setting baseMin;
        [SerializeField] private Block.BasePiece.Setting baseMax;
        [SerializeField] private Block.BumpPiece.Setting bumpMin;
        [SerializeField] private Block.BumpPiece.Setting bumpMax;
        
        private Block block;
        private float timeAfterChange;
        private IEnumerator changeCoroutine;
        
        private void Start()
        {
            CreateBlock();
        }
        
        private void Update()
        {
            if (rotateBlock)
                RotateBlock();
            
            if (!block) return;

            if (timeAfterChange > randomChangeInterval && changeCoroutine == null)
            {
                Block.BasePiece.Setting baseSetting = baseMin;
                baseSetting.Padding = Random.Range(baseMin.Padding, baseMax.Padding);
                baseSetting.Height = Random.Range(baseMin.Height, baseMax.Height);
                baseSetting.Roundness = Random.Range(baseMin.Roundness, baseMax.Roundness);

                float randomVal = Random.value;
                baseSetting.Color = GetColorFromPalette(randomVal, colorPalette_1);
                baseSetting.GradientColor = GetColorFromPalette(Random.value, colorPalette_1);

                Block.BumpPiece.Setting bumpSetting = bumpMin;
                bumpSetting.Size = Vector3.Lerp(bumpMin.Size, bumpMax.Size, Random.value);
                bumpSetting.HoleScale = Random.Range(bumpMin.HoleScale, bumpMax.HoleScale);
                bumpSetting.Roundness = Random.Range(bumpMin.Roundness, bumpMax.Roundness);
                bumpSetting.Blend = Random.Range(bumpMin.Blend, bumpMax.Blend);
                bumpSetting.Color = GetColorFromPalette(randomVal + 0.1f, colorPalette_1);

                Vector2Int bumpCount = new Vector2Int(Random.Range(bumpCountMin.x, bumpCountMax.x),
                    Random.Range(bumpCountMin.y, bumpCountMax.y));
                Vector2 bumpSpacing = Vector2.Lerp(bumpSpacingMin, bumpSpacingMax, Random.value);
                changeCoroutine = RandomChange(bumpCount, bumpSpacing, baseSetting, bumpSetting);
                StartCoroutine(changeCoroutine);
            }

            if (changeCoroutine == null)
                timeAfterChange += Time.deltaTime;
        }
        
        private void CreateBlock()
        {
            block = Instantiate(blockPrefab, transform);
            block.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            block.BaseSetting = baseMin;
            block.BumpSetting = bumpMin;
            block.InitializeBlock(bumpCountMin, bumpSpacingMin);
        }

        private void RotateBlock()
        {
            Vector3 forwardXZ = Vector3.Scale(transform.forward, new Vector3(1, 0, 1));
            float angle = Vector3.Angle(forwardXZ, targetDir);
            float speed = Mathf.Lerp(minRotSpeed, maxRotSpeed, angle / 180f);
            Vector3 rot = Vector3.Scale(Vector3.one * (speed * Time.deltaTime), rotateAxis);
            transform.Rotate(rot, Space.World);
        }

        private IEnumerator RandomChange(Vector2Int bumpCount, Vector2 bumpSpacing,
            Block.BasePiece.Setting targetBaseSetting, Block.BumpPiece.Setting targetBumpSetting)
        {
            block.SetupBlocks(bumpCount);
            float elapsedTime = 0f;
            
            Block.BasePiece.Setting startBaseSetting = block.BaseSetting;
            Block.BumpPiece.Setting startBumpSetting = block.BumpSetting;

            Vector2 bumpSize = new Vector2(targetBumpSetting.Size.x, targetBumpSetting.Size.z);
            bumpSpacing += bumpSize * 2f;
            
            Vector3 blockStartSize = block.Base.BaseSize;
            Vector2 baseSize = block.CalculateBaseSize(bumpCount, bumpSpacing, targetBaseSetting.Padding, bumpSize);
            Vector3 blockTargetSize = new Vector3(baseSize.x, targetBaseSetting.Height, baseSize.y);

            Vector2 bumpOffset = -baseSize + (Vector2.one * targetBaseSetting.Padding + bumpSize) * 0.5f + bumpSize * 0.5f;
            Vector3[][] startPositions = block.GetBumpLocalPositions();
            
            while (elapsedTime < lerpDuration)
            {
                float t = lerpCurve.Evaluate(elapsedTime / lerpDuration);
                
                block.BaseSetting = Block.BasePiece.Setting.Lerp(startBaseSetting, targetBaseSetting, t);
                block.BumpSetting = Block.BumpPiece.Setting.Lerp(startBumpSetting, targetBumpSetting, t);
                
                block.Base.UpdateSetting(block.BaseSetting);
                block.Base.BaseSize = Vector3.Lerp(blockStartSize, blockTargetSize, t);
                float borderThickness = Mathf.Min(block.Base.BaseSize.x, block.Base.BaseSize.z) * 0.2f;
                float holeX = Mathf.Max(0f, block.Base.BaseSize.x - 2f * borderThickness);
                float holeZ = Mathf.Max(0f, block.Base.BaseSize.z - 2f * borderThickness);
                block.Base.HoleSize = new Vector3(holeX, block.Base.BaseSize.y, holeZ);
                block.Base.HoleShape.transform.localPosition = new Vector3(0f, -block.BaseSetting.Height * 1.5f, 0f);
                
                for (int i = 0; i < block.BumpGrid.Length; i++)
                {
                    for (int j = 0; j < block.BumpGrid[i].Length; j++)
                    {
                        block.BumpGrid[i][j].UpdateSetting(block.BumpSetting);
                        
                        Vector3 targetPosition = new Vector3(i * bumpSpacing.x + bumpOffset.x, targetBaseSetting.Roundness,
                            j * bumpSpacing.y + bumpOffset.y);
                        block.BumpGrid[i][j].BumpShape.transform.localPosition = Vector3.Lerp(startPositions[i][j], targetPosition, t);
                    }
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            timeAfterChange = 0f;
            changeCoroutine = null;
        }
        
        private Color GetColorFromPalette(float t, Vector3[] palette)
        {
            Vector3 colorValue = palette[0] + Vector3.Scale(palette[1], new Vector3(
                Mathf.Cos(6.28318f * (palette[2].x * t + palette[3].x)),
                Mathf.Cos(6.28318f * (palette[2].y * t + palette[3].y)),
                Mathf.Cos(6.28318f * (palette[2].z * t + palette[3].z))
            ));
            return new Color(colorValue.x, colorValue.y, colorValue.z);
        }
    }
}
