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

        ConfigurarVisual();

        if (botao != null)
        {
            botao.interactable = true;
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(AoClicar);
        }
        else
        {
            Debug.LogWarning("PacoteInventarioUI não possui um Button configurado.");
        }
    }

    private void ConfigurarVisual()
    {
        if (imagemPacote != null)
        {
            imagemPacote.preserveAspect = true;
            imagemPacote.raycastTarget = true;

            if (pacote.imagemPacote != null)
            {
                // Se um dia você voltar a colocar uma arte de pacote, ela continua funcionando.
                imagemPacote.sprite = pacote.imagemPacote;
                imagemPacote.color = Color.white;
            }
            else
            {
                // Não existe imagem: o próprio Image vira uma embalagem simples colorida.
                imagemPacote.sprite = null;
                imagemPacote.color = pacote.corPrincipalPacote;
                imagemPacote.preserveAspect = false;
            }
        }

        if (textoNomePacote != null)
        {
            textoNomePacote.text = pacote.nomePacote;
            textoNomePacote.color = pacote.corTextoPacote;
            textoNomePacote.raycastTarget = false;
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
