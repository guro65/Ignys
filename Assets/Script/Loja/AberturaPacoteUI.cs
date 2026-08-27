using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AberturaPacoteUI : MonoBehaviour
{
    private static AberturaPacoteUI instancia;

    [Header("Velocidade da abertura")]
    [Min(0.05f)][SerializeField] private float duracaoEntradaPacote = 0.28f;
    [Min(0.05f)][SerializeField] private float duracaoRasgo = 0.24f;
    [Min(0.05f)][SerializeField] private float duracaoSepararMetades = 0.48f;
    [Min(0.05f)][SerializeField] private float duracaoEntradaCarta = 0.32f;
    [Min(0.01f)][SerializeField] private float duracaoSaidaCarta = 0.12f;

    [Header("Cores gerais")]
    [SerializeField] private Color corFundo = new Color32(9, 12, 20, 250);
    [SerializeField] private Color corFlash = Color.white;

    [Header("Cores das raridades")]
    [SerializeField] private Color corComum = new Color32(190, 195, 205, 255);
    [SerializeField] private Color corEpico = new Color32(155, 90, 220, 255);
    [SerializeField] private Color corMitico = new Color32(255, 145, 55, 255);
    [SerializeField] private Color corProdigio = new Color32(55, 220, 220, 255);
    [SerializeField] private Color corCeleste = new Color32(115, 190, 255, 255);
    [SerializeField] private Color corScarlet = new Color32(235, 35, 70, 255);
    [SerializeField] private Color corDeus = new Color32(255, 225, 95, 255);

    [Header("Quantidade máxima de efeitos")]
    [Min(0)][SerializeField] private int maximoParticulas = 40;
    [Min(0)][SerializeField] private int maximoRaios = 24;

    private Canvas canvasAbertura;
    private RectTransform raizCanvas;
    private Image fundoTela;
    private Image flashTela;
    private Button botaoCapturarClique;
    private TMP_Text textoInstrucao;

    private PacoteAdquirido pacoteAtual;
    private List<Carta> cartasAtuais = new List<Carta>();
    private Action aoFinalizar;

    private bool aberturaEmAndamento = false;
    private bool aceitandoClique = false;
    private bool cliqueRecebido = false;
    private RectTransform conteudoTemporarioAtual;

    public static AberturaPacoteUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<AberturaPacoteUI>();

        if (instancia != null)
            return instancia;

        GameObject objeto = new GameObject("AberturaPacoteUI");
        instancia = objeto.AddComponent<AberturaPacoteUI>();
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

    public void IniciarAbertura(PacoteAdquirido pacote, List<Carta> cartas, Action callbackFinal)
    {
        if (aberturaEmAndamento)
        {
            Debug.LogWarning("Já existe uma abertura de pacote em andamento.");
            return;
        }

        if (pacote == null || cartas == null || cartas.Count == 0)
        {
            Debug.LogWarning("Não foi possível iniciar a abertura visual: pacote ou cartas inválidos.");
            callbackFinal?.Invoke();
            return;
        }

        pacoteAtual = pacote;
        cartasAtuais = new List<Carta>();

        for (int i = 0; i < cartas.Count; i++)
        {
            if (cartas[i] != null)
                cartasAtuais.Add(cartas[i]);
        }

        if (cartasAtuais.Count == 0)
        {
            callbackFinal?.Invoke();
            return;
        }

        aoFinalizar = callbackFinal;
        aberturaEmAndamento = true;
        StartCoroutine(RotinaCompletaDeAbertura());
    }

    private IEnumerator RotinaCompletaDeAbertura()
    {
        CriarInterfaceBase();

        yield return AnimarPacoteRasgando();
        yield return new WaitForSecondsRealtime(0.12f);

        for (int i = 0; i < cartasAtuais.Count; i++)
        {
            Carta carta = cartasAtuais[i];
            yield return RevelarCarta(carta, i + 1, cartasAtuais.Count);

            textoInstrucao.text = i < cartasAtuais.Count - 1
                ? "CLIQUE PARA REVELAR A PRÓXIMA CARTA"
                : "CLIQUE PARA VER TODAS AS CARTAS";

            yield return EsperarClique();
            yield return EsconderCartaAtual();
        }

        yield return MostrarResumoFinal();

        textoInstrucao.text = "CLIQUE PARA CONTINUAR";
        yield return EsperarClique();

        FinalizarAbertura();
    }

    private void CriarInterfaceBase()
    {
        GameObject canvasObj = new GameObject(
            "Canvas_AberturaPacote",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasAbertura = canvasObj.GetComponent<Canvas>();
        canvasAbertura.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasAbertura.sortingOrder = 5000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raizCanvas = canvasObj.GetComponent<RectTransform>();

        RectTransform fundoRT = CriarImagemEsticada(
            "Fundo",
            raizCanvas,
            corFundo,
            Vector2.zero,
            Vector2.zero
        );
        fundoTela = fundoRT.GetComponent<Image>();

        RectTransform flashRT = CriarImagemEsticada(
            "Flash",
            raizCanvas,
            ComAlpha(corFlash, 0f),
            Vector2.zero,
            Vector2.zero
        );
        flashTela = flashRT.GetComponent<Image>();

        textoInstrucao = CriarTexto(
            "Instrucao",
            raizCanvas,
            "ABRINDO PACOTE...",
            28f,
            Color.white,
            new Vector2(1000f, 70f),
            new Vector2(0f, -465f),
            TextAlignmentOptions.Center
        );

        // Área transparente que captura o clique em qualquer ponto da tela.
        GameObject cliqueObj = new GameObject(
            "CapturadorDeClique",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        RectTransform cliqueRT = cliqueObj.GetComponent<RectTransform>();
        cliqueRT.SetParent(raizCanvas, false);
        Esticar(cliqueRT, Vector2.zero, Vector2.zero);

        Image cliqueImage = cliqueObj.GetComponent<Image>();
        cliqueImage.color = new Color(0f, 0f, 0f, 0f);
        cliqueImage.raycastTarget = true;

        botaoCapturarClique = cliqueObj.GetComponent<Button>();
        botaoCapturarClique.transition = Selectable.Transition.None;
        botaoCapturarClique.onClick.RemoveAllListeners();
        botaoCapturarClique.onClick.AddListener(RegistrarClique);
    }

    private IEnumerator AnimarPacoteRasgando()
    {
        Carta.Raridade melhorRaridade = ObterMelhorRaridade(cartasAtuais);
        int dramaticidade = CalcularDramaticidade(pacoteAtual, melhorRaridade);
        Color corPressagio = dramaticidade >= 3
            ? ObterCorRaridade(melhorRaridade)
            : pacoteAtual.corSecundariaPacote;

        Sprite spritePacoteReal = pacoteAtual != null ? pacoteAtual.imagemPacote : null;
        bool possuiImagemReal = spritePacoteReal != null;

        Vector2 tamanhoPacote = possuiImagemReal
            ? CalcularTamanhoPacoteReal(spritePacoteReal, new Vector2(470f, 680f))
            : new Vector2(470f, 680f);

        RectTransform pacoteRoot = CriarRetangulo(
            possuiImagemReal ? "PacoteImagemReal" : "PacoteVisualFallback",
            raizCanvas,
            tamanhoPacote,
            Vector2.zero
        );

        pacoteRoot.localScale = Vector3.one * 0.65f;

        RectTransform metadeSuperior;
        RectTransform metadeInferior;

        if (possuiImagemReal)
        {
            // A MESMA imagem real do pacote é duplicada e recortada em duas metades.
            // Assim o pacote pode "rasgar" sem precisar de sprites extras.
            metadeSuperior = CriarMetadePacoteReal(
                "MetadeSuperior_ImagemReal",
                pacoteRoot,
                spritePacoteReal,
                tamanhoPacote,
                true
            );

            metadeInferior = CriarMetadePacoteReal(
                "MetadeInferior_ImagemReal",
                pacoteRoot,
                spritePacoteReal,
                tamanhoPacote,
                false
            );
        }
        else
        {
            // Fallback para pacotes antigos/sem imagem configurada.
            // Se imagemPacote estiver preenchida no prefab, este trecho nunca aparece.
            Debug.LogWarning(
                $"O pacote {pacoteAtual?.nomePacote ?? "desconhecido"} não possui imagemPacote. " +
                "A abertura usará um fallback simples."
            );

            float alturaMetade = tamanhoPacote.y * 0.5f;

            metadeSuperior = CriarImagem(
                "MetadeSuperior_Fallback",
                pacoteRoot,
                pacoteAtual != null ? pacoteAtual.corPrincipalPacote : new Color32(50, 58, 82, 255),
                new Vector2(tamanhoPacote.x, alturaMetade),
                new Vector2(0f, alturaMetade * 0.5f)
            );

            metadeInferior = CriarImagem(
                "MetadeInferior_Fallback",
                pacoteRoot,
                pacoteAtual != null
                    ? Escurecer(pacoteAtual.corPrincipalPacote, 0.10f)
                    : new Color32(42, 48, 68, 255),
                new Vector2(tamanhoPacote.x, alturaMetade),
                new Vector2(0f, -alturaMetade * 0.5f)
            );

            TMP_Text nomeFallback = CriarTexto(
                "NomePacoteFallback",
                pacoteRoot,
                string.IsNullOrWhiteSpace(pacoteAtual?.nomePacote)
                    ? "PACOTE"
                    : pacoteAtual.nomePacote.ToUpperInvariant(),
                34f,
                pacoteAtual != null ? pacoteAtual.corTextoPacote : Color.white,
                new Vector2(tamanhoPacote.x * 0.82f, 150f),
                Vector2.zero,
                TextAlignmentOptions.Center
            );

            nomeFallback.enableAutoSizing = true;
            nomeFallback.fontSizeMin = 16f;
            nomeFallback.fontSizeMax = 34f;
        }

        CanvasGroup grupoSuperior = metadeSuperior.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup grupoInferior = metadeInferior.gameObject.AddComponent<CanvasGroup>();

        // A única sobreposição feita em cima da imagem real é a linha temporária do rasgo.
        // O desenho, nome, logos e identidade do pacote continuam exatamente os do sprite original.
        RectTransform linhaRasgo = CriarImagem(
            "LinhaDoRasgo",
            pacoteRoot,
            Color.white,
            new Vector2(0f, 8f),
            Vector2.zero
        );

        textoInstrucao.text = "ABRINDO PACOTE...";

        yield return AnimarEscala(
            pacoteRoot,
            Vector3.one * 0.65f,
            Vector3.one,
            duracaoEntradaPacote
        );

        float duracaoTremor = 0.36f + (dramaticidade * 0.14f);
        float intensidadeTremor = 7f + (dramaticidade * 5f);
        yield return Tremer(pacoteRoot, duracaoTremor, intensidadeTremor);

        if (dramaticidade >= 2)
        {
            yield return PulsoEscala(
                pacoteRoot,
                1f,
                1.055f + dramaticidade * 0.006f,
                0.18f
            );
        }

        // Para aberturas muito raras, o rasgo começa, para e depois explode de vez.
        if (dramaticidade >= 3)
        {
            float larguraPressagio = Mathf.Min(180f, tamanhoPacote.x * 0.42f);

            yield return AnimarLargura(linhaRasgo, 0f, larguraPressagio, 0.12f);
            yield return PiscarTela(corPressagio, 0.28f, 0.11f);
            yield return AnimarLargura(linhaRasgo, larguraPressagio, 0f, 0.08f);
            yield return new WaitForSecondsRealtime(0.16f);
            yield return Tremer(pacoteRoot, 0.36f, intensidadeTremor * 1.45f);
        }

        if (dramaticidade >= 4)
        {
            yield return PiscarTela(corPressagio, 0.36f, 0.09f);
            yield return new WaitForSecondsRealtime(0.12f);
        }

        yield return AnimarLargura(
            linhaRasgo,
            0f,
            tamanhoPacote.x + 40f,
            duracaoRasgo
        );

        yield return PiscarTela(
            dramaticidade >= 3 ? corPressagio : Color.white,
            0.72f,
            0.14f
        );

        Vector2 inicioSuperior = metadeSuperior.anchoredPosition;
        Vector2 inicioInferior = metadeInferior.anchoredPosition;

        float deslocamentoHorizontal = Mathf.Max(45f, tamanhoPacote.x * 0.14f);
        float deslocamentoVertical = Mathf.Max(390f, tamanhoPacote.y * 0.84f);

        float tempo = 0f;

        while (tempo < duracaoSepararMetades)
        {
            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracaoSepararMetades);
            float suave = 1f - Mathf.Pow(1f - t, 3f);

            metadeSuperior.anchoredPosition = Vector2.Lerp(
                inicioSuperior,
                inicioSuperior + new Vector2(-deslocamentoHorizontal, deslocamentoVertical),
                suave
            );

            metadeInferior.anchoredPosition = Vector2.Lerp(
                inicioInferior,
                inicioInferior + new Vector2(deslocamentoHorizontal, -deslocamentoVertical),
                suave
            );

            metadeSuperior.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(0f, -22f, suave)
            );

            metadeInferior.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(0f, 18f, suave)
            );

            grupoSuperior.alpha = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);
            grupoInferior.alpha = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);

            yield return null;
        }

        Destroy(pacoteRoot.gameObject);
    }

    private Vector2 CalcularTamanhoPacoteReal(Sprite sprite, Vector2 tamanhoMaximo)
    {
        if (sprite == null)
            return tamanhoMaximo;

        float larguraSprite = sprite.rect.width;
        float alturaSprite = sprite.rect.height;

        if (larguraSprite <= 0f || alturaSprite <= 0f)
            return tamanhoMaximo;

        float escala = Mathf.Min(
            tamanhoMaximo.x / larguraSprite,
            tamanhoMaximo.y / alturaSprite
        );

        return new Vector2(
            larguraSprite * escala,
            alturaSprite * escala
        );
    }

    private RectTransform CriarMetadePacoteReal(
        string nome,
        RectTransform parent,
        Sprite sprite,
        Vector2 tamanhoCompleto,
        bool superior
    )
    {
        float alturaMetade = tamanhoCompleto.y * 0.5f;
        float posicaoMetadeY = superior
            ? alturaMetade * 0.5f
            : -alturaMetade * 0.5f;

        GameObject mascaraObj = new GameObject(
            nome,
            typeof(RectTransform),
            typeof(RectMask2D)
        );

        RectTransform mascaraRT = mascaraObj.GetComponent<RectTransform>();
        mascaraRT.SetParent(parent, false);

        ConfigurarCentro(
            mascaraRT,
            new Vector2(tamanhoCompleto.x, alturaMetade),
            new Vector2(0f, posicaoMetadeY)
        );

        // A imagem inteira fica dentro da máscara, deslocada para manter
        // exatamente o mesmo alinhamento visual nas duas metades.
        RectTransform imagemRT = CriarImagem(
            "ImagemRealDoPacote",
            mascaraRT,
            Color.white,
            tamanhoCompleto,
            new Vector2(
                0f,
                superior ? -alturaMetade * 0.5f : alturaMetade * 0.5f
            )
        );

        Image imagem = imagemRT.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.color = Color.white;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;

        return mascaraRT;
    }

    private IEnumerator RevelarCarta(Carta carta, int indice, int total)
    {
        if (carta == null)
            yield break;

        Color corRaridade = ObterCorRaridade(carta.raridade);
        int nivel = Mathf.Clamp((int)carta.raridade, 0, 6);

        fundoTela.color = corFundo;

        RectTransform root = CriarRetangulo(
            "RevelacaoCarta",
            raizCanvas,
            new Vector2(1000f, 940f),
            new Vector2(0f, 5f)
        );
        conteudoTemporarioAtual = root;

        CanvasGroup grupo = root.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        CriarTexto(
            "Contador",
            root,
            $"{indice} / {total}",
            30f,
            Color.white,
            new Vector2(300f, 55f),
            new Vector2(0f, 425f),
            TextAlignmentOptions.Center
        );

        CriarTexto(
            "NomeCarta",
            root,
            string.IsNullOrWhiteSpace(carta.nome) ? "CARTA" : carta.nome,
            36f,
            Color.white,
            new Vector2(760f, 65f),
            new Vector2(0f, 360f),
            TextAlignmentOptions.Center
        );

        TMP_Text textoRaridade = CriarTexto(
            "Raridade",
            root,
            ObterNomeRaridade(carta.raridade),
            34f,
            corRaridade,
            new Vector2(600f, 60f),
            new Vector2(0f, -365f),
            TextAlignmentOptions.Center
        );
        textoRaridade.fontStyle = FontStyles.Bold;

        RectTransform brilho = CriarImagem(
            "BrilhoRaridade",
            root,
            ComAlpha(corRaridade, nivel == 0 ? 0.05f : 0.18f),
            new Vector2(590f, 760f),
            new Vector2(0f, -5f)
        );
        brilho.localRotation = Quaternion.Euler(0f, 0f, 45f);

        RectTransform efeitosRoot = CriarRetangulo(
            "Efeitos",
            root,
            new Vector2(900f, 820f),
            new Vector2(0f, -5f)
        );

        int quantidadeRaios = ObterQuantidadeRaios(nivel);
        int quantidadeParticulas = ObterQuantidadeParticulas(nivel);

        CriarRaios(efeitosRoot, corRaridade, quantidadeRaios);
        CriarParticulas(efeitosRoot, corRaridade, quantidadeParticulas);

        RectTransform cartaRT = CriarImagem(
            "ImagemCarta",
            root,
            Color.white,
            new Vector2(440f, 610f),
            new Vector2(0f, -10f)
        );

        Image imagemCarta = cartaRT.GetComponent<Image>();
        imagemCarta.preserveAspect = true;
        Sprite spriteCarta = ObterSpriteCarta(carta);

        if (spriteCarta != null)
        {
            imagemCarta.sprite = spriteCarta;
            imagemCarta.color = Color.white;
        }
        else
        {
            imagemCarta.sprite = null;
            imagemCarta.color = new Color32(35, 38, 48, 255);

            CriarTexto(
                "FallbackNome",
                cartaRT,
                carta.nome,
                34f,
                Color.white,
                new Vector2(390f, 180f),
                Vector2.zero,
                TextAlignmentOptions.Center
            );
        }

        cartaRT.localScale = Vector3.one * 0.24f;

        // Antecipação específica das raridades mais altas.
        if (carta.raridade == Carta.Raridade.Celeste)
        {
            yield return new WaitForSecondsRealtime(0.20f);
            yield return PiscarTela(corRaridade, 0.30f, 0.12f);
        }
        else if (carta.raridade == Carta.Raridade.Scarlet)
        {
            yield return PulsoFundo(corRaridade, 0.42f, 2);
            yield return new WaitForSecondsRealtime(0.18f);
            yield return PiscarTela(corRaridade, 0.62f, 0.12f);
        }
        else if (carta.raridade == Carta.Raridade.Deus)
        {
            Color fundoOriginal = fundoTela.color;
            fundoTela.color = new Color(0.01f, 0.01f, 0.015f, 0.99f);
            yield return new WaitForSecondsRealtime(0.36f);
            yield return PiscarTela(Color.white, 0.78f, 0.10f);
            yield return new WaitForSecondsRealtime(0.08f);
            yield return PiscarTela(corRaridade, 0.82f, 0.14f);
            fundoTela.color = fundoOriginal;
        }
        else if (nivel >= 1)
        {
            yield return PiscarTela(corRaridade, 0.12f + nivel * 0.055f, 0.08f);
        }

        yield return AnimarAlpha(grupo, 0f, 1f, 0.12f);

        float overshoot = 1.02f + nivel * 0.012f;
        yield return AnimarEscala(cartaRT, Vector3.one * 0.24f, Vector3.one * overshoot, duracaoEntradaCarta);
        yield return AnimarEscala(cartaRT, Vector3.one * overshoot, Vector3.one, 0.10f);

        if (nivel >= 2)
            yield return Tremer(cartaRT, 0.12f + nivel * 0.018f, 3f + nivel * 1.3f);

        StartCoroutine(PulsarBrilho(brilho, nivel));
    }

    private IEnumerator EsconderCartaAtual()
    {
        if (conteudoTemporarioAtual == null)
            yield break;

        CanvasGroup grupo = conteudoTemporarioAtual.GetComponent<CanvasGroup>();

        if (grupo != null)
            yield return AnimarAlpha(grupo, grupo.alpha, 0f, duracaoSaidaCarta);

        if (conteudoTemporarioAtual != null)
            Destroy(conteudoTemporarioAtual.gameObject);

        conteudoTemporarioAtual = null;
        yield return new WaitForSecondsRealtime(0.04f);
    }

    private IEnumerator MostrarResumoFinal()
    {
        RectTransform resumoRoot = CriarRetangulo(
            "ResumoFinal",
            raizCanvas,
            new Vector2(1700f, 930f),
            new Vector2(0f, 10f)
        );
        conteudoTemporarioAtual = resumoRoot;

        CanvasGroup grupo = resumoRoot.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        CriarTexto(
            "TituloResumo",
            resumoRoot,
            "CARTAS OBTIDAS",
            44f,
            Color.white,
            new Vector2(900f, 70f),
            new Vector2(0f, 410f),
            TextAlignmentOptions.Center
        );

        GameObject gridObj = new GameObject(
            "GridCartas",
            typeof(RectTransform),
            typeof(GridLayoutGroup)
        );
        RectTransform gridRT = gridObj.GetComponent<RectTransform>();
        gridRT.SetParent(resumoRoot, false);
        ConfigurarCentro(gridRT, new Vector2(1475f, 705f), new Vector2(0f, 5f));

        GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(270f, 325f);
        grid.spacing = new Vector2(25f, 25f);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        for (int i = 0; i < cartasAtuais.Count; i++)
            CriarCartaResumo(gridRT, cartasAtuais[i]);

        RectTransform botaoVisual = CriarImagem(
            "BotaoContinuarVisual",
            resumoRoot,
            pacoteAtual != null ? pacoteAtual.corSecundariaPacote : new Color32(80, 105, 180, 255),
            new Vector2(310f, 72f),
            new Vector2(0f, -420f)
        );

        CriarTexto(
            "TextoContinuar",
            botaoVisual,
            "CONTINUAR",
            29f,
            Color.white,
            new Vector2(290f, 60f),
            Vector2.zero,
            TextAlignmentOptions.Center
        );

        yield return AnimarAlpha(grupo, 0f, 1f, 0.20f);
    }

    private void CriarCartaResumo(RectTransform parent, Carta carta)
    {
        if (carta == null)
            return;

        GameObject cellObj = new GameObject(
            $"Resumo_{carta.nome}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform cellRT = cellObj.GetComponent<RectTransform>();
        cellRT.SetParent(parent, false);

        Image fundoCell = cellObj.GetComponent<Image>();
        Color cor = ObterCorRaridade(carta.raridade);
        fundoCell.color = ComAlpha(cor, 0.22f);
        fundoCell.raycastTarget = false;

        RectTransform imagemRT = CriarImagem(
            "Carta",
            cellRT,
            Color.white,
            new Vector2(235f, 250f),
            new Vector2(0f, 25f)
        );

        Image img = imagemRT.GetComponent<Image>();
        img.preserveAspect = true;
        Sprite sprite = ObterSpriteCarta(carta);

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            img.sprite = null;
            img.color = new Color32(35, 38, 48, 255);
        }

        TMP_Text nome = CriarTexto(
            "Nome",
            cellRT,
            carta.nome,
            20f,
            Color.white,
            new Vector2(250f, 42f),
            new Vector2(0f, -132f),
            TextAlignmentOptions.Center
        );
        nome.enableAutoSizing = true;
        nome.fontSizeMin = 12f;
        nome.fontSizeMax = 20f;
    }

    private IEnumerator EsperarClique()
    {
        cliqueRecebido = false;
        aceitandoClique = true;

        while (!cliqueRecebido)
            yield return null;

        aceitandoClique = false;
        cliqueRecebido = false;
    }

    private void RegistrarClique()
    {
        if (!aceitandoClique)
            return;

        cliqueRecebido = true;
        aceitandoClique = false;
    }

    private void FinalizarAbertura()
    {
        aceitandoClique = false;
        cliqueRecebido = false;
        aberturaEmAndamento = false;

        if (canvasAbertura != null)
            Destroy(canvasAbertura.gameObject);

        canvasAbertura = null;
        raizCanvas = null;
        fundoTela = null;
        flashTela = null;
        botaoCapturarClique = null;
        textoInstrucao = null;
        conteudoTemporarioAtual = null;

        pacoteAtual = null;
        cartasAtuais.Clear();

        Action callback = aoFinalizar;
        aoFinalizar = null;
        callback?.Invoke();
    }

    private Carta.Raridade ObterMelhorRaridade(List<Carta> cartas)
    {
        Carta.Raridade melhor = Carta.Raridade.Comum;

        if (cartas == null)
            return melhor;

        for (int i = 0; i < cartas.Count; i++)
        {
            if (cartas[i] != null && (int)cartas[i].raridade > (int)melhor)
                melhor = cartas[i].raridade;
        }

        return melhor;
    }

    private int CalcularDramaticidade(PacoteAdquirido pacote, Carta.Raridade melhorRaridade)
    {
        int nivelPeso = 0;

        if (pacote != null)
        {
            if (pacote.peso == PesoPacote.Mediano)
                nivelPeso = 1;
            else if (pacote.peso == PesoPacote.Pesado)
                nivelPeso = 2;
        }

        int nivelRaridade = 0;

        switch (melhorRaridade)
        {
            case Carta.Raridade.Mitico:
                nivelRaridade = 1;
                break;
            case Carta.Raridade.Prodigio:
                nivelRaridade = 2;
                break;
            case Carta.Raridade.Celeste:
                nivelRaridade = 2;
                break;
            case Carta.Raridade.Scarlet:
                nivelRaridade = 3;
                break;
            case Carta.Raridade.Deus:
                nivelRaridade = 4;
                break;
        }

        return Mathf.Clamp(Mathf.Max(nivelPeso, nivelRaridade), 0, 4);
    }

    private int ObterQuantidadeParticulas(int nivel)
    {
        int[] valores = { 0, 6, 12, 17, 23, 31, 40 };
        return Mathf.Min(maximoParticulas, valores[Mathf.Clamp(nivel, 0, valores.Length - 1)]);
    }

    private int ObterQuantidadeRaios(int nivel)
    {
        int[] valores = { 0, 0, 6, 8, 12, 17, 24 };
        return Mathf.Min(maximoRaios, valores[Mathf.Clamp(nivel, 0, valores.Length - 1)]);
    }

    private void CriarParticulas(RectTransform parent, Color cor, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            float tamanho = UnityEngine.Random.Range(7f, 20f);
            RectTransform particula = CriarImagem(
                $"Particula_{i}",
                parent,
                ComAlpha(cor, UnityEngine.Random.Range(0.55f, 1f)),
                new Vector2(tamanho, tamanho),
                UnityEngine.Random.insideUnitCircle * 65f
            );

            Vector2 direcao = UnityEngine.Random.insideUnitCircle.normalized;
            if (direcao.sqrMagnitude < 0.01f)
                direcao = Vector2.up;

            Vector2 destino = direcao * UnityEngine.Random.Range(260f, 500f);
            float duracao = UnityEngine.Random.Range(0.48f, 0.95f);
            StartCoroutine(AnimarParticula(particula, destino, duracao));
        }
    }

    private void CriarRaios(RectTransform parent, Color cor, int quantidade)
    {
        if (quantidade <= 0)
            return;

        for (int i = 0; i < quantidade; i++)
        {
            float angulo = (360f / quantidade) * i + UnityEngine.Random.Range(-6f, 6f);
            float comprimento = UnityEngine.Random.Range(180f, 330f);
            float espessura = UnityEngine.Random.Range(4f, 10f);

            RectTransform raio = CriarImagem(
                $"Raio_{i}",
                parent,
                ComAlpha(cor, UnityEngine.Random.Range(0.18f, 0.48f)),
                new Vector2(comprimento, espessura),
                Vector2.zero
            );

            raio.pivot = new Vector2(0f, 0.5f);
            raio.localRotation = Quaternion.Euler(0f, 0f, angulo);
            raio.localScale = new Vector3(0.08f, 1f, 1f);

            StartCoroutine(AnimarRaio(raio, UnityEngine.Random.Range(0.35f, 0.70f)));
        }
    }

    private IEnumerator AnimarParticula(RectTransform particula, Vector2 deslocamento, float duracao)
    {
        if (particula == null)
            yield break;

        Image imagem = particula.GetComponent<Image>();
        Vector2 inicio = particula.anchoredPosition;
        Vector2 fim = inicio + deslocamento;
        Color corInicial = imagem != null ? imagem.color : Color.white;
        float rotacaoInicial = UnityEngine.Random.Range(0f, 360f);
        float rotacaoFinal = rotacaoInicial + UnityEngine.Random.Range(-220f, 220f);
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (particula == null || imagem == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            float suave = 1f - Mathf.Pow(1f - t, 2f);

            particula.anchoredPosition = Vector2.Lerp(inicio, fim, suave);
            particula.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(rotacaoInicial, rotacaoFinal, t));
            imagem.color = ComAlpha(corInicial, Mathf.Lerp(corInicial.a, 0f, t));

            yield return null;
        }

        if (particula != null)
            Destroy(particula.gameObject);
    }

    private IEnumerator AnimarRaio(RectTransform raio, float duracao)
    {
        if (raio == null)
            yield break;

        Image imagem = raio.GetComponent<Image>();
        Color corInicial = imagem != null ? imagem.color : Color.white;
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (raio == null || imagem == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            float curva = Mathf.Sin(t * Mathf.PI);

            raio.localScale = new Vector3(Mathf.Lerp(0.08f, 1f, curva), 1f, 1f);
            imagem.color = ComAlpha(corInicial, corInicial.a * curva);

            yield return null;
        }

        if (raio != null)
            Destroy(raio.gameObject);
    }

    private IEnumerator PulsarBrilho(RectTransform brilho, int nivel)
    {
        if (brilho == null)
            yield break;

        int pulsos = Mathf.Clamp(1 + nivel / 2, 1, 4);
        Vector3 escalaBase = Vector3.one;
        Vector3 escalaGrande = Vector3.one * (1.06f + nivel * 0.018f);

        for (int i = 0; i < pulsos; i++)
        {
            if (brilho == null)
                yield break;

            yield return AnimarEscala(brilho, escalaBase, escalaGrande, 0.18f);
            yield return AnimarEscala(brilho, escalaGrande, escalaBase, 0.18f);
        }
    }

    private IEnumerator PulsoFundo(Color cor, float intensidade, int pulsos)
    {
        if (fundoTela == null)
            yield break;

        Color original = corFundo;
        Color alvo = Color.Lerp(corFundo, cor, Mathf.Clamp01(intensidade));
        alvo.a = corFundo.a;

        for (int i = 0; i < pulsos; i++)
        {
            yield return AnimarCorImagem(fundoTela, original, alvo, 0.12f);
            yield return AnimarCorImagem(fundoTela, alvo, original, 0.16f);
        }

        fundoTela.color = corFundo;
    }

    private IEnumerator PiscarTela(Color cor, float alphaMaximo, float duracao)
    {
        if (flashTela == null)
            yield break;

        Color inicio = ComAlpha(cor, 0f);
        Color pico = ComAlpha(cor, Mathf.Clamp01(alphaMaximo));

        flashTela.color = inicio;
        yield return AnimarCorImagem(flashTela, inicio, pico, duracao * 0.42f);
        yield return AnimarCorImagem(flashTela, pico, inicio, duracao * 0.58f);
        flashTela.color = inicio;
    }

    private IEnumerator Tremer(RectTransform alvo, float duracao, float intensidade)
    {
        if (alvo == null)
            yield break;

        Vector2 posicaoOriginal = alvo.anchoredPosition;
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (alvo == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float restante = 1f - Mathf.Clamp01(tempo / duracao);
            Vector2 deslocamento = UnityEngine.Random.insideUnitCircle * intensidade * (0.45f + restante * 0.55f);
            alvo.anchoredPosition = posicaoOriginal + deslocamento;
            yield return null;
        }

        if (alvo != null)
            alvo.anchoredPosition = posicaoOriginal;
    }

    private IEnumerator PulsoEscala(RectTransform alvo, float escalaInicial, float escalaPico, float duracao)
    {
        yield return AnimarEscala(alvo, Vector3.one * escalaInicial, Vector3.one * escalaPico, duracao * 0.5f);
        yield return AnimarEscala(alvo, Vector3.one * escalaPico, Vector3.one * escalaInicial, duracao * 0.5f);
    }

    private IEnumerator AnimarEscala(RectTransform alvo, Vector3 inicio, Vector3 fim, float duracao)
    {
        if (alvo == null)
            yield break;

        if (duracao <= 0f)
        {
            alvo.localScale = fim;
            yield break;
        }

        float tempo = 0f;

        while (tempo < duracao)
        {
            if (alvo == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            float suave = t * t * (3f - 2f * t);
            alvo.localScale = Vector3.LerpUnclamped(inicio, fim, suave);
            yield return null;
        }

        if (alvo != null)
            alvo.localScale = fim;
    }

    private IEnumerator AnimarAlpha(CanvasGroup grupo, float inicio, float fim, float duracao)
    {
        if (grupo == null)
            yield break;

        if (duracao <= 0f)
        {
            grupo.alpha = fim;
            yield break;
        }

        float tempo = 0f;

        while (tempo < duracao)
        {
            if (grupo == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            grupo.alpha = Mathf.Lerp(inicio, fim, t);
            yield return null;
        }

        if (grupo != null)
            grupo.alpha = fim;
    }

    private IEnumerator AnimarCorImagem(Image imagem, Color inicio, Color fim, float duracao)
    {
        if (imagem == null)
            yield break;

        if (duracao <= 0f)
        {
            imagem.color = fim;
            yield break;
        }

        float tempo = 0f;

        while (tempo < duracao)
        {
            if (imagem == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            imagem.color = Color.Lerp(inicio, fim, t);
            yield return null;
        }

        if (imagem != null)
            imagem.color = fim;
    }

    private IEnumerator AnimarLargura(RectTransform alvo, float inicio, float fim, float duracao)
    {
        if (alvo == null)
            yield break;

        Vector2 tamanho = alvo.sizeDelta;
        float tempo = 0f;

        if (duracao <= 0f)
        {
            tamanho.x = fim;
            alvo.sizeDelta = tamanho;
            yield break;
        }

        while (tempo < duracao)
        {
            if (alvo == null)
                yield break;

            tempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            tamanho.x = Mathf.Lerp(inicio, fim, t);
            alvo.sizeDelta = tamanho;
            yield return null;
        }

        if (alvo != null)
        {
            tamanho.x = fim;
            alvo.sizeDelta = tamanho;
        }
    }

    private Sprite ObterSpriteCarta(Carta carta)
    {
        if (carta == null)
            return null;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = carta.GetComponentInChildren<SpriteRenderer>();

        return sr != null ? sr.sprite : null;
    }

    private Color ObterCorRaridade(Carta.Raridade raridade)
    {
        switch (raridade)
        {
            case Carta.Raridade.Epico:
                return corEpico;
            case Carta.Raridade.Mitico:
                return corMitico;
            case Carta.Raridade.Prodigio:
                return corProdigio;
            case Carta.Raridade.Celeste:
                return corCeleste;
            case Carta.Raridade.Scarlet:
                return corScarlet;
            case Carta.Raridade.Deus:
                return corDeus;
            default:
                return corComum;
        }
    }

    private string ObterNomeRaridade(Carta.Raridade raridade)
    {
        switch (raridade)
        {
            case Carta.Raridade.Comum:
                return "COMUM";
            case Carta.Raridade.Epico:
                return "ÉPICO";
            case Carta.Raridade.Mitico:
                return "MÍTICO";
            case Carta.Raridade.Prodigio:
                return "PRODÍGIO";
            case Carta.Raridade.Celeste:
                return "CELESTE";
            case Carta.Raridade.Scarlet:
                return "SCARLET";
            case Carta.Raridade.Deus:
                return "DEUS";
            default:
                return raridade.ToString().ToUpperInvariant();
        }
    }

    private RectTransform CriarRetangulo(string nome, Transform parent, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        ConfigurarCentro(rt, tamanho, posicao);
        return rt;
    }

    private RectTransform CriarImagem(string nome, Transform parent, Color cor, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(
            nome,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        ConfigurarCentro(rt, tamanho, posicao);

        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = null;
        imagem.color = cor;
        imagem.raycastTarget = false;

        return rt;
    }

    private RectTransform CriarImagemEsticada(string nome, Transform parent, Color cor, Vector2 margemMin, Vector2 margemMax)
    {
        GameObject obj = new GameObject(
            nome,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Esticar(rt, margemMin, margemMax);

        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = null;
        imagem.color = cor;
        imagem.raycastTarget = false;

        return rt;
    }

    private TMP_Text CriarTexto(
        string nome,
        Transform parent,
        string conteudo,
        float tamanhoFonte,
        Color cor,
        Vector2 tamanho,
        Vector2 posicao,
        TextAlignmentOptions alinhamento
    )
    {
        GameObject obj = new GameObject(
            nome,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        ConfigurarCentro(rt, tamanho, posicao);

        TextMeshProUGUI texto = obj.GetComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.enableWordWrapping = true;
        texto.raycastTarget = false;

        return texto;
    }

    private void CriarBordaPacote(RectTransform parent, Color cor)
    {
        float largura = parent.sizeDelta.x;
        float altura = parent.sizeDelta.y;
        float espessura = 7f;

        CriarImagem("BordaTopo", parent, cor, new Vector2(largura, espessura), new Vector2(0f, altura * 0.5f - espessura * 0.5f));
        CriarImagem("BordaBaixo", parent, cor, new Vector2(largura, espessura), new Vector2(0f, -altura * 0.5f + espessura * 0.5f));
        CriarImagem("BordaEsquerda", parent, cor, new Vector2(espessura, altura), new Vector2(-largura * 0.5f + espessura * 0.5f, 0f));
        CriarImagem("BordaDireita", parent, cor, new Vector2(espessura, altura), new Vector2(largura * 0.5f - espessura * 0.5f, 0f));
    }

    private void ConfigurarCentro(RectTransform rt, Vector2 tamanho, Vector2 posicao)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private void Esticar(RectTransform rt, Vector2 margemMin, Vector2 margemMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = margemMin;
        rt.offsetMax = -margemMax;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private Color ComAlpha(Color cor, float alpha)
    {
        cor.a = Mathf.Clamp01(alpha);
        return cor;
    }

    private Color Escurecer(Color cor, float quantidade)
    {
        float fator = 1f - Mathf.Clamp01(quantidade);
        return new Color(cor.r * fator, cor.g * fator, cor.b * fator, cor.a);
    }
}
