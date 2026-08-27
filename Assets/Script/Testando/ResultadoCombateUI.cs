using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultadoCombateUI : MonoBehaviour
{
    private static ResultadoCombateUI instancia;
    private Canvas canvas;
    private bool aberto;

    [Header("Cores")]
    [SerializeField] private Color corFundo = new Color(0.015f, 0.02f, 0.035f, 0.96f);
    [SerializeField] private Color corVitoria = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color corDerrota = new Color(1f, 0.25f, 0.32f, 1f);

    public static ResultadoCombateUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<ResultadoCombateUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("ResultadoCombateUI");
        instancia = obj.AddComponent<ResultadoCombateUI>();
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

    public void MostrarVitoria(string nomeInimigo, RecompensaCombateRecebida recompensa, Action aoContinuar)
    {
        if (aberto)
            return;

        aberto = true;
        RectTransform raiz = CriarCanvasBase();
        RectTransform janela = CriarImagem("JanelaResultado", raiz, new Color(0.055f, 0.065f, 0.1f, 1f), new Vector2(850f, 680f), Vector2.zero);

        TMP_Text titulo = CriarTexto("Titulo", janela, "VITÓRIA!", 76f, corVitoria,
            new Vector2(760f, 100f), new Vector2(0f, 235f), TextAlignmentOptions.Center);
        CriarTexto("Subtitulo", janela, string.IsNullOrEmpty(nomeInimigo) ? "Oponente derrotado" : $"{nomeInimigo} foi derrotado",
            27f, Color.white, new Vector2(760f, 55f), new Vector2(0f, 165f), TextAlignmentOptions.Center);

        string textoRecompensa = MontarTextoRecompensas(recompensa);
        CriarTexto("Recompensas", janela, textoRecompensa, 25f, new Color(0.9f, 0.93f, 1f, 1f),
            new Vector2(710f, 330f), new Vector2(0f, 0f), TextAlignmentOptions.Center);

        Button continuar = CriarBotao("Continuar", janela, "CONTINUAR", new Vector2(340f, 74f), new Vector2(0f, -245f), corVitoria);
        continuar.onClick.AddListener(() =>
        {
            aoContinuar?.Invoke();
        });

        StartCoroutine(AnimarEntrada(janela));
        CriarConfetes(raiz, corVitoria, 34);
    }

    public void MostrarDerrota(string nomeInimigo, Action aoTentarNovamente, Action aoSair)
    {
        if (aberto)
            return;

        aberto = true;
        RectTransform raiz = CriarCanvasBase();
        RectTransform janela = CriarImagem("JanelaResultado", raiz, new Color(0.055f, 0.055f, 0.075f, 1f), new Vector2(850f, 650f), Vector2.zero);

        CriarTexto("Titulo", janela, "DERROTA", 76f, corDerrota,
            new Vector2(760f, 100f), new Vector2(0f, 220f), TextAlignmentOptions.Center);
        CriarTexto("Subtitulo", janela,
            string.IsNullOrEmpty(nomeInimigo) ? "Você não conseguiu vencer o duelo." : $"Você foi derrotado por {nomeInimigo}.",
            27f, Color.white, new Vector2(720f, 80f), new Vector2(0f, 120f), TextAlignmentOptions.Center);

        CriarTexto("SemRecompensa", janela, "Nenhuma recompensa de vitória foi recebida.", 24f,
            new Color(0.72f, 0.76f, 0.86f, 1f), new Vector2(700f, 70f), new Vector2(0f, 25f), TextAlignmentOptions.Center);

        Button tentar = CriarBotao("TentarNovamente", janela, "TENTAR NOVAMENTE", new Vector2(360f, 74f), new Vector2(0f, -105f), new Color(0.24f, 0.55f, 0.95f, 1f));
        tentar.onClick.AddListener(() => aoTentarNovamente?.Invoke());

        Button sair = CriarBotao("Sair", janela, "SAIR", new Vector2(300f, 68f), new Vector2(0f, -205f), new Color(0.28f, 0.3f, 0.38f, 1f));
        sair.onClick.AddListener(() => aoSair?.Invoke());

        StartCoroutine(AnimarEntrada(janela));
    }

    private string MontarTextoRecompensas(RecompensaCombateRecebida recompensa)
    {
        if (recompensa == null || !recompensa.PossuiAlgumaRecompensa())
            return "<b>RECOMPENSAS</b>\n\nNenhuma recompensa configurada.";

        string texto = "<b>RECOMPENSAS</b>\n\n";

        if (recompensa.recompensasEntregues != null && recompensa.recompensasEntregues.Count > 0)
        {
            for (int i = 0; i < recompensa.recompensasEntregues.Count; i++)
            {
                RecompensaEntregueCombate item = recompensa.recompensasEntregues[i];
                if (item == null || item.quantidade <= 0)
                    continue;

                if (item.tipo == TipoRecompensaInimigo.Orbs)
                {
                    texto += $"+ {item.quantidade} ORBS\n";
                }
                else if (item.tipo == TipoRecompensaInimigo.Carta)
                {
                    string raridade = item.carta != null ? $" ({item.carta.raridade})" : "";
                    texto += item.quantidade > 1
                        ? $"CARTA: {item.nome}{raridade}  x{item.quantidade}\n"
                        : $"CARTA: {item.nome}{raridade}\n";
                }
                else if (item.tipo == TipoRecompensaInimigo.Pacote)
                {
                    texto += item.quantidade > 1
                        ? $"PACOTE: {item.nome}  x{item.quantidade}\n"
                        : $"PACOTE: {item.nome}\n";
                }
            }

            return texto;
        }

        // Compatibilidade caso algum outro sistema ainda preencha somente os campos antigos.
        if (recompensa.orbsRecebidos > 0)
            texto += $"+ {recompensa.orbsRecebidos} ORBS\n";

        if (recompensa.pacoteRecebido != null)
            texto += $"PACOTE: {recompensa.pacoteRecebido.nomePacote}\n";

        if (recompensa.cartaRecebida != null)
            texto += $"CARTA: {recompensa.cartaRecebida.nome} ({recompensa.cartaRecebida.raridade})\n";

        return texto;
    }

    private RectTransform CriarCanvasBase()
    {
        GameObject canvasObj = new GameObject("Canvas_ResultadoCombate", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform raiz = canvasObj.GetComponent<RectTransform>();
        RectTransform fundo = CriarImagem("Fundo", raiz, corFundo, Vector2.zero, Vector2.zero);
        Esticar(fundo);
        fundo.GetComponent<Image>().raycastTarget = true;
        return raiz;
    }

    private IEnumerator AnimarEntrada(RectTransform janela)
    {
        if (janela == null)
            yield break;

        janela.localScale = Vector3.one * 0.72f;
        float t = 0f;
        const float duracao = 0.28f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float suavizado = 1f - Mathf.Pow(1f - p, 3f);
            janela.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, suavizado);
            yield return null;
        }
        janela.localScale = Vector3.one;
    }

    private void CriarConfetes(RectTransform parent, Color cor, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            RectTransform rt = CriarImagem("Confete", parent,
                Color.Lerp(cor, Color.white, UnityEngine.Random.Range(0f, 0.6f)),
                new Vector2(UnityEngine.Random.Range(8f, 20f), UnityEngine.Random.Range(8f, 24f)),
                new Vector2(UnityEngine.Random.Range(-850f, 850f), UnityEngine.Random.Range(-470f, 470f)));
            rt.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            rt.GetComponent<Image>().raycastTarget = false;
        }
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

        CriarTexto("Texto", rt, texto, 24f, Color.white, tamanho, Vector2.zero, TextAlignmentOptions.Center);
        return btn;
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
        tmp.enableWordWrapping = true;
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
}
