using UnityEngine;

public class GerenciadorInimigo : MonoBehaviour
{
    public static GerenciadorInimigo instancia;

    [Header("Inimigo selecionado")]
    public InimigoBase inimigoSelecionado;

    [Header("Cena para retornar após o combate")]
    public string nomeCenaRetorno;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelecionarInimigo(InimigoBase inimigo)
    {
        if (inimigo == null)
        {
            Debug.LogWarning("Tentou selecionar um inimigo nulo.");
            return;
        }

        inimigoSelecionado = inimigo;
        Debug.Log($"Inimigo selecionado: {inimigo.nomeInimigo}");
    }

    public void DefinirCenaRetorno(string nomeCena)
    {
        nomeCenaRetorno = nomeCena;
    }

    public void LimparInimigoSelecionado()
    {
        inimigoSelecionado = null;
    }
}
