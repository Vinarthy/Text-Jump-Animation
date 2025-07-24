using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Jump : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float jumpHeight = 20f; // 字符跳动高度
    [SerializeField] private float jumpDuration = 0.3f; // 单个字符跳动时长
    [SerializeField] private float delayBetweenChars = 0.1f; // 字符间延迟
    [SerializeField] private Ease jumpEase = Ease.OutQuad; // 跳动动画曲线

    private TMP_Text textMesh;
    private List<Vector3> originalPositions = new List<Vector3>();
    private Sequence bounceSequence;
    private bool isAnimating = false;

    void Start()
    {
        textMesh = GetComponent<TMP_Text>();
        CacheOriginalPositions();
        StartAnimation();
    }

    void OnEnable()
    {
        if (isAnimating && textMesh != null)
        {
            ResetPositions();
            StartAnimation();
        }
    }

    void OnDisable()
    {
        StopAnimation();
        ResetPositions();
    }

    // 缓存每个字符的原始位置
    private void CacheOriginalPositions()
    {
        originalPositions.Clear();
        textMesh.ForceMeshUpdate(); // 强制更新网格以获取正确位置

        TMP_TextInfo textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            Vector3 charPosition = GetCharWorldPosition(charInfo);
            originalPositions.Add(charPosition);
        }
    }

    // 获取字符的世界坐标位置
    private Vector3 GetCharWorldPosition(TMP_CharacterInfo charInfo)
    {
        Vector3 bottomLeft = charInfo.bottomLeft;
        Vector3 topRight = charInfo.topRight;

        // 计算字符中心点
        Vector3 charCenter = (bottomLeft + topRight) / 2f;

        // 转换为世界坐标
        return transform.TransformPoint(charCenter);
    }

    // 开始动画
    public void StartAnimation()
    {
        if (isAnimating) return;

        isAnimating = true;
        bounceSequence = DOTween.Sequence();

        for (int i = 0; i < originalPositions.Count; i++)
        {
            int charIndex = i; // 创建局部变量捕获

            // 添加字符跳动动画
            bounceSequence.AppendCallback(() => AnimateChar(charIndex));

            // 添加字符间延迟
            if (i < originalPositions.Count - 1)
            {
                bounceSequence.AppendInterval(delayBetweenChars);
            }
        }

        // 完成一轮后重新开始
        bounceSequence.OnComplete(() => {
            if (isAnimating)
            {
                bounceSequence.Restart();
            }
        });

        bounceSequence.SetLoops(-1); // 无限循环
    }

    // 停止动画
    public void StopAnimation()
    {
        isAnimating = false;
        bounceSequence?.Kill();
    }

    // 重置所有字符位置
    private void ResetPositions()
    {
        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;
        int charIndex = 0;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            if (charIndex < originalPositions.Count)
            {
                Vector3 originalPos = originalPositions[charIndex];
                Vector3 localPos = transform.InverseTransformPoint(originalPos);

                // 更新顶点位置
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] = localPos +
                        (vertices[vertexIndex + j] - textInfo.characterInfo[i].bottomLeft);
                }

                charIndex++;
            }
        }

        // 应用顶点修改
        UpdateTextMesh();
    }

    // 动画单个字符
    private void AnimateChar(int charIndex)
    {
        if (charIndex >= originalPositions.Count) return;

        Vector3 originalPos = originalPositions[charIndex];
        Vector3 jumpTarget = originalPos + Vector3.up * jumpHeight;

        // 创建字符跳动动画
        DOTween.Sequence()
            .Append(DOTween.To(() => originalPos,
                              pos => SetCharPosition(charIndex, pos),
                              jumpTarget,
                              jumpDuration / 2f)
                    .SetEase(jumpEase))
            .Append(DOTween.To(() => jumpTarget,
                              pos => SetCharPosition(charIndex, pos),
                              originalPos,
                              jumpDuration / 2f)
                    .SetEase(jumpEase));
    }

    // 设置字符位置
    private void SetCharPosition(int charIndex, Vector3 worldPos)
    {
        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;
        int visibleCharCount = 0;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            if (visibleCharCount == charIndex)
            {
                Vector3 localPos = transform.InverseTransformPoint(worldPos);

                // 更新顶点位置
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

                Vector3 offset = localPos - (charInfo.bottomLeft + charInfo.topRight) / 2f;

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] += offset;
                }

                // 应用修改
                UpdateTextMesh();
                return;
            }

            visibleCharCount++;
        }
    }

    // 更新文本网格
    private void UpdateTextMesh()
    {
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
