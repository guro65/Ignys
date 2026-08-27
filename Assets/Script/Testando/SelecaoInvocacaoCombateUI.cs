using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI criada automaticamente para habilidades de Invocação.
/// 1) Quando necessário, permite escolher qual carta será invocada.
/// 2) Depois cria pequenos botões sobre os slots livres do tabuleiro para escolher a posição.
/// Não precisa existir na Hierarchy.
/// </summary>
public class SelecaoInvocacaoCombateUI : MonoBehaviour
{
    private static SelecaoInvocacaoCombateUI instancia;

    private Canvas canvas;
    private RectTransform raiz;
    private bool aberto;
    private Action callbackCancelar;

    public static SelecaoInvocacaoCombateUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<SelecaoInvocacaoCombateUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("SelecaoInvocacaoCombateUI");
        instancia = obj.AddComponent<SelecaoInvocacaoCombateUI>();
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

    public void MostrarEscolhaCarta(List<Carta> cartas, Action<Carta> aoSelecionar, Action aoCancelar = null)
    {
        List<Carta> validas = new List<Carta>();
        if (cartas != null)
        {
            for (int i = 0; i < cartas.Count; i++)
            {
                if (cartas[i] != null)
                    validas.Add(cartas[i]);
            }
        }

        if (validas.Count == 0)
        {
            aoCancelar?.Invoke();
            return;
        }

        LimparCanvas();
        callbackCancelar = aoCancelar;
        aberto = true;
        CriarCanvasBase(true);

        RectTransform janela = CriarImagem("JanelaInvocacao", raiz,
            new Color(0.045f, 0.06f, 0.095f, 0.985f), new Vector2(1120f, 700f), Vector2.zero);

        CriarTexto("Titulo", janela, "ESCOLHA A CARTA PARA INVOCAR", 30f, Color.white,
            new Vector2(980f, 55f), new Vector2(0f, 300f), TextAlignmentOptions.Center);

        CriarTexto("Info", janela, "Depois você escolherá um slot livre do seu tabuleiro.", 18f,
            new Color(0.76f, 0.83f, 0.95f, 1f), new Vector2(900f, 38f), new Vector2(0f, 260f), TextAlignmentOptions.Center);

        GameObject gridObj = new GameObject("GridInvocacoes", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform gridRT = gridObj.GetComponent<RectTransform>();
        gridRT.SetParent(janela, false);
        gridRT.sizeDelta = new Vector2(1010f, 485f);
        gridRT.anchoredPosition = new Vector2(0f, -5f);

        GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
        int colunas = Mathf.Clamp(validas.Count, 1, 5);
        float largura = validas.Count > 5 ? 168f : 185f;
        float altura = validas.Count > 5 ? 220f : 285f;
        grid.cellSize = new Vector2(largura, altura);
        grid.spacing = new Vector2(18f, 18f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = colunas;

        for (int i = 0; i < validas.Count; i++)
        {
            Carta capturada = validas[i];
            CriarBotaoCarta(gridRT, capturada, largura, altura, () =>
            {
                Action<Carta> callback = aoSelecionar;
                LimparCanvas();
                aberto = false;
                callback?.Invoke(capturada);
            });
        }

        Button cancelar = CriarBotao("Cancelar", janela, "CANCELAR",
            new Vector2(230f, 52f), new Vector2(0f, -308f), new Color(0.18f, 0.21f, 0.3f, 1f));
        cancelar.onClick.AddListener(Cancelar);
    }

    public void MostrarEscolhaPosicao(List<Transform> slotsLivres, Camera cameraMundo, Carta cartaEscolhida,
        Action<Transform> aoSelecionar, Action aoCancelar = null)
    {
        List<Transform> validos = new List<Transform>();
        if (slotsLivres != null)
        {
            for (int i = 0; i < slotsLivres.Count; i++)
            {
                if (slotsLivres[i] != null)
                    validos.Add(slotsLivres[i]);
            }
        }

        if (validos.Count == 0 || cameraMundo == null)
        {
            aoCancelar?.Invoke();
            return;
        }

        LimparCanvas();
        callbackCancelar = aoCancelar;
        aberto = true;
        CriarCanvasBase(false);

        RectTransform faixa = CriarImagem("FaixaInstrucao", raiz,
            new Color(0.025f, 0.035f, 0.065f, 0.93f), new Vector2(560f, 72f), new Vector2(0f, 435f));

        string nomeCarta = cartaEscolhida != null && !string.IsNullOrWhiteSpace(cartaEscolhida.nome)
            ? cartaEscolhida.nome : "Carta";
        CriarTexto("Titulo", faixa, $"ESCOLHA O SLOT  •  {nomeCarta}", 21f, Color.white,
            new Vector2(530f, 56f), Vector2.zero, TextAlignmentOptions.Center);

        for (int i = 0; i < validos.Count; i++)
        {
            Transform slot = validos[i];
            Vector3 tela = cameraMundo.WorldToScreenPoint(slot.position);
            if (tela.z < 0f)
                continue;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, tela, null, out local);

            Vector2 tamanho = CalcularTamanhoSlotNaTela(slot, cameraMundo);
            tamanho.x = Mathf.Clamp(tamanho.x, 80f, 175f);
            tamanho.y = Mathf.Clamp(tamanho.y, 95f, 210f);

            Button botao = CriarBotaoSlot($"SlotInvocacao_{i + 1}", raiz, i + 1, tamanho, local);
            Transform capturado = slot;
            botao.onClick.AddListener(() =>
            {
                Action<Transform> callback = aoSelecionar;
                LimparCanvas();
                aberto = false;
                callback?.Invoke(capturado);
            });

            StartCoroutine(PulsarSlot(botao.GetComponent<Image>()));
        }

        Button cancelar = CriarBotao("Cancelar", raiz, "CANCELAR",
            new Vector2(190f, 46f), new Vector2(0f, -470f), new Color(0.12f, 0.15f, 0.23f, 0.94f));
        cancelar.onClick.AddListener(Cancelar);
    }

    public void Fechar()
    {
        LimparCanvas();
        aberto = false;
        callbackCancelar = null;
    }

    public bool EstaAberto()
    {
        return aberto;
    }

    private void Cancelar()
    {
        Action callback = callbackCancelar;
        LimparCanvas();
        aberto = false;
        callbackCancelar = null;
        callback?.Invoke();
    }

    private void CriarCanvasBase(bool bloquearVisualmente)
    {
        GameObject canvasObj = new GameObject("Canvas_SelecaoInvocacao", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4700;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz = canvasObj.GetComponent<RectTransform>();

        RectTransform bloqueador = CriarImagem("Bloqueador", raiz,
            bloquearVisualmente ? new Color(0.005f, 0.008f, 0.02f, 0.80f) : new Color(0f, 0f, 0f, 0.22f),
            Vector2.zero, Vector2.zero);
        Esticar(bloqueador);
        bloqueador.GetComponent<Image>().raycastTarget = true;
    }

    private void CriarBotaoCarta(RectTransform parent, Carta carta, float largura, float altura, Action callback)
    {
        GameObject obj = new GameObject($"Invocar_{carta.nome}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        Image fundo = obj.GetComponent<Image>();
        fundo.color = new Color(0.105f, 0.13f, 0.20f, 1f);

        Button botao = obj.GetComponent<Button>();
        botao.targetGraphic = fundo;
        botao.onClick.AddListener(() => callback?.Invoke());

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float imagemAltura = altura - 60f;
            RectTransform imgRT = CriarImagem("Imagem", rt, Color.white,
                new Vector2(largura - 20f, imagemAltura), new Vector2(0f, 20f));
            Image img = imgRT.GetComponent<Image>();
            img.sprite = sr.sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        TMP_Text nome = CriarTexto("Nome", rt, carta.nome, 16f, Color.white,
            new Vector2(largura - 12f, 34f), new Vector2(0f, -altura * 0.5f + 27f), TextAlignmentOptions.Center);
        nome.enableAutoSizing = true;
        nome.fontSizeMin = 9f;
        nome.fontSizeMax = 16f;

        CriarTexto("Raridade", rt, carta.raridade.ToString(), 12f, new Color(0.95f, 0.82f, 0.42f, 1f),
            new Vector2(largura - 12f, 24f), new Vector2(0f, -altura * 0.5f + 9f), TextAlignmentOptions.Center);
    }

    private Button CriarBotaoSlot(string nome, Transform parent, int numero, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.17f, 0.78f, 1f, 0.30f);

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cores = btn.colors;
        cores.highlightedColor = new Color(0.30f, 0.90f, 1f, 0.52f);
        cores.pressedColor = new Color(0.08f, 0.62f, 0.92f, 0.68f);
        btn.colors = cores;

        CriarTexto("Numero", rt, numero.ToString(), 18f, Color.white,
            new Vector2(48f, 38f), Vector2.zero, TextAlignmentOptions.Center);
        return btn;
    }

    private Vector2 CalcularTamanhoSlotNaTela(Transform slot, Camera cam)
    {
        Collider2D col = slot.GetComponent<Collider2D>();
        if (col == null)
            col = slot.GetComponentInChildren<Collider2D>();

        if (col == null)
            return new Vector2(110f, 150f);

        Bounds b = col.bounds;
        Vector3 minTela = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, slot.position.z));
        Vector3 maxTela = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, slot.position.z));

        Vector2 minLocal;
        Vector2 maxLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, minTela, null, out minLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, maxTela, null, out maxLocal);

        Vector2 tamanho = new Vector2(Mathf.Abs(maxLocal.x - minLocal.x), Mathf.Abs(maxLocal.y - minLocal.y));
        if (tamanho.x < 10f || tamanho.y < 10f)
            return new Vector2(110f, 150f);

        return tamanho * 1.08f;
    }

    private IEnumerator PulsarSlot(Image img)
    {
        if (img == null)
            yield break;

        Color baseCor = img.color;
        float tempo = UnityEngine.Random.Range(0f, 0.8f);
        while (img != null && aberto)
        {
            tempo += Time.unscaledDeltaTime * 2.5f;
            float onda = (Mathf.Sin(tempo) + 1f) * 0.5f;
            Color c = baseCor;
            c.a = Mathf.Lerp(0.22f, 0.48f, onda);
            img.color = c;
            yield return null;
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
        CriarTexto("Texto", rt, texto, 16f, Color.white, tamanho, Vector2.zero, TextAlignmentOptions.Center);
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

    private void LimparCanvas()
    {
        StopAllCoroutines();
        if (canvas != null)
            Destroy(canvas.gameObject);
        canvas = null;
        raiz = null;
    }
}
