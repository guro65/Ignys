using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimadorEntradaDeckCombate : MonoBehaviour
{
    private static AnimadorEntradaDeckCombate instancia;

    [Header("Animação automática de entrada dos decks")]
    [Min(0.05f)] [SerializeField] private float duracaoEntradaCarta = 0.32f;
    [Min(0f)] [SerializeField] private float intervaloEntrePares = 0.10f;
    [SerializeField] private float deslocamentoVertical = 0.75f;
    [SerializeField] private float rotacaoInicialGraus = 14f;
    [SerializeField] private float escalaInicialRelativa = 0.28f;

    private class DadosEntradaCarta
    {
        public GameObject obj;
        public Transform slot;
        public Vector3 escalaFinal;
        public Vector3 posicaoInicial;
        public Quaternion rotacaoInicial;
        public Collider2D[] colliders;
    }

    public static AnimadorEntradaDeckCombate ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<AnimadorEntradaDeckCombate>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("AnimadorEntradaDeckCombate");
        instancia = obj.AddComponent<AnimadorEntradaDeckCombate>();
        return instancia;
    }

    private void Awake()
    {
        if (instancia == null)
            instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    public IEnumerator ColocarDecksComAnimacao(
        List<Carta> deckPlayer,
        List<Carta> deckInimigo,
        string tagSlotDeckPlayer,
        string tagSlotDeckInimigo,
        string tagCartaPlayer,
        string tagCartaInimigo,
        PreparacaoCombatePlayer preparacaoPlayer = null,
        OrganizadorDeckInimigo organizadorInimigo = null)
    {
        List<Transform> slotsPlayer = BuscarSlots(tagSlotDeckPlayer);
        List<Transform> slotsInimigo = BuscarSlots(tagSlotDeckInimigo);

        if (preparacaoPlayer != null)
            preparacaoPlayer.LimparRegistroCartasInstanciadasNoDeck();

        if (organizadorInimigo != null)
            organizadorInimigo.LimparRegistroCartasInstanciadas();

        int qtdPlayer = deckPlayer != null ? Mathf.Min(deckPlayer.Count, slotsPlayer.Count) : 0;
        int qtdInimigo = deckInimigo != null ? Mathf.Min(deckInimigo.Count, slotsInimigo.Count) : 0;
        int maior = Mathf.Max(qtdPlayer, qtdInimigo);

        for (int i = 0; i < maior; i++)
        {
            DadosEntradaCarta player = null;
            DadosEntradaCarta inimigo = null;

            if (i < qtdPlayer && deckPlayer[i] != null)
            {
                player = CriarCarta(deckPlayer[i], slotsPlayer[i], tagCartaPlayer, true);
                if (preparacaoPlayer != null && player != null && player.obj != null)
                    preparacaoPlayer.RegistrarCartaInstanciadaNoDeck(player.obj);
            }

            if (i < qtdInimigo && deckInimigo[i] != null)
            {
                inimigo = CriarCarta(deckInimigo[i], slotsInimigo[i], tagCartaInimigo, false);
                if (organizadorInimigo != null && inimigo != null && inimigo.obj != null)
                    organizadorInimigo.RegistrarCartaInstanciada(inimigo.obj);
            }

            yield return AnimarPar(player, inimigo);

            if (intervaloEntrePares > 0f)
                yield return new WaitForSecondsRealtime(intervaloEntrePares);
        }
    }

    private DadosEntradaCarta CriarCarta(Carta prefab, Transform slot, string tagCarta, bool player)
    {
        if (prefab == null || slot == null)
            return null;

        GameObject obj = Instantiate(prefab.gameObject);
        Vector3 escalaFinal = obj.transform.localScale;

        obj.transform.SetParent(slot);
        obj.transform.position = slot.position + (player ? Vector3.down : Vector3.up) * deslocamentoVertical;
        obj.transform.localScale = escalaFinal * Mathf.Max(0.01f, escalaInicialRelativa);
        obj.transform.rotation = Quaternion.Euler(0f, 0f, player ? -rotacaoInicialGraus : rotacaoInicialGraus);
        obj.tag = tagCarta;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        Collider2D[] colliders = obj.GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        return new DadosEntradaCarta
        {
            obj = obj,
            slot = slot,
            escalaFinal = escalaFinal,
            posicaoInicial = obj.transform.position,
            rotacaoInicial = obj.transform.rotation,
            colliders = colliders
        };
    }

    private IEnumerator AnimarPar(DadosEntradaCarta player, DadosEntradaCarta inimigo)
    {
        float t = 0f;
        float duracao = Mathf.Max(0.05f, duracaoEntradaCarta);

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float suave = 1f - Mathf.Pow(1f - p, 3f);

            // Pequeno overshoot no final para dar sensação de a carta "encaixando" no deck.
            float fatorEscala = p < 0.82f
                ? Mathf.Lerp(Mathf.Max(0.01f, escalaInicialRelativa), 1.08f, p / 0.82f)
                : Mathf.Lerp(1.08f, 1f, (p - 0.82f) / 0.18f);

            AplicarFrame(player, suave, fatorEscala);
            AplicarFrame(inimigo, suave, fatorEscala);
            yield return null;
        }

        FinalizarCarta(player);
        FinalizarCarta(inimigo);
    }

    private void AplicarFrame(DadosEntradaCarta dados, float suave, float fatorEscala)
    {
        if (dados == null || dados.obj == null || dados.slot == null)
            return;

        dados.obj.transform.position = Vector3.Lerp(dados.posicaoInicial, dados.slot.position, suave);
        dados.obj.transform.rotation = Quaternion.Slerp(dados.rotacaoInicial, Quaternion.identity, suave);
        dados.obj.transform.localScale = dados.escalaFinal * fatorEscala;

        SpriteRenderer sr = dados.obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = suave;
            sr.color = c;
        }
    }

    private void FinalizarCarta(DadosEntradaCarta dados)
    {
        if (dados == null || dados.obj == null || dados.slot == null)
            return;

        dados.obj.transform.position = dados.slot.position;
        dados.obj.transform.rotation = Quaternion.identity;
        dados.obj.transform.localScale = dados.escalaFinal;

        SpriteRenderer sr = dados.obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        if (dados.colliders != null)
        {
            for (int i = 0; i < dados.colliders.Length; i++)
            {
                if (dados.colliders[i] != null)
                    dados.colliders[i].enabled = true;
            }
        }
    }

    private List<Transform> BuscarSlots(string tag)
    {
        List<Transform> resultado = new List<Transform>();
        if (string.IsNullOrEmpty(tag))
            return resultado;

        GameObject[] encontrados = GameObject.FindGameObjectsWithTag(tag);
        for (int i = 0; i < encontrados.Length; i++)
        {
            if (encontrados[i] != null)
                resultado.Add(encontrados[i].transform);
        }

        resultado.Sort((a, b) => a.name.CompareTo(b.name));
        return resultado;
    }
}
