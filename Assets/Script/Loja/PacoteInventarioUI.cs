using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PacoteInventarioUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private Image imagemPacote;
    [SerializeField] private TMP_Text textoNomePacote;
    [SerializeField] private Button botao;

    private PacoteAdquirido pacote;
    private InventarioUI inventarioUI;

    private void Awake()
    {
        if (imagemPacote == null)
            imagemPacote = GetComponent<Image>();

        if (botao == null)
            botao = GetComponent<Button>();
    }

    public void Configurar(PacoteAdquirido pacoteRecebido, InventarioUI inventarioRecebido)
    {
        pacote = pacoteRecebido;
        inventarioUI = inventarioRecebido;

        if (pacote == null)
        {
            Debug.LogWarning("PacoteInventarioUI recebeu um pacote nulo.");
            return;
        }

        if (imagemPacote != null)
        {
            imagemPacote.sprite = pacote.imagemPacote;
            imagemPacote.preserveAspect = true;
        }

        if (textoNomePacote != null)
            textoNomePacote.text = pacote.nomePacote;

        if (botao != null)
        {
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(AoClicar);
        }
    }

    private void AoClicar()
    {
        if (inventarioUI == null || pacote == null)
            return;

        inventarioUI.SelecionarPacote(pacote);
    }

    public PacoteAdquirido ObterPacote()
    {
        return pacote;
    }
}
