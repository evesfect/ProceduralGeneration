using UnityEngine;

[System.Serializable]
public class BlockOrientationData
{
    public string blockName;
    public Quaternion correctionRotation = Quaternion.identity;
    public Vector3 correctionOffset = Vector3.zero;
}