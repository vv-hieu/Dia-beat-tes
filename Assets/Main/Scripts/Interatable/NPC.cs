using System;
using UnityEngine;
using UnityEngine.Events;

public class NPC : Interactable
{
    [Header("NPC")]
    [SerializeField] private string[]   dialogLines;
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private Transform  dialogBoxPosition;
    [SerializeField] private Vector2    dialogBoxSize;

    [Header("Events")]
    public NPCEvent preDialog;
    public NPCEvent postDialog;

    [Header("References")]
    [SerializeField] private GameObject interactableSign;
    [SerializeField] private Transform  sprite;

    private bool m_interacting                 = false;
    private bool m_showingApproachNotification = false;

    public void SetLines(string[] lines)
    {
        dialogLines = lines;
    }

    private void Update()
    {
        if (sprite != null)
        {
            sprite.localScale = Vector3.one * (Mathf.Sin(Time.time * 10.0f) * 0.01f + 1.0f);
        }
    }

    public override void OnApproach(Player player)
    {
        if (interactableSign != null)
        {
            interactableSign.SetActive(!m_interacting);
        }
    }

    public override void OnInteract(Player player)
    {
        if (m_interacting)
        {
            return;
        }
        m_interacting = true;
        if (interactableSign != null)
        {
            m_showingApproachNotification = interactableSign.activeInHierarchy;
            interactableSign.SetActive(false);
        }
        preDialog?.Invoke();
        GameObject go = Instantiate(dialogBox, dialogBoxPosition.transform.position, Quaternion.identity, dialogBoxPosition);
        if (go.TryGetComponent(out DialogBox dialog))
        {
            dialog.Display(dialogLines, dialogBoxSize, p_Callback);
        }
    }

    public override void OnLeave(Player player)
    {
        m_showingApproachNotification = false;
        if (interactableSign != null)
        {
            interactableSign.SetActive(false);
        }
    }

    private void p_Callback()
    {
        m_interacting = false;
        if (interactableSign != null)
        {
            interactableSign.SetActive(m_showingApproachNotification);
            m_showingApproachNotification = false;
        }
        postDialog?.Invoke();
    }

    [Serializable]
    public class NPCEvent : UnityEvent { }
}
