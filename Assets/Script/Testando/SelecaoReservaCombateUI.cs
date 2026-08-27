using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelecaoReservaCombateUI : MonoBehaviour
{
    private static SelecaoReservaCombateUI instancia;

    private Canvas canvas;
    private RectTransform raiz;
    private Action<Carta> callbackSelecao;
    private Action callbackCancelar;
    private bool aberto;

    public static SelecaoReservaCombateUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<SelecaoReservaCombateUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("SelecaoReservaCombateUI");
        instancia = obj.AddComponent<SelecaoReservaCombateUI>();
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

    public void Mostrar(List<Carta> cartas, Action<Carta> aoSelecionar, Action aoCancelar = null)
    {
        if (aberto)
            return;

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

        callbackSelecao = aoSelecionar;
        callbackCancelar = aoCancelar;
        aberto = true;
        CriarInterface(validas);
    }

    public void FecharSemSelecionar()
    {
        if (!aberto)
            return;

        Action cancelar = callbackCancelar;
        FecharInterno();
        cancelar?.Invoke();
    }

    private void CriarInterface(List<Carta> cartas)
    {
        GameObject canvasObj = new GameObject("Canvas_SelecaoReserva", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz = canvasObj.GetComponent<RectTransform>();

        RectTransform fundo = CriarImagem("FundoBloqueador", raiz, new Color(0.01f, 0.015f, 0.03f, 0.88f), Vector2.zero, Vector2.zero);
        Esticar(fundo);
        fundo.GetComponent<Image>().raycastTarget = true;

        RectTransform janela = CriarImagem("JanelaReserva", raiz, new Color(0.06f, 0.075f, 0.12f, 0.98f), new Vector2(1050f, 650f), Vector2.zero);

        CriarTexto("Titulo", janela, "ESCOLHA UMA CARTA DA RESERVA", 40f, Color.white,
            new Vector2(900f, 70f), new Vector2(0f, 255f), TextAlignmentOptions.Center);
        CriarTexto("Explicacao", janela, "A carta escolhida será movida para um slot livre do seu deck.", 23f,
            new Color(0.78f, 0.84f, 0.95f, 1f), new Vector2(900f, 55f), new Vector2(0f, 205f), TextAlignmentOptions.Center);

        float largura = 250f;
        float espacamento = 55f;
        float total = cartas.Count * largura + Mathf.Max(0, cartas.Count - 1) * espacamento;
        float inicioX = -total / 2f + largura / 2f;

        for (int i = 0; i < cartas.Count; i++)
        {
            Carta carta = cartas[i];
            float x = inicioX + i * (largura + espacamento);
            CriarBotaoCarta(janela, carta, new Vector2(x, -25f));
        }

        Button cancelar = CriarBotao("Cancelar", janela, "CANCELAR", new Vector2(260f, 62f), new Vector2(0f, -270f));
        cancelar.onClick.AddListener(FecharSemSelecionar);
    }

    private void CriarBotaoCarta(RectTransform parent, Carta carta, Vector2 posicao)
    {
        GameObject obj = new GameObject($"Reserva_{carta.nome}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(250f, 350f);
        rt.anchoredPosition = posicao;

        Image fundo = obj.GetComponent<Image>();
        fundo.color = new Color(0.13f, 0.16f, 0.24f, 1f);

        Button botao = obj.GetComponent<Button>();
        botao.targetGraphic = fundo;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            RectTransform imagemRT = CriarImagem("ImagemCarta", rt, Color.white, new Vector2(210f, 255f), new Vector2(0f, 25f));
            Image img = imagemRT.GetComponent<Image>();
            img.sprite = sr.sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        CriarTexto("Nome", rt, carta.nome, 22f, Color.white, new Vector2(220f, 48f), new Vector2(0f, -135f), TextAlignmentOptions.Center);
        CriarTexto("Raridade", rt, carta.raridade.ToString(), 17f, new Color(0.9f, 0.8f, 0.45f, 1f),
            new Vector2(220f, 32f), new Vector2(0f, -168f), TextAlignmentOptions.Center);

        Carta cartaCapturada = carta;
        botao.onClick.AddListener(() => Selecionar(cartaCapturada));
    }

    private void Selecionar(Carta carta)
    {
        if (!aberto || carta == null)
            return;

        Action<Carta> callback = callbackSelecao;
        FecharInterno();
        callback?.Invoke(carta);
    }

    private void FecharInterno()
    {
        aberto = false;
        callbackSelecao = null;
        callbackCancelar = null;

        if (canvas != null)
            Destroy(canvas.gameObject);

        canvas = null;
        raiz = null;
    }

    private Button CriarBotao(string nome, Transform parent, string texto, Vector2 tamanho, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.34f, 1f);

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        CriarTexto("Texto", rt, texto, 23f, Color.white, tamanho, Vector2.zero, TextAlignmentOptions.Center);
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
