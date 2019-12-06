using System;
using UnityEngine;

public class CharacterHealthUI : MonoBehaviour
{
    public void UpdateUI(ref HealthStateData healthState)
    {
        if (m_Health != healthState.health)
        {
            m_Health = healthState.health;
            m_HealthText.text = (Mathf.CeilToInt(m_Health)).ToString();
        }
    }

    #pragma warning disable 649
    [SerializeField] TMPro.TMP_Text m_HealthText;
    #pragma warning restore 649

    float m_Health = -1;
}
