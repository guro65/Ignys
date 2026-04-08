using UnityEngine;
using UnityEngine.SceneManagement;

public class TrocarCena : MonoBehaviour
{
    [Header("Nome da cena que será carregada")]
    public string nomeDaCena;

    public void CarregarCena()
    {
        if (string.IsNullOrEmpty(nomeDaCena))
        {
            Debug.LogError("Nome da cena não foi definido.");
            return;
        }

        SceneManager.LoadScene(nomeDaCena);
    }
}