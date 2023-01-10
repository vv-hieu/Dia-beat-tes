using UnityEngine;
using TMPro;

public class RelicCountDisplay : MonoBehaviour
{
    [SerializeField] private Transform       relicPivot;
    [SerializeField] private TextMeshProUGUI count;
    [SerializeField] private int             index;

    private GameObject m_relicObject;

    private void Update()
    {
        if (relicPivot != null && count != null && index >= 0)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null && go.TryGetComponent(out Player player)) 
            {
                if (player.GetRelic(index, out GameObject relicObject, out int relicCount, out string relicName))
                {
                    if (m_relicObject == null)
                    {
                        m_relicObject = Instantiate(relicObject);
                    }
                    m_relicObject.SetActive(true);
                    m_relicObject.transform.parent = relicPivot;
                    m_relicObject.transform.localPosition = Vector3.zero;

                    count.text = "x" + relicCount;
                }
                else
                {
                    count.text = "";
                }
            }
            else
            {
                count.text = "";
            }
        }
    }
}
