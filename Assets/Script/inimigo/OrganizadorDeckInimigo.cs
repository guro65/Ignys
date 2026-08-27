using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganizadorDeckInimigo : MonoBehaviour
{
    [Header("Referência do controlador do inimigo")]
    public ControladorInimigoNaCena controladorInimigo;

    [Header("Tag dos slots do deck do inimigo")]
    public string tagSlotDeckInimigo = "SlotDackInimigo";

    [Header("Lista dos slots encontrados")]
    public List<Transform> slotsDeckInimigo = new List<Transform>();

    [Header("Cartas instanciadas no deck do inimigo")]
    public List<GameObject> cartasInstanciadas = new List<GameObject>();
    public string tagCartaInimigo = "CartaInimigo";

    [Header("Integração com CombateAmigavel")]
    [Tooltip("Neste sistema o Start apenas encontra os slots. As cartas são instanciadas depois da preparação pelo CombateAmigavel/AnimadorEntradaDeckCombate.")]
    public bool deckEhColocadoSomenteQuandoCombateInicia = true;

    private IEnumerator Start()
    {
        yield return null;
        BuscarSlotsDeckInimigo();
        // Não instancia cartas aqui. Isso impede que o player veja o deck inimigo durante a seleção.
    }

    public void BuscarSlotsDeckInimigo()
    {
        slotsDeckInimigo.Clear();

        GameObject[] slotsEncontrados = GameObject.FindGameObjectsWithTag(tagSlotDeckInimigo);
        for (int i = 0; i < slotsEncontrados.Length; i++)
        {
            if (slotsEncontrados[i] != null)
                slotsDeckInimigo.Add(slotsEncontrados[i].transform);
        }

        slotsDeckInimigo.Sort((a, b) => a.name.CompareTo(b.name));
        Debug.Log($"Foram encontrados {slotsDeckInimigo.Count} slots com a tag {tagSlotDeckInimigo}.");
    }

    // Mantido para compatibilidade/manual. No CombateAmigavel novo, o AnimadorEntradaDeckCombate é quem instancia.
    public void PosicionarCartasDoDeckInimigo()
    {
        if (controladorInimigo == null)
        {
            Debug.LogError("ControladorInimigoNaCena não foi definido no OrganizadorDeckInimigo.");
            return;
        }

        if (!controladorInimigo.deckFoiCarregado)
        {
            Debug.LogWarning("O deck do inimigo ainda não foi carregado.");
            return;
        }

        if (slotsDeckInimigo.Count == 0)
            BuscarSlotsDeckInimigo();

        if (controladorInimigo.deckInimigo == null || controladorInimigo.deckInimigo.Count == 0 || slotsDeckInimigo.Count == 0)
            return;

        LimparRegistroCartasInstanciadas();
        int quantidadeParaPosicionar = Mathf.Min(controladorInimigo.deckInimigo.Count, slotsDeckInimigo.Count);

        for (int i = 0; i < quantidadeParaPosicionar; i++)
        {
            Carta cartaPrefab = controladorInimigo.deckInimigo[i];
            Transform slotAtual = slotsDeckInimigo[i];
            if (cartaPrefab == null || slotAtual == null)
                continue;

            GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);
            Vector3 escalaOriginal = cartaInstanciada.transform.localScale;
            cartaInstanciada.transform.SetParent(slotAtual);
            cartaInstanciada.transform.position = slotAtual.position;
            cartaInstanciada.transform.localScale = escalaOriginal;
            cartaInstanciada.tag = tagCartaInimigo;
            RegistrarCartaInstanciada(cartaInstanciada);
        }
    }

    public void LimparRegistroCartasInstanciadas()
    {
        cartasInstanciadas.Clear();
    }

    public void RegistrarCartaInstanciada(GameObject carta)
    {
        if (carta != null && !cartasInstanciadas.Contains(carta))
            cartasInstanciadas.Add(carta);
    }
}
