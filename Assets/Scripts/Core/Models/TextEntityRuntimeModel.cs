using UnityEngine;

/// <summary>
/// Text 实体运行时参数（与关卡 EntityData.Text 同步）。
/// </summary>
public class TextEntityRuntimeModel : MonoBehaviour
{
    public TextEntityPayload Payload = new TextEntityPayload();

    public void SetPayload(TextEntityPayload payload)
    {
        Payload = TextEntityUtility.ClonePayload(payload);
        Payload.EnsureValid();
    }
}
