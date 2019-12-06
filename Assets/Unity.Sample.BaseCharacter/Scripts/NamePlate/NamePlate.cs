using UnityEngine;
using UnityEngine.UI;

public class NamePlate : MonoBehaviour
{
    public Transform namePlateRoot;
    public TMPro.TextMeshProUGUI nameText;
    public RawImage icon;

    public Color friendColor = Color.cyan;
    public Color enemyColor = Color.red;
    public float maxNameDistance = 50f;

    void OnDestroy()
    {
        // FIXME: This should not be necessary if TextMeshProUGUI would dispose everything that it allocates.
        nameText.ClearMesh();
    }
}
