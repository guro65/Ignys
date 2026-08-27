using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDDuelistasCombateUI : MonoBehaviour
{
    private static HUDDuelistasCombateUI instancia;

    [Header("Cores")]
    [SerializeField] private Color corPainel = new Color(0.04f, 0.05f, 0.08f, 0.88f);
    [SerializeField] private Color corPlayer = new Color(0.25f, 0.75f, 1f, 1f);
    [SerializeField] private Color corInimigo = new Color(1f, 0.32f, 0.38f, 1f);
    [SerializeField] private Color corVidaCheia = new Color(0.25f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color corVidaBaixa = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Posição dos HUDs")]
    [SerializeField] private Vector2 posicaoHUDPlayer = new Vector2(30f, -180f);
    [SerializeField] private Vector2 posicaoHUDInimigo = new Vector2(-30f, -30f);

    private Canvas canvas;
    private RectTransform raiz;
    private RectTransform painelPlayer;
    private RectTransform painelInimigo;
    private Image barraPlayer;
    private Image barraInimigo;
    private TMP_Text textoPlayer;
    private TMP_Text textoInimigo;
    private TMP_Text textoReservaPlayer;
    private TMP_Text textoReservaInimigo;
    private TMP_Text textoAcoesPlayer;
    private TMP_Text textoAcoesInimigo;
    private TMP_Text textoEstadoCampo;
    private int mensagemTemporariaId = 0;

    public static HUDDuelistasCombateUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<HUDDuelistasCombateUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("HUDDuelistasCombateUI");
        instancia = obj.AddComponent<HUDDuelistasCombateUI>();
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

    public void Configurar(string nomePlayer, int vidaAtualPlayer, int vidaMaximaPlayer, int reservaPlayer,
        string nomeInimigo, int vidaAtualInimigo, int vidaMaximaInimigo, int reservaInimigo)
    {
        CriarInterfaceSeNecessario();

        AtualizarPlayer(nomePlayer, vidaAtualPlayer, vidaMaximaPlayer, reservaPlayer);
        AtualizarInimigo(nomeInimigo, vidaAtualInimigo, vidaMaximaInimigo, reservaInimigo);
    }

    public void AtualizarPlayer(string nome, int atual, int maximo, int reserva)
    {
        CriarInterfaceSeNecessario();
        AtualizarPainel(textoPlayer, textoReservaPlayer, barraPlayer, nome, atual, maximo, reserva, corPlayer);
    }

    public void AtualizarInimigo(string nome, int atual, int maximo, int reserva)
    {
        CriarInterfaceSeNecessario();
        AtualizarPainel(textoInimigo, textoReservaInimigo, barraInimigo, nome, atual, maximo, reserva, corInimigo);
    }

    public void AtualizarAcoes(int acoesPlayer, int maxPlayer, int acoesInimigo, int maxInimigo)
    {
        CriarInterfaceSeNecessario();

        if (textoAcoesPlayer != null)
            textoAcoesPlayer.text = $"Ações: {Mathf.Max(0, acoesPlayer)}/{Mathf.Max(1, maxPlayer)}";

        if (textoAcoesInimigo != null)
            textoAcoesInimigo.text = $"Ações: {Mathf.Max(0, acoesInimigo)}/{Mathf.Max(1, maxInimigo)}";
    }

    public void AtualizarEstadoCampo(bool playerPossuiCartaNoCampo, bool inimigoPossuiCartaNoCampo)
    {
        CriarInterfaceSeNecessario();
        if (textoEstadoCampo == null)
            return;

        if (!playerPossuiCartaNoCampo && !inimigoPossuiCartaNoCampo)
            textoEstadoCampo.text = "OS DOIS CAMPOS ESTÃO ABERTOS";
        else if (!inimigoPossuiCartaNoCampo)
            textoEstadoCampo.text = "CAMPO INIMIGO ABERTO — seus ataques atingem o oponente";
        else if (!playerPossuiCartaNoCampo)
            textoEstadoCampo.text = "SEU CAMPO ESTÁ ABERTO — proteja o duelista";
        else
            textoEstadoCampo.text = "";
    }

    public void AnimarDanoNoPlayer(int dano)
    {
        CriarInterfaceSeNecessario();
        if (painelPlayer != null)
            StartCoroutine(AnimarDano(painelPlayer, dano, corPlayer));
    }

    public void AnimarDanoNoInimigo(int dano)
    {
        CriarInterfaceSeNecessario();
        if (painelInimigo != null)
            StartCoroutine(AnimarDano(painelInimigo, dano, corInimigo));
    }

    public void MostrarMensagemTemporaria(string mensagem, float duracao = 1.4f)
    {
        CriarInterfaceSeNecessario();
        if (textoEstadoCampo == null)
            return;

        mensagemTemporariaId++;
        int id = mensagemTemporariaId;
        StartCoroutine(RotinaMensagem(mensagem, duracao, id));
    }

    public void Ocultar()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void CriarInterfaceSeNecessario()
    {
        if (canvas != null)
            return;

        GameObject canvasObj = new GameObject("Canvas_HUDDuelistas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2500;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz = canvasObj.GetComponent<RectTransform>();

        painelPlayer = CriarPainelDuelista("PainelPlayer", raiz, posicaoHUDPlayer, false,
            out textoPlayer, out textoReservaPlayer, out textoAcoesPlayer, out barraPlayer);

        painelInimigo = CriarPainelDuelista("PainelInimigo", raiz, posicaoHUDInimigo, true,
            out textoInimigo, out textoReservaInimigo, out textoAcoesInimigo, out barraInimigo);

        textoEstadoCampo = CriarTexto("EstadoCampo", raiz, "", 26f, Color.white,
            new Vector2(1000f, 70f), new Vector2(0f, -38f), TextAlignmentOptions.Center);
        textoEstadoCampo.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        textoEstadoCampo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        textoEstadoCampo.rectTransform.pivot = new Vector2(0.5f, 1f);
    }

    private RectTransform CriarPainelDuelista(string nome, RectTransform parent, Vector2 margem, bool direita,
        out TMP_Text textoVida, out TMP_Text textoReserva, out TMP_Text textoAcoes, out Image barra)
    {
        GameObject painelObj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = painelObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(410f, 150f);
        rt.anchorMin = direita ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = direita ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rt.anchoredPosition = margem;

        Image fundo = painelObj.GetComponent<Image>();
        fundo.color = corPainel;
        fundo.raycastTarget = false;

        textoVida = CriarTexto("Vida", rt, "", 27f, Color.white,
            new Vector2(370f, 42f), new Vector2(0f, 32f), TextAlignmentOptions.Center);

        RectTransform trilho = CriarImagem("TrilhoVida", rt, new Color(0f, 0f, 0f, 0.6f),
            new Vector2(350f, 22f), new Vector2(0f, -7f));
        trilho.GetComponent<Image>().raycastTarget = false;

        RectTransform barraRT = CriarImagem("BarraVida", trilho, corVidaCheia,
            new Vector2(350f, 22f), Vector2.zero);
        barraRT.anchorMin = new Vector2(0f, 0.5f);
        barraRT.anchorMax = new Vector2(0f, 0.5f);
        barraRT.pivot = new Vector2(0f, 0.5f);
        barra = barraRT.GetComponent<Image>();
        barra.raycastTarget = false;

        textoReserva = CriarTexto("Reserva", rt, "Reserva: 0", 18f, new Color(0.86f, 0.9f, 1f, 1f),
            new Vector2(350f, 26f), new Vector2(0f, -39f), TextAlignmentOptions.Center);

        textoAcoes = CriarTexto("Acoes", rt, "Ações: 0/3", 18f, new Color(1f, 0.86f, 0.4f, 1f),
            new Vector2(350f, 26f), new Vector2(0f, -66f), TextAlignmentOptions.Center);

        return rt;
    }

    private void AtualizarPainel(TMP_Text textoVida, TMP_Text textoReserva, Image barra, string nome,
        int atual, int maximo, int reserva, Color corNome)
    {
        maximo = Mathf.Max(1, maximo);
        atual = Mathf.Clamp(atual, 0, maximo);

        if (textoVida != null)
        {
            textoVida.text = $"<b>{nome}</b>   {atual}/{maximo} VIDA";
            textoVida.color = Color.white;
        }

        if (textoReserva != null)
            textoReserva.text = $"Reserva: {Mathf.Max(0, reserva)} carta(s)";

        if (barra != null)
        {
            float proporcao = (float)atual / maximo;
            RectTransform rt = barra.rectTransform;
            rt.sizeDelta = new Vector2(350f * proporcao, 22f);
            barra.color = Color.Lerp(corVidaBaixa, corVidaCheia, proporcao);
        }
    }

    private IEnumerator AnimarDano(RectTransform painel, int dano, Color corBase)
    {
        if (painel == null)
            yield break;

        TMP_Text texto = CriarTexto("DanoFlutuante", painel, $"-{Mathf.Max(0, dano)}", 42f,
            new Color(1f, 0.3f, 0.3f, 1f), new Vector2(180f, 70f), new Vector2(0f, -80f), TextAlignmentOptions.Center);
        RectTransform rt = texto.rectTransform;
        Vector2 inicio = rt.anchoredPosition;
        Vector2 fim = inicio + new Vector2(0f, 85f);

        float duracao = 0.7f;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            rt.anchoredPosition = Vector2.Lerp(inicio, fim, p);
            rt.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, Mathf.Sin(p * Mathf.PI));
            Color c = texto.color;
            c.a = 1f - p;
            texto.color = c;
            yield return null;
        }

        if (texto != null)
            Destroy(texto.gameObject);
    }

    private IEnumerator RotinaMensagem(string mensagem, float duracao, int id)
    {
        textoEstadoCampo.text = mensagem;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duracao));

        if (id == mensagemTemporariaId && textoEstadoCampo != null)
            textoEstadoCampo.text = "";
    }

    private RectTransform CriarImagem(string nome, Transform parent, Color cor, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;
        Image image = obj.GetComponent<Image>();
        image.color = cor;
        image.raycastTarget = false;
        return rt;
    }

    private TMP_Text CriarTexto(string nome, Transform parent, string texto, float tamanhoFonte, Color cor,
        Vector2 tamanho, Vector2 posicao, TextAlignmentOptions alinhamento)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanhoFonte;
        tmp.color = cor;
        tmp.alignment = alinhamento;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }
}
