using UnityEngine;

[System.Serializable]
public class Carta : MonoBehaviour
{
    public enum Raridade { Comum, Epico, Mitico, Prodigio, Celeste, Deus }

    public string nome;
    public int dano;
    public int vida;
    public int defesa;
    public Raridade raridade;

    public void DefinirEstatisticas(string _nome, int _dano, int _vida, int _defesa, Raridade _raridade)
    {
        nome = _nome;
        dano = _dano;
        vida = _vida;
        defesa = _defesa;
        raridade = _raridade;
    }

    public void ExibirInfoCarta()
    {
        Debug.Log($"Carta: {nome} | Dano: {dano} | Vida: {vida} | Defesa: {defesa} | Raridade: {raridade}");
    }
}