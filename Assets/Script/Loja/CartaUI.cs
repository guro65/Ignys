using UnityEngine;
using UnityEngine.UI;

public class CartaUI : MonoBehaviour
{
    [SerializeField] private Image imagemCarta;

    public void Configurar(Carta carta)
    {
        if (carta == null)
            return;

        SpriteRenderer spriteRenderer = carta.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            imagemCarta.sprite = spriteRenderer.sprite;
            imagemCarta.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning($"A carta {carta.nome} não possui SpriteRenderer.");
        }
    }
}