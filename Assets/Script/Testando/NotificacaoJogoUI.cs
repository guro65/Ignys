using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificacaoJogoUI : MonoBehaviour
{
    private static NotificacaoJogoUI instancia;

    [Header("Tamanhos compactos")]
    [SerializeField] private float tamanhoFonteNotificacao = 18f;
    [SerializeField] private float tamanhoFonteBanner = 28f;
    [SerializeField] private float tamanhoFonteConfirmacao = 18f;

    [Header("Cores")]
    [SerializeField] private Color corFundo = new Color(0.035f, 0.045f, 0.075f, 0.94f);
    [SerializeField] private Color corPlayer = new Color(0.25f, 0.72f, 1f, 1f);
    [SerializeField] private Color corInimigo = new Color(1f, 0.32f, 0.38f, 1f);
    [SerializeField] private Color corAviso = new Color(1f, 0.72f, 0.22f, 1f);

    private Canvas canvas;
    private RectTransform raiz;
    private GameObject notificacaoAtual;
    private GameObject bannerAtual;
    private GameObject confirmacaoAtual;
    private int idNotificacao;

    public static NotificacaoJogoUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<NotificacaoJogoUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("NotificacaoJogoUI");
        instancia = obj.AddComponent<NotificacaoJogoUI>();
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

    public void MostrarMensagem(string mensagem, float duracao = 1.35f)
    {
        MostrarMensagem(mensagem, corAviso, duracao);
    }

    public void MostrarMensagem(string mensagem, Color corDestaque, float duracao = 1.35f)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            return;

        CriarCanvasSeNecessario();
        idNotificacao++;
        int id = idNotificacao;

        if (notificacaoAtual != null)
            Destroy(notificacaoAtual);

        RectTransform painel = CriarPainel("NotificacaoCompacta", raiz, new Vector2(520f, 54f), new Vector2(0f, -122f), corFundo);
        painel.anchorMin = new Vector2(0.5f, 1f);
        painel.anchorMax = painel.anchorMin;
        painel.pivot = new Vector2(0.5f, 1f);

        RectTransform faixa = CriarImagem("Faixa", painel, corDestaque, new Vector2(5f, 42f), new Vector2(-247f, 0f));
        faixa.GetComponent<Image>().raycastTarget = false;

        TMP_Text texto = CriarTexto("Texto", painel, mensagem, tamanhoFonteNotificacao, Color.white,
            new Vector2(485f, 46f), new Vector2(7f, 0f), TextAlignmentOptions.Center);
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 11f;
        texto.fontSizeMax = tamanhoFonteNotificacao;
        texto.enableWordWrapping = false;
        texto.overflowMode = TextOverflowModes.Ellipsis;

        CanvasGroup grupo = painel.gameObject.AddComponent<CanvasGroup>();
        notificacaoAtual = painel.gameObject;
        StartCoroutine(AnimarNotificacao(painel, grupo, duracao, id));
    }

    public void MostrarBannerTurno(bool turnoPlayer)
    {
        CriarCanvasSeNecessario();

        if (bannerAtual != null)
            Destroy(bannerAtual);

        Color cor = turnoPlayer ? corPlayer : corInimigo;
        string texto = turnoPlayer ? "SEU TURNO" : "TURNO DO OPONENTE";

        RectTransform painel = CriarPainel("BannerTurno", raiz, new Vector2(420f, 68f), Vector2.zero,
            new Color(cor.r * 0.18f, cor.g * 0.18f, cor.b * 0.18f, 0.96f));
        CriarImagem("FaixaSuperior", painel, cor, new Vector2(390f, 4f), new Vector2(0f, 27f));
        CriarImagem("FaixaInferior", painel, cor, new Vector2(390f, 4f), new Vector2(0f, -27f));
        CriarTexto("Texto", painel, texto, tamanhoFonteBanner, Color.white,
            new Vector2(390f, 54f), Vector2.zero, TextAlignmentOptions.Center);

        CanvasGroup grupo = painel.gameObject.AddComponent<CanvasGroup>();
        bannerAtual = painel.gameObject;
        StartCoroutine(AnimarBanner(painel, grupo));
    }

    public IEnumerator MostrarApresentacaoDuelo(string nomePlayer, string nomeInimigo)
    {
        CriarCanvasSeNecessario();

        string player = string.IsNullOrWhiteSpace(nomePlayer) ? "PLAYER" : nomePlayer;
        string inimigo = string.IsNullOrWhiteSpace(nomeInimigo) ? "OPONENTE" : nomeInimigo;

        RectTransform painel = CriarPainel("ApresentacaoDuelo", raiz, new Vector2(620f, 150f), Vector2.zero, corFundo);
        CanvasGroup grupo = painel.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        painel.localScale = Vector3.one * 0.82f;

        TMP_Text texto = CriarTexto("Texto", painel,
            $"<b>{player}</b>   <color=#FFD75A>VS</color>   <b>{inimigo}</b>",
            28f, Color.white, new Vector2(570f, 75f), new Vector2(0f, 10f), TextAlignmentOptions.Center);
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 18f;
        texto.fontSizeMax = 28f;
        CriarTexto("Sub", painel, "PREPARE O DUELO", 15f, new Color(0.75f, 0.8f, 0.9f, 1f),
            new Vector2(500f, 32f), new Vector2(0f, -43f), TextAlignmentOptions.Center);

        float t = 0f;
        while (t < 0.24f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.24f);
            grupo.alpha = p;
            painel.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, 1f - Mathf.Pow(1f - p, 3f));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.65f);

        t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Mathf.Clamp01(t / 0.2f);
            yield return null;
        }

        if (painel != null)
            Destroy(painel.gameObject);
    }

    public void ConfirmarPassarTurno(int acoesRestantes, Action aoConfirmar, Action aoCancelar = null)
    {
        CriarCanvasSeNecessario();

        if (confirmacaoAtual != null)
            return;

        GameObject bloqueadorObj = new GameObject("ConfirmacaoPassarTurno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform bloqueador = bloqueadorObj.GetComponent<RectTransform>();
        bloqueador.SetParent(raiz, false);
        Esticar(bloqueador);
        Image fundoBloqueador = bloqueadorObj.GetComponent<Image>();
        fundoBloqueador.color = new Color(0f, 0f, 0f, 0.38f);
        fundoBloqueador.raycastTarget = true;
        confirmacaoAtual = bloqueadorObj;

        RectTransform janela = CriarPainel("Janela", bloqueador, new Vector2(440f, 176f), Vector2.zero, corFundo);
        CriarTexto("Titulo", janela, "PASSAR O TURNO?", 21f, Color.white,
            new Vector2(390f, 34f), new Vector2(0f, 54f), TextAlignmentOptions.Center);
        CriarTexto("Mensagem", janela, $"Ainda restam {Mathf.Max(0, acoesRestantes)} ação(ões).",
            tamanhoFonteConfirmacao, new Color(0.82f, 0.86f, 0.94f, 1f),
            new Vector2(390f, 34f), new Vector2(0f, 13f), TextAlignmentOptions.Center);

        Button confirmar = CriarBotao("Confirmar", janela, "PASSAR", new Vector2(150f, 48f), new Vector2(-90f, -48f), corAviso);
        Button voltar = CriarBotao("Voltar", janela, "VOLTAR", new Vector2(150f, 48f), new Vector2(90f, -48f), new Color(0.25f, 0.3f, 0.42f, 1f));

        confirmar.onClick.AddListener(() =>
        {
            FecharConfirmacao();
            aoConfirmar?.Invoke();
        });
        voltar.onClick.AddListener(() =>
        {
            FecharConfirmacao();
            aoCancelar?.Invoke();
        });

        StartCoroutine(AnimarEntradaCurta(janela));
    }

    public void FecharConfirmacao()
    {
        if (confirmacaoAtual != null)
            Destroy(confirmacaoAtual);
        confirmacaoAtual = null;
    }

    private void CriarCanvasSeNecessario()
    {
        if (canvas != null)
            return;

        GameObject canvasObj = new GameObject("Canvas_NotificacoesJogo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5200;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz = canvasObj.GetComponent<RectTransform>();
    }

    private IEnumerator AnimarNotificacao(RectTransform painel, CanvasGroup grupo, float duracao, int id)
    {
        grupo.alpha = 0f;
        Vector2 destino = painel.anchoredPosition;
        Vector2 inicio = destino + new Vector2(0f, 18f);
        painel.anchoredPosition = inicio;

        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.16f);
            grupo.alpha = p;
            painel.anchoredPosition = Vector2.Lerp(inicio, destino, p);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, duracao - 0.32f));

        t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.16f);
            grupo.alpha = 1f - p;
            yield return null;
        }

        if (id == idNotificacao && painel != null)
        {
            Destroy(painel.gameObject);
            notificacaoAtual = null;
        }
    }

    private IEnumerator AnimarBanner(RectTransform painel, CanvasGroup grupo)
    {
        grupo.alpha = 0f;
        painel.localScale = new Vector3(0.82f, 1f, 1f);
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.18f);
            grupo.alpha = p;
            painel.localScale = new Vector3(Mathf.Lerp(0.82f, 1f, p), 1f, 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.52f);

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Mathf.Clamp01(t / 0.18f);
            yield return null;
        }

        if (painel != null)
            Destroy(painel.gameObject);
        bannerAtual = null;
    }

    private IEnumerator AnimarEntradaCurta(RectTransform rt)
    {
        rt.localScale = Vector3.one * 0.86f;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.18f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, 1f - Mathf.Pow(1f - p, 3f));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private RectTransform CriarPainel(string nome, Transform parent, Vector2 tamanho, Vector2 posicao, Color cor)
    {
        return CriarImagem(nome, parent, cor, tamanho, posicao);
    }

    private RectTransform CriarImagem(string nome, Transform parent, Color cor, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;
        Image img = obj.GetComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
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

    private Button CriarBotao(string nome, Transform parent, string texto, Vector2 tamanho, Vector2 posicao, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;
        Image img = obj.GetComponent<Image>();
        img.color = cor;
        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;
        CriarTexto("Texto", rt, texto, 16f, Color.white, tamanho, Vector2.zero, TextAlignmentOptions.Center);
        return btn;
    }

    private void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
