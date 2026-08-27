using UnityEngine;

/// <summary>
/// Marcador colocado automaticamente em cartas criadas por habilidades de Invocação.
/// Não precisa ser adicionado manualmente aos prefabs.
/// </summary>
public class CartaInvocadaRuntime : MonoBehaviour
{
    [Header("Estado da invocação - runtime")]
    public bool pertenceAoPlayer;
    public bool permanente = true;
    [Min(0)] public int turnosRestantes = 0;
    public bool ignorarPrimeiraChecagem = true;
    public bool expirando = false;
    public string habilidadeOrigem = "";

    public void Configurar(bool donoPlayer, bool invocacaoPermanente, int duracaoTurnos, string origem)
    {
        pertenceAoPlayer = donoPlayer;
        permanente = invocacaoPermanente;
        turnosRestantes = permanente ? 0 : Mathf.Max(1, duracaoTurnos);
        ignorarPrimeiraChecagem = !permanente;
        expirando = false;
        habilidadeOrigem = origem ?? "";
    }
}
