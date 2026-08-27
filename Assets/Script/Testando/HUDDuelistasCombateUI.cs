using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDDuelistasCombateUI : MonoBehaviour
{
    private static HUDDuelistasCombateUI instancia;

    [Header("Cores")]
    [SerializeField] private Color corPainel = new Color(0.04f, 0.05f, 0.08f, 0.9f);
    [SerializeField] private Color corPlayer = new Color(0.25f, 0.75f, 1f, 1f);
    [SerializeField] private Color corInimigo = new Color(1f, 0.32f, 0.38f, 1f);
    [SerializeField] private Color corVidaCheia = new Color(0.25f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color corVidaBaixa = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color corAcaoDisponivel = new Color(1f, 0.82f, 0.22f, 1f);
    [SerializeField] private Color corAcaoGasta = new Color(0.24f, 0.27f, 0.34f, 1f);

    [Header("Posição dos HUDs")]
    [SerializeField] private Vector2 posicaoHUDPlayer = new Vector2(24f, -180f);
    [SerializeField] private Vector2 posicaoHUDInimigo = new Vector2(-24f, -24f);

    [Header("Animação da vida")]
    [SerializeField] private float velocidadeAnimacaoVida = 6f;
    [SerializeField] private float limiteVidaBaixa = 0.25f;

    private Canvas canvas;
    private RectTransform raiz;
    private RectTransform painelPlayer;
    private RectTransform painelInimigo;
    private Image barraPlayer;
    private Image barraInimigo;
    private TMP_Text textoPlayer;
    private TMP_Text textoInimigo;
    private TMP_Text textoContadoresPlayer;
    private TMP_Text textoContadoresInimigo;
    private TMP_Text textoAcoesPlayer;
    private TMP_Text textoAcoesInimigo;
    private TMP_Text textoEstadoCampo;
    private RectTransform containerAcoesPlayer;
    private RectTransform containerAcoesInimigo;
    private readonly List<Image> pipsPlayer = new List<Image>();
    private readonly List<Image> pipsInimigo = new List<Image>();

    private float alvoVidaPlayer = 1f;
    private float alvoVidaInimigo = 1f;
    private float visualVidaPlayer = 1f;
    private float visualVidaInimigo = 1f;
    private int maxAcoesPlayerAnterior = -1;
    private int maxAcoesInimigoAnterior = -1;

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

    private void Update()
    {
        if (canvas == null)
            return;

        visualVidaPlayer = Mathf.Lerp(visualVidaPlayer, alvoVidaPlayer, 1f - Mathf.Exp(-velocidadeAnimacaoVida * Time.unscaledDeltaTime));
        visualVidaInimigo = Mathf.Lerp(visualVidaInimigo, alvoVidaInimigo, 1f - Mathf.Exp(-velocidadeAnimacaoVida * Time.unscaledDeltaTime));

        AtualizarBarraVisual(barraPlayer, visualVidaPlayer);
        AtualizarBarraVisual(barraInimigo, visualVidaInimigo);
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
        AtualizarPainel(textoPlayer, nome, atual, maximo, true);
        if (textoContadoresPlayer != null && string.IsNullOrEmpty(textoContadoresPlayer.text))
            textoContadoresPlayer.text = $"Res {Mathf.Max(0, reserva)}";
    }

    public void AtualizarInimigo(string nome, int atual, int maximo, int reserva)
    {
        CriarInterfaceSeNecessario();
        AtualizarPainel(textoInimigo, nome, atual, maximo, false);
        if (textoContadoresInimigo != null && string.IsNullOrEmpty(textoContadoresInimigo.text))
            textoContadoresInimigo.text = $"Res {Mathf.Max(0, reserva)}";
    }

    public void AtualizarContadores(int deckPlayer, int reservaPlayer, int cemiterioPlayer,
        int deckInimigo, int reservaInimigo, int cemiterioInimigo)
    {
        CriarInterfaceSeNecessario();
        if (textoContadoresPlayer != null)
            textoContadoresPlayer.text = $"Deck {Mathf.Max(0, deckPlayer)}  •  Res {Mathf.Max(0, reservaPlayer)}  •  Cem {Mathf.Max(0, cemiterioPlayer)}";
        if (textoContadoresInimigo != null)
            textoContadoresInimigo.text = $"Deck {Mathf.Max(0, deckInimigo)}  •  Res {Mathf.Max(0, reservaInimigo)}  •  Cem {Mathf.Max(0, cemiterioInimigo)}";
    }

    public void AtualizarAcoes(int acoesPlayer, int maxPlayer, int acoesInimigo, int maxInimigo)
    {
        CriarInterfaceSeNecessario();
        maxPlayer = Mathf.Max(1, maxPlayer);
        maxInimigo = Mathf.Max(1, maxInimigo);

        if (textoAcoesPlayer != null)
            textoAcoesPlayer.text = "AÇÕES";
        if (textoAcoesInimigo != null)
            textoAcoesInimigo.text = "AÇÕES";

        if (maxAcoesPlayerAnterior != maxPlayer)
        {
            RecriarPips(containerAcoesPlayer, pipsPlayer, maxPlayer);
            maxAcoesPlayerAnterior = maxPlayer;
        }
        if (maxAcoesInimigoAnterior != maxInimigo)
        {
            RecriarPips(containerAcoesInimigo, pipsInimigo, maxInimigo);
            maxAcoesInimigoAnterior = maxInimigo;
        }

        AtualizarPips(pipsPlayer, Mathf.Max(0, acoesPlayer));
        AtualizarPips(pipsInimigo, Mathf.Max(0, acoesInimigo));
    }

    public void AtualizarEstadoCampo(bool playerPossuiCartaNoCampo, bool inimigoPossuiCartaNoCampo)
    {
        CriarInterfaceSeNecessario();
        if (textoEstadoCampo == null)
            return;

        if (!playerPossuiCartaNoCampo && !inimigoPossuiCartaNoCampo)
            textoEstadoCampo.text = "CAMPOS ABERTOS";
        else if (!inimigoPossuiCartaNoCampo)
            textoEstadoCampo.text = "CAMPO INIMIGO ABERTO";
        else if (!playerPossuiCartaNoCampo)
            textoEstadoCampo.text = "SEU CAMPO ESTÁ ABERTO";
        else
            textoEstadoCampo.text = "";
    }

    public void AnimarDanoNoPlayer(int dano)
    {
        CriarInterfaceSeNecessario();
        if (painelPlayer != null)
            StartCoroutine(AnimarDano(painelPlayer, dano));
    }

    public void AnimarDanoNoInimigo(int dano)
    {
        CriarInterfaceSeNecessario();
        if (painelInimigo != null)
            StartCoroutine(AnimarDano(painelInimigo, dano));
    }

    public void AnimarAtaqueDiretoNoPlayer()
    {
        if (painelPlayer != null)
            StartCoroutine(AnimarImpactoPainel(painelPlayer));
    }

    public void AnimarAtaqueDiretoNoInimigo()
    {
        if (painelInimigo != null)
            StartCoroutine(AnimarImpactoPainel(painelInimigo));
    }

    public void MostrarMensagemTemporaria(string mensagem, float duracao = 1.4f)
    {
        NotificacaoJogoUI notificacao = NotificacaoJogoUI.ObterOuCriar();
        if (notificacao != null)
            notificacao.MostrarMensagem(mensagem, duracao);
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
            out textoPlayer, out textoContadoresPlayer, out textoAcoesPlayer, out containerAcoesPlayer, out barraPlayer);
        painelInimigo = CriarPainelDuelista("PainelInimigo", raiz, posicaoHUDInimigo, true,
            out textoInimigo, out textoContadoresInimigo, out textoAcoesInimigo, out containerAcoesInimigo, out barraInimigo);

        textoEstadoCampo = CriarTexto("EstadoCampo", raiz, "", 16f, new Color(0.9f, 0.93f, 1f, 0.95f),
            new Vector2(420f, 28f), new Vector2(0f, -42f), TextAlignmentOptions.Center);
        textoEstadoCampo.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        textoEstadoCampo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        textoEstadoCampo.rectTransform.pivot = new Vector2(0.5f, 1f);
    }

    private RectTransform CriarPainelDuelista(string nome, RectTransform parent, Vector2 margem, bool direita,
        out TMP_Text textoVida, out TMP_Text textoContadores, out TMP_Text textoAcoes, out RectTransform containerAcoes, out Image barra)
    {
        GameObject painelObj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = painelObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(360f, 136f);
        rt.anchorMin = direita ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = direita ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rt.anchoredPosition = margem;

        Image fundo = painelObj.GetComponent<Image>();
        fundo.color = corPainel;
        fundo.raycastTarget = false;

        textoVida = CriarTexto("Vida", rt, "", 22f, Color.white,
            new Vector2(330f, 34f), new Vector2(0f, 45f), TextAlignmentOptions.Center);
        textoVida.enableAutoSizing = true;
        textoVida.fontSizeMin = 15f;
        textoVida.fontSizeMax = 22f;
        textoVida.enableWordWrapping = false;

        RectTransform trilho = CriarImagem("TrilhoVida", rt, new Color(0f, 0f, 0f, 0.6f),
            new Vector2(300f, 16f), new Vector2(0f, 17f));
        RectTransform barraRT = CriarImagem("BarraVida", trilho, corVidaCheia, new Vector2(300f, 16f), Vector2.zero);
        barraRT.anchorMin = new Vector2(0f, 0.5f);
        barraRT.anchorMax = new Vector2(0f, 0.5f);
        barraRT.pivot = new Vector2(0f, 0.5f);
        barra = barraRT.GetComponent<Image>();

        textoContadores = CriarTexto("Contadores", rt, "", 14f, new Color(0.82f, 0.86f, 0.94f, 1f),
            new Vector2(320f, 24f), new Vector2(0f, -8f), TextAlignmentOptions.Center);
        textoContadores.enableWordWrapping = false;

        textoAcoes = CriarTexto("AcoesTitulo", rt, "AÇÕES", 12f, new Color(1f, 0.86f, 0.4f, 1f),
            new Vector2(70f, 20f), new Vector2(-105f, -39f), TextAlignmentOptions.Center);

        GameObject contObj = new GameObject("PipsAcoes", typeof(RectTransform));
        containerAcoes = contObj.GetComponent<RectTransform>();
        containerAcoes.SetParent(rt, false);
        containerAcoes.sizeDelta = new Vector2(190f, 22f);
        containerAcoes.anchoredPosition = new Vector2(35f, -39f);

        return rt;
    }

    private void AtualizarPainel(TMP_Text textoVida, string nome, int atual, int maximo, bool player)
    {
        maximo = Mathf.Max(1, maximo);
        atual = Mathf.Clamp(atual, 0, maximo);
        float proporcao = (float)atual / maximo;

        if (textoVida != null)
            textoVida.text = $"<b>{nome}</b>   {atual}/{maximo} VIDA";

        if (player)
            alvoVidaPlayer = proporcao;
        else
            alvoVidaInimigo = proporcao;
    }

    private void AtualizarBarraVisual(Image barra, float proporcao)
    {
        if (barra == null)
            return;
        proporcao = Mathf.Clamp01(proporcao);
        barra.rectTransform.sizeDelta = new Vector2(300f * proporcao, 16f);
        Color baseCor = Color.Lerp(corVidaBaixa, corVidaCheia, proporcao);
        if (proporcao <= limiteVidaBaixa && proporcao > 0f)
        {
            float pulso = 0.72f + 0.28f * (Mathf.Sin(Time.unscaledTime * 7f) * 0.5f + 0.5f);
            baseCor = new Color(baseCor.r, baseCor.g, baseCor.b, pulso);
        }
        barra.color = baseCor;
    }

    private void RecriarPips(RectTransform container, List<Image> lista, int quantidade)
    {
        if (container == null)
            return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
        lista.Clear();

        float tamanho = 14f;
        float espaco = 8f;
        float total = quantidade * tamanho + Mathf.Max(0, quantidade - 1) * espaco;
        float inicio = -total * 0.5f + tamanho * 0.5f;
        for (int i = 0; i < quantidade; i++)
        {
            RectTransform rt = CriarImagem("Acao" + (i + 1), container, corAcaoGasta, new Vector2(tamanho, tamanho),
                new Vector2(inicio + i * (tamanho + espaco), 0f));
            lista.Add(rt.GetComponent<Image>());
        }
    }

    private void AtualizarPips(List<Image> lista, int disponiveis)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] != null)
                lista[i].color = i < disponiveis ? corAcaoDisponivel : corAcaoGasta;
        }
    }

    private IEnumerator AnimarDano(RectTransform painel, int dano)
    {
        if (painel == null)
            yield break;

        TMP_Text texto = CriarTexto("DanoFlutuante", painel, $"-{Mathf.Max(0, dano)}", 28f,
            new Color(1f, 0.35f, 0.35f, 1f), new Vector2(120f, 42f), new Vector2(0f, -78f), TextAlignmentOptions.Center);
        texto.fontStyle = FontStyles.Bold;
        RectTransform rt = texto.rectTransform;
        Vector2 inicio = rt.anchoredPosition;
        Vector2 fim = inicio + new Vector2(0f, 55f);
        StartCoroutine(AnimarImpactoPainel(painel));

        float t = 0f;
        while (t < 0.62f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.62f);
            rt.anchoredPosition = Vector2.Lerp(inicio, fim, p);
            Color c = texto.color;
            c.a = 1f - p;
            texto.color = c;
            yield return null;
        }
        if (texto != null)
            Destroy(texto.gameObject);
    }

    private IEnumerator AnimarImpactoPainel(RectTransform painel)
    {
        if (painel == null)
            yield break;

        Vector2 origem = painel.anchoredPosition;
        Image flash = CriarImagem("FlashImpacto", painel, new Color(1f, 1f, 1f, 0.34f), painel.sizeDelta, Vector2.zero).GetComponent<Image>();
        flash.transform.SetAsLastSibling();

        float t = 0f;
        const float duracao = 0.28f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float intensidade = (1f - p) * 8f;
            painel.anchoredPosition = origem + new Vector2(Random.Range(-intensidade, intensidade), Random.Range(-intensidade, intensidade));
            Color c = flash.color;
            c.a = 0.34f * (1f - p);
            flash.color = c;
            yield return null;
        }
        painel.anchoredPosition = origem;
        if (flash != null)
            Destroy(flash.gameObject);
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
        tmp.enableWordWrapping = false;
        return tmp;
    }
}
