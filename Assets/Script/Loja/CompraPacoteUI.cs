using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompraPacoteUI : MonoBehaviour
{
    private static CompraPacoteUI instancia;
    private static bool compraEmAndamentoGlobal;

    [Header("Duração da apresentação")]
    [SerializeField, Min(0.05f)] private float duracaoEntrada = 0.34f;
    [SerializeField, Min(0.05f)] private float duracaoContagemOrbs = 0.42f;
    [SerializeField, Min(0.05f)] private float duracaoSelamento = 0.30f;
    [SerializeField, Min(0f)] private float tempoExibicaoPacote = 0.48f;
    [SerializeField, Min(0.05f)] private float duracaoVooInventario = 0.52f;

    [Header("Tamanho visual")]
    [SerializeField] private Vector2 tamanhoMaximoPacote = new Vector2(370f, 540f);
    [SerializeField, Range(0.05f, 0.5f)] private float escalaFinalNoInventario = 0.13f;

    [Header("Cores")]
    [SerializeField] private Color corFundo = new Color(0.01f, 0.012f, 0.025f, 0.83f);
    [SerializeField] private Color corTextoPrincipal = Color.white;
    [SerializeField] private Color corOrbs = new Color(0.55f, 0.85f, 1f, 1f);
    [SerializeField] private Color corFalha = new Color(1f, 0.3f, 0.35f, 1f);

    private Canvas canvas;
    private RectTransform raiz;
    private RectTransform pacoteVisual;
    private CanvasGroup grupoPacote;
    private RectTransform brilho;
    private Image imagemPacote;
    private RectTransform selo;
    private TMP_Text textoSelo;
    private TMP_Text textoAdquirido;
    private TMP_Text textoOrbs;
    private TMP_Text textoPreco;
    private RectTransform alvoInventario;
    private Vector3 escalaOriginalAlvoInventario;

    public static CompraPacoteUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<CompraPacoteUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("CompraPacoteUI");
        instancia = obj.AddComponent<CompraPacoteUI>();
        return instancia;
    }

    public static bool ExisteCompraEmAndamento()
    {
        return compraEmAndamentoGlobal;
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

        compraEmAndamentoGlobal = false;
    }

    public void IniciarCompra(PacoteAdquirido pacote, int preco, int orbsAntes, int orbsDepois)
    {
        if (pacote == null || compraEmAndamentoGlobal)
            return;

        StartCoroutine(RotinaCompra(pacote, Mathf.Max(0, preco), Mathf.Max(0, orbsAntes), Mathf.Max(0, orbsDepois)));
    }

    public void MostrarFalhaCompra(string mensagem)
    {
        if (compraEmAndamentoGlobal)
            return;

        StartCoroutine(RotinaFalha(string.IsNullOrWhiteSpace(mensagem) ? "COMPRA NÃO REALIZADA" : mensagem));
    }

    private IEnumerator RotinaCompra(PacoteAdquirido pacote, int preco, int orbsAntes, int orbsDepois)
    {
        compraEmAndamentoGlobal = true;
        CriarInterfaceBase();
        CriarPacoteVisual(pacote);
        alvoInventario = ProcurarAlvoInventarioAutomaticamente();

        if (alvoInventario != null)
            escalaOriginalAlvoInventario = alvoInventario.localScale;

        yield return AnimarEntradaPacote(pacote);
        yield return AnimarPagamento(preco, orbsAntes, orbsDepois, pacote.corSecundariaPacote);
        yield return AnimarSelamento(pacote.corSecundariaPacote, pacote.corTextoPacote);

        textoAdquirido.text = "PACOTE ADQUIRIDO";
        yield return AnimarTextoRapido(textoAdquirido, 0.24f);
        yield return new WaitForSecondsRealtime(tempoExibicaoPacote);

        yield return AnimarVooParaInventario(pacote.corSecundariaPacote);

        if (canvas != null)
            Destroy(canvas.gameObject);

        LimparReferencias();
        compraEmAndamentoGlobal = false;
    }

    private IEnumerator RotinaFalha(string mensagem)
    {
        compraEmAndamentoGlobal = true;
        CriarInterfaceBase();

        TMP_Text texto = CriarTexto("Falha", raiz, mensagem, 31f, corFalha,
            new Vector2(760f, 80f), Vector2.zero, TextAlignmentOptions.Center);

        texto.alpha = 0f;
        texto.rectTransform.localScale = Vector3.one * 0.78f;

        float t = 0f;
        const float entrada = 0.18f;
        while (t < entrada)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / entrada);
            texto.alpha = p;
            texto.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, SuavizarSaida(p));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.65f);

        t = 0f;
        const float saida = 0.20f;
        while (t < saida)
        {
            t += Time.unscaledDeltaTime;
            texto.alpha = 1f - Mathf.Clamp01(t / saida);
            yield return null;
        }

        if (canvas != null)
            Destroy(canvas.gameObject);

        LimparReferencias();
        compraEmAndamentoGlobal = false;
    }

    private void CriarInterfaceBase()
    {
        GameObject canvasObj = new GameObject("Canvas_CompraPacote", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5700;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz = canvasObj.GetComponent<RectTransform>();

        RectTransform fundo = CriarImagem("FundoBloqueador", raiz, corFundo, Vector2.zero, Vector2.zero);
        Esticar(fundo);
        fundo.GetComponent<Image>().raycastTarget = true;

        textoOrbs = CriarTexto("ContadorOrbs", raiz, "", 24f, corOrbs,
            new Vector2(440f, 48f), new Vector2(0f, 430f), TextAlignmentOptions.Center);

        textoPreco = CriarTexto("Preco", raiz, "", 21f, Color.white,
            new Vector2(440f, 42f), new Vector2(0f, 385f), TextAlignmentOptions.Center);

        textoAdquirido = CriarTexto("PacoteAdquirido", raiz, "", 30f, corTextoPrincipal,
            new Vector2(650f, 62f), new Vector2(0f, -355f), TextAlignmentOptions.Center);
        textoAdquirido.alpha = 0f;

        // Brilho grande atrás da embalagem.
        brilho = CriarImagem("Brilho", raiz, new Color(1f, 1f, 1f, 0.08f), new Vector2(560f, 700f), new Vector2(0f, 15f));
        brilho.localEulerAngles = new Vector3(0f, 0f, 45f);
        brilho.GetComponent<Image>().raycastTarget = false;
    }

    private void CriarPacoteVisual(PacoteAdquirido pacote)
    {
        GameObject wrapper = new GameObject("PacoteVisual", typeof(RectTransform), typeof(CanvasGroup));
        pacoteVisual = wrapper.GetComponent<RectTransform>();
        pacoteVisual.SetParent(raiz, false);
        pacoteVisual.sizeDelta = tamanhoMaximoPacote;
        pacoteVisual.anchoredPosition = new Vector2(0f, 15f);

        grupoPacote = wrapper.GetComponent<CanvasGroup>();
        grupoPacote.alpha = 0f;
        grupoPacote.blocksRaycasts = false;
        grupoPacote.interactable = false;

        if (pacote.imagemPacote != null)
        {
            RectTransform imagemRT = CriarImagem("ImagemRealDoPacote", pacoteVisual, Color.white, tamanhoMaximoPacote, Vector2.zero);
            imagemPacote = imagemRT.GetComponent<Image>();
            imagemPacote.sprite = pacote.imagemPacote;
            imagemPacote.preserveAspect = true;
            imagemPacote.raycastTarget = false;
        }
        else
        {
            CriarPacoteFallbackGerado(pacote);
        }

        selo = CriarImagem("FaixaSelamento", pacoteVisual, pacote.corSecundariaPacote,
            new Vector2(tamanhoMaximoPacote.x * 1.04f, 62f), new Vector2(0f, -18f));
        selo.localScale = new Vector3(0f, 1f, 1f);
        selo.GetComponent<Image>().raycastTarget = false;

        textoSelo = CriarTexto("TextoSelado", selo, "SELADO", 23f, pacote.corTextoPacote,
            selo.sizeDelta, Vector2.zero, TextAlignmentOptions.Center);
        textoSelo.alpha = 0f;
    }

    private void CriarPacoteFallbackGerado(PacoteAdquirido pacote)
    {
        RectTransform basePacote = CriarImagem("PacoteGerado", pacoteVisual, pacote.corPrincipalPacote,
            new Vector2(330f, 500f), Vector2.zero);
        basePacote.GetComponent<Image>().raycastTarget = false;

        CriarImagem("FaixaTopo", basePacote, pacote.corSecundariaPacote,
            new Vector2(330f, 48f), new Vector2(0f, 205f)).GetComponent<Image>().raycastTarget = false;

        CriarImagem("FaixaBaixo", basePacote, pacote.corSecundariaPacote,
            new Vector2(330f, 48f), new Vector2(0f, -205f)).GetComponent<Image>().raycastTarget = false;

        CriarTexto("NomePacote", basePacote,
            string.IsNullOrWhiteSpace(pacote.nomePacote) ? "CARD PACK" : pacote.nomePacote.ToUpperInvariant(),
            25f, pacote.corTextoPacote, new Vector2(285f, 120f), new Vector2(0f, 35f), TextAlignmentOptions.Center);

        CriarTexto("Quantidade", basePacote, "10 CARTAS", 18f, pacote.corTextoPacote,
            new Vector2(260f, 45f), new Vector2(0f, -65f), TextAlignmentOptions.Center);
    }

    private IEnumerator AnimarEntradaPacote(PacoteAdquirido pacote)
    {
        if (pacoteVisual == null)
            yield break;

        pacoteVisual.localScale = Vector3.one * 0.14f;
        pacoteVisual.localEulerAngles = new Vector3(0f, 0f, -7f);
        grupoPacote.alpha = 0f;

        Color corBrilho = pacote.corSecundariaPacote;
        corBrilho.a = 0.12f;
        brilho.GetComponent<Image>().color = corBrilho;
        brilho.localScale = Vector3.one * 0.65f;

        float t = 0f;
        while (t < duracaoEntrada)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracaoEntrada);
            float suave = SuavizarSaida(p);

            grupoPacote.alpha = p;

            float escala;
            if (p < 0.78f)
                escala = Mathf.Lerp(0.14f, 1.08f, SuavizarSaida(p / 0.78f));
            else
                escala = Mathf.Lerp(1.08f, 1f, (p - 0.78f) / 0.22f);

            pacoteVisual.localScale = Vector3.one * escala;
            pacoteVisual.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-7f, 0f, suave));
            brilho.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.05f, suave);
            yield return null;
        }

        pacoteVisual.localScale = Vector3.one;
        pacoteVisual.localEulerAngles = Vector3.zero;
        grupoPacote.alpha = 1f;
    }

    private IEnumerator AnimarPagamento(int preco, int orbsAntes, int orbsDepois, Color corPacote)
    {
        if (textoOrbs == null || textoPreco == null)
            yield break;

        if (preco <= 0)
        {
            textoOrbs.text = $"{orbsDepois} ORBS";
            textoPreco.text = "GRÁTIS";
            yield return new WaitForSecondsRealtime(0.20f);
            yield break;
        }

        textoPreco.text = $"-{preco} ORBS";
        textoPreco.color = corPacote;
        textoPreco.alpha = 0f;
        textoPreco.rectTransform.localScale = Vector3.one * 0.72f;

        float t = 0f;
        while (t < duracaoContagemOrbs)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracaoContagemOrbs);
            int valor = Mathf.RoundToInt(Mathf.Lerp(orbsAntes, orbsDepois, SuavizarSaida(p)));
            textoOrbs.text = $"{valor} ORBS";

            float aparecimento = Mathf.Clamp01(p * 3f);
            textoPreco.alpha = aparecimento;
            textoPreco.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, SuavizarSaida(aparecimento));

            if (p > 0.72f)
                textoPreco.alpha = 1f - ((p - 0.72f) / 0.28f);

            yield return null;
        }

        textoOrbs.text = $"{orbsDepois} ORBS";
        textoPreco.alpha = 0f;

        CriarParticulasPagamento(corPacote, 12);
        yield return new WaitForSecondsRealtime(0.12f);
    }

    private IEnumerator AnimarSelamento(Color corSelo, Color corTexto)
    {
        if (selo == null || textoSelo == null)
            yield break;

        Image imgSelo = selo.GetComponent<Image>();
        imgSelo.color = corSelo;
        textoSelo.color = corTexto;

        float t = 0f;
        while (t < duracaoSelamento)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracaoSelamento);
            float suave = SuavizarSaida(p);
            selo.localScale = new Vector3(suave, 1f, 1f);
            textoSelo.alpha = Mathf.Clamp01((p - 0.40f) / 0.35f);
            yield return null;
        }

        selo.localScale = Vector3.one;
        textoSelo.alpha = 1f;

        // Pequeno impacto/bounce quando o pacote é lacrado.
        Vector3 escalaBase = pacoteVisual.localScale;
        t = 0f;
        const float impacto = 0.18f;
        while (t < impacto)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / impacto);
            float pulso = Mathf.Sin(p * Mathf.PI);
            pacoteVisual.localScale = escalaBase * (1f + pulso * 0.055f);
            brilho.localScale = Vector3.one * (1.05f + pulso * 0.14f);
            yield return null;
        }

        pacoteVisual.localScale = escalaBase;
    }

    private IEnumerator AnimarTextoRapido(TMP_Text texto, float duracao)
    {
        if (texto == null)
            yield break;

        texto.alpha = 0f;
        texto.rectTransform.localScale = Vector3.one * 0.82f;

        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            texto.alpha = p;
            texto.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, SuavizarSaida(p));
            yield return null;
        }

        texto.alpha = 1f;
        texto.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator AnimarVooParaInventario(Color corPacote)
    {
        if (pacoteVisual == null)
            yield break;

        Vector2 inicio = pacoteVisual.anchoredPosition;
        Vector2 destino = ObterDestinoLocalDoInventario();
        Vector3 escalaInicio = pacoteVisual.localScale;
        float rotacaoInicio = pacoteVisual.localEulerAngles.z;

        // Elementos textuais desaparecem antes do voo.
        if (textoAdquirido != null)
            textoAdquirido.alpha = 0f;
        if (textoOrbs != null)
            textoOrbs.alpha = 0f;
        if (textoPreco != null)
            textoPreco.alpha = 0f;

        float t = 0f;
        while (t < duracaoVooInventario)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracaoVooInventario);
            float suave = p * p * (3f - 2f * p);

            // Arco leve para não parecer apenas um teleport linear.
            Vector2 pos = Vector2.Lerp(inicio, destino, suave);
            pos.y += Mathf.Sin(p * Mathf.PI) * 80f;
            pacoteVisual.anchoredPosition = pos;

            float escala = Mathf.Lerp(1f, escalaFinalNoInventario, SuavizarEntrada(p));
            pacoteVisual.localScale = escalaInicio * escala;
            pacoteVisual.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(rotacaoInicio, 18f, p));

            if (brilho != null)
            {
                Image img = brilho.GetComponent<Image>();
                Color c = img.color;
                c.a = Mathf.Lerp(c.a, 0f, p);
                img.color = c;
            }

            yield return null;
        }

        pacoteVisual.anchoredPosition = destino;
        pacoteVisual.localScale = escalaInicio * escalaFinalNoInventario;

        CriarTextoMaisUmPacote(destino, corPacote);

        if (alvoInventario != null)
            yield return AnimarBounceNoInventario(alvoInventario);
        else
            yield return new WaitForSecondsRealtime(0.22f);
    }

    private IEnumerator AnimarBounceNoInventario(RectTransform alvo)
    {
        if (alvo == null)
            yield break;

        Vector3 original = escalaOriginalAlvoInventario;
        float t = 0f;
        const float duracao = 0.24f;

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float pulso = Mathf.Sin(p * Mathf.PI);
            alvo.localScale = original * (1f + pulso * 0.16f);
            yield return null;
        }

        if (alvo != null)
            alvo.localScale = original;
    }

    private void CriarParticulasPagamento(Color cor, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            RectTransform rt = CriarImagem("OrbParticula", raiz,
                Color.Lerp(cor, Color.white, Random.Range(0.15f, 0.65f)),
                new Vector2(Random.Range(7f, 14f), Random.Range(7f, 14f)),
                new Vector2(Random.Range(-85f, 85f), Random.Range(300f, 400f)));

            rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            rt.GetComponent<Image>().raycastTarget = false;
            StartCoroutine(AnimarParticulaAtePacote(rt));
        }
    }

    private IEnumerator AnimarParticulaAtePacote(RectTransform rt)
    {
        if (rt == null)
            yield break;

        Vector2 inicio = rt.anchoredPosition;
        Vector2 fim = new Vector2(Random.Range(-80f, 80f), Random.Range(-80f, 100f));
        float duracao = Random.Range(0.28f, 0.46f);
        float t = 0f;
        Image img = rt.GetComponent<Image>();

        while (t < duracao && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            rt.anchoredPosition = Vector2.Lerp(inicio, fim, SuavizarEntrada(p));
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, p);

            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - p;
                img.color = c;
            }

            yield return null;
        }

        if (rt != null)
            Destroy(rt.gameObject);
    }

    private void CriarTextoMaisUmPacote(Vector2 destino, Color cor)
    {
        TMP_Text texto = CriarTexto("MaisUmPacote", raiz, "+1 PACOTE", 20f,
            Color.Lerp(cor, Color.white, 0.35f), new Vector2(260f, 45f),
            destino + new Vector2(0f, 60f), TextAlignmentOptions.Center);

        texto.alpha = 1f;
        StartCoroutine(AnimarMaisUm(texto));
    }

    private IEnumerator AnimarMaisUm(TMP_Text texto)
    {
        if (texto == null)
            yield break;

        RectTransform rt = texto.rectTransform;
        Vector2 inicio = rt.anchoredPosition;
        float t = 0f;
        const float duracao = 0.42f;

        while (t < duracao && texto != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            rt.anchoredPosition = inicio + new Vector2(0f, 35f * p);
            texto.alpha = 1f - p;
            yield return null;
        }

        if (texto != null)
            Destroy(texto.gameObject);
    }

    private RectTransform ProcurarAlvoInventarioAutomaticamente()
    {
        RectTransform melhor = null;
        int melhorPontuacao = int.MinValue;

        RectTransform[] todos = FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            RectTransform rt = todos[i];
            if (rt == null || rt == raiz || !rt.gameObject.scene.IsValid())
                continue;

            string nome = rt.name.ToLowerInvariant();
            int pontos = 0;

            if (nome.Contains("inventario")) pontos += 10;
            if (nome.Contains("inventário")) pontos += 10;
            if (nome.Contains("inventory")) pontos += 10;
            if (nome.Contains("abrir")) pontos += 5;
            if (nome.Contains("botao") || nome.Contains("botão") || nome.Contains("button")) pontos += 3;
            if (rt.GetComponent<Button>() != null) pontos += 5;
            if (rt.GetComponent<Image>() != null) pontos += 1;
            if (!rt.gameObject.activeInHierarchy) pontos -= 4;

            // Evita escolher grandes painéis que apenas possuem "Inventario" no nome.
            if (rt.rect.width > 700f || rt.rect.height > 700f)
                pontos -= 6;

            if (pontos > melhorPontuacao && pontos >= 10)
            {
                melhorPontuacao = pontos;
                melhor = rt;
            }
        }

        return melhor;
    }

    private Vector2 ObterDestinoLocalDoInventario()
    {
        if (raiz == null)
            return new Vector2(-760f, -430f);

        if (alvoInventario == null)
            return new Vector2(-760f, -430f);

        Canvas canvasAlvo = alvoInventario.GetComponentInParent<Canvas>();
        Camera cameraAlvo = null;

        if (canvasAlvo != null && canvasAlvo.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraAlvo = canvasAlvo.worldCamera;

        Vector2 posicaoTela = RectTransformUtility.WorldToScreenPoint(cameraAlvo, alvoInventario.position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, posicaoTela, null, out Vector2 local))
            return local;

        return new Vector2(-760f, -430f);
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
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private float SuavizarSaida(float p)
    {
        p = Mathf.Clamp01(p);
        return 1f - Mathf.Pow(1f - p, 3f);
    }

    private float SuavizarEntrada(float p)
    {
        p = Mathf.Clamp01(p);
        return p * p * p;
    }

    private void LimparReferencias()
    {
        canvas = null;
        raiz = null;
        pacoteVisual = null;
        grupoPacote = null;
        brilho = null;
        imagemPacote = null;
        selo = null;
        textoSelo = null;
        textoAdquirido = null;
        textoOrbs = null;
        textoPreco = null;
        alvoInventario = null;
    }
}
