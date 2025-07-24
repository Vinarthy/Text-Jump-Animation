using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Jump : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private float jumpHeight = 20f; // �ַ������߶�
    [SerializeField] private float jumpDuration = 0.3f; // �����ַ�����ʱ��
    [SerializeField] private float delayBetweenChars = 0.1f; // �ַ����ӳ�
    [SerializeField] private Ease jumpEase = Ease.OutQuad; // ������������

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

    // ����ÿ���ַ���ԭʼλ��
    private void CacheOriginalPositions()
    {
        originalPositions.Clear();
        textMesh.ForceMeshUpdate(); // ǿ�Ƹ��������Ի�ȡ��ȷλ��

        TMP_TextInfo textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            Vector3 charPosition = GetCharWorldPosition(charInfo);
            originalPositions.Add(charPosition);
        }
    }

    // ��ȡ�ַ�����������λ��
    private Vector3 GetCharWorldPosition(TMP_CharacterInfo charInfo)
    {
        Vector3 bottomLeft = charInfo.bottomLeft;
        Vector3 topRight = charInfo.topRight;

        // �����ַ����ĵ�
        Vector3 charCenter = (bottomLeft + topRight) / 2f;

        // ת��Ϊ��������
        return transform.TransformPoint(charCenter);
    }

    // ��ʼ����
    public void StartAnimation()
    {
        if (isAnimating) return;

        isAnimating = true;
        bounceSequence = DOTween.Sequence();

        for (int i = 0; i < originalPositions.Count; i++)
        {
            int charIndex = i; // �����ֲ���������

            // ����ַ���������
            bounceSequence.AppendCallback(() => AnimateChar(charIndex));

            // ����ַ����ӳ�
            if (i < originalPositions.Count - 1)
            {
                bounceSequence.AppendInterval(delayBetweenChars);
            }
        }

        // ���һ�ֺ����¿�ʼ
        bounceSequence.OnComplete(() => {
            if (isAnimating)
            {
                bounceSequence.Restart();
            }
        });

        bounceSequence.SetLoops(-1); // ����ѭ��
    }

    // ֹͣ����
    public void StopAnimation()
    {
        isAnimating = false;
        bounceSequence?.Kill();
    }

    // ���������ַ�λ��
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

                // ���¶���λ��
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

        // Ӧ�ö����޸�
        UpdateTextMesh();
    }

    // ���������ַ�
    private void AnimateChar(int charIndex)
    {
        if (charIndex >= originalPositions.Count) return;

        Vector3 originalPos = originalPositions[charIndex];
        Vector3 jumpTarget = originalPos + Vector3.up * jumpHeight;

        // �����ַ���������
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

    // �����ַ�λ��
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

                // ���¶���λ��
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

                Vector3 offset = localPos - (charInfo.bottomLeft + charInfo.topRight) / 2f;

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] += offset;
                }

                // Ӧ���޸�
                UpdateTextMesh();
                return;
            }

            visibleCharCount++;
        }
    }

    // �����ı�����
    private void UpdateTextMesh()
    {
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
