using UnityEngine;
using UnityEngine.SceneManagement;

public class SelecionadorDeInimigo : MonoBehaviour
{
    [Header("Inimigo ligado a este botão")]
    public InimigoBase inimigo;

    [Header("Cena de combate")]
    public string nomeCenaCombate = "CenaCombate";

    public void EnfrentarInimigo()
    {
        if (GerenciadorInimigo.instancia == null)
        {
            Debug.LogError("GerenciadorInimigo não encontrado.");
            return;
        }

        if (inimigo == null)
        {
            Debug.LogError("Nenhum inimigo foi definido neste botão.");
            return;
        }

        GerenciadorInimigo.instancia.SelecionarInimigo(inimigo);
        SceneManager.LoadScene(nomeCenaCombate);
    }
}