using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EfeitoHabilidade
{
    public enum TipoEfeito { Sobrecarga, Fogo, Sangramento }

    [Header("Tipo de efeito")]
    public TipoEfeito tipoEfeito = TipoEfeito.Fogo;

    [Header("Chance do efeito")]
    [Range(0f, 100f)] public float chanceAplicar = 100f;

    [Header("Duração do efeito")]
    [Min(1)] public int duracaoTurnos = 1;

    [Header("Dano por turno")]
    [Min(0)] public int danoPorTurno = 0;
}

[System.Serializable]
public class HabilidadeCarta
{
    public enum TipoHabilidade { Nenhuma, Dano, Defesa, Buff, Anulacao, Disfarce }
    public enum TipoBuff { Nenhum, Vida, Dano, Defesa }
    public enum AlvoHabilidade { Nenhum, PropriaCarta, CartaAliada, CartaInimiga }

    [Header("Identificação")]
    public string nomeHabilidade;
    [TextArea] public string descricaoHabilidade;

    [Header("Tipo da habilidade")]
    public TipoHabilidade tipoHabilidade = TipoHabilidade.Nenhuma;
    public AlvoHabilidade alvoHabilidade = AlvoHabilidade.Nenhum;
    public TipoBuff tipoBuff = TipoBuff.Nenhum;
    public int valorHabilidade = 0;

    [Header("Efeitos extras que essa habilidade pode aplicar")]
    public List<EfeitoHabilidade> efeitos = new List<EfeitoHabilidade>();

    [Header("Duração para defesa/buff")]
    [Min(1)] public int duracaoHabilidadeTurnos = 1;

    [Header("Cooldown das habilidades comuns")]
    public bool usarCooldown = true;
    [Min(0)] public int cooldownTurnos = 1;

    [Header("Habilidade especial")]
    public bool habilidadeEspecial = false;

    [Header("Ativação especial por sacrifício")]
    public bool exigirSacrificioCartas = false;
    [Min(0)] public int quantidadeCartasParaSacrificar = 0;

    [Header("Ativação especial por dano total")]
    public bool exigirDanoTotalCausado = false;
    [Min(0)] public int danoTotalNecessario = 0;

    [Header("Ativação especial por vida")]
    public bool exigirVidaMenorOuIgual = false;
    [Min(0)] public int vidaNecessariaMenorOuIgual = 0;

    [Header("Ativação especial por cooldown")]
    public bool usarCooldownEspecial = false;
    [Min(0)] public int cooldownEspecialTurnos = 0;

    [Header("Ativação em conjunto")]
    public bool ativacaoEmConjunto = false;
    public Carta cartaNecessariaNoTabuleiro;
    public string nomeBotaoConjunto = "Habilidade em Conjunto";

    public bool EstaConfigurada()
    {
        if ((string.IsNullOrEmpty(nomeHabilidade) || nomeHabilidade.Trim().Length == 0))
            return false;

        if (tipoHabilidade == TipoHabilidade.Nenhuma)
            return false;

        if (alvoHabilidade == AlvoHabilidade.Nenhum)
            return false;

        if (tipoHabilidade == TipoHabilidade.Buff && tipoBuff == TipoBuff.Nenhum)
            return false;

        if (tipoHabilidade == TipoHabilidade.Dano || tipoHabilidade == TipoHabilidade.Defesa || tipoHabilidade == TipoHabilidade.Buff)
        {
            if (valorHabilidade <= 0 && (efeitos == null || efeitos.Count == 0))
                return false;
        }

        return true;
    }

    public bool PossuiEfeito(EfeitoHabilidade.TipoEfeito tipo)
    {
        if (efeitos == null)
            return false;

        for (int i = 0; i < efeitos.Count; i++)
        {
            if (efeitos[i] != null && efeitos[i].tipoEfeito == tipo)
                return true;
        }

        return false;
    }
}

[System.Serializable]
public class Carta : MonoBehaviour
{
    public enum Raridade { Comum, Epico, Mitico, Prodigio, Celeste, Scarlet, Deus }

    public enum Estrela
    {
        UmaEstrela = 1,
        DuasEstrelas = 2,
        TresEstrelas = 3
    }

    [Header("Estatísticas da Carta")]
    public string nome;

    [Header("Estrelas da Carta")]
    public Estrela estrelas = Estrela.UmaEstrela;

    public int dano;
    public int vida;
    public int defesa;
    public Raridade raridade;

    [Header("Habilidades da Carta")]
    [Range(0, 4)] public int quantidadeHabilidades = 0;
    public List<HabilidadeCarta> habilidades = new List<HabilidadeCarta>();

    [Header("Efeitos atuais na carta")]
    public bool efeitoSobrecargaAtivo = false;
    public int turnosSobrecargaRestantes = 0;

    public bool efeitoFogoAtivo = false;
    public int turnosFogoRestantes = 0;
    public int danoFogoPorTurno = 0;

    public bool efeitoSangramentoAtivo = false;
    public int turnosSangramentoRestantes = 0;
    public int danoSangramentoPorTurno = 0;

    [Header("Disfarce")]
    public bool disfarceAtivo = false;
    public Sprite spriteOriginalAntesDoDisfarce;

    public void DefinirEstatisticas(string _nome, int _dano, int _vida, int _defesa, Raridade _raridade)
    {
        nome = _nome;
        dano = _dano;
        vida = _vida;
        defesa = _defesa;
        raridade = _raridade;
    }

    public string ObterEstrelas()
    {
        switch (estrelas)
        {
            case Estrela.UmaEstrela:
                return "?";

            case Estrela.DuasEstrelas:
                return "??";

            case Estrela.TresEstrelas:
                return "???";
        }

        return "?";
    }

    public void ExibirInfoCarta()
    {
        Debug.Log($"Carta: {nome} | Estrelas: {ObterEstrelas()} | Dano: {dano} | Vida: {vida} | Defesa: {defesa} | Raridade: {raridade}");
    }

    public int QuantidadeHabilidadesValidas()
    {
        int quantidade = 0;
        int limite = Mathf.Min(Mathf.Clamp(quantidadeHabilidades, 0, 4), habilidades.Count);

        for (int i = 0; i < limite; i++)
        {
            if (habilidades[i] != null && habilidades[i].EstaConfigurada())
                quantidade++;
        }

        return quantidade;
    }

    public bool TemHabilidadeValida()
    {
        return QuantidadeHabilidadesValidas() > 0;
    }

    public HabilidadeCarta ObterHabilidade(int indice)
    {
        if (indice < 0)
            return null;

        if (indice >= habilidades.Count)
            return null;

        if (indice >= Mathf.Clamp(quantidadeHabilidades, 0, 4))
            return null;

        HabilidadeCarta habilidade = habilidades[indice];

        if (habilidade == null || !habilidade.EstaConfigurada())
            return null;

        return habilidade;
    }

    public HabilidadeCarta ObterPrimeiraHabilidadeValida()
    {
        int limite = Mathf.Min(Mathf.Clamp(quantidadeHabilidades, 0, 4), habilidades.Count);

        for (int i = 0; i < limite; i++)
        {
            HabilidadeCarta habilidade = ObterHabilidade(i);
            if (habilidade != null)
                return habilidade;
        }

        return null;
    }

    public bool PossuiEfeitoNegativoAtivo()
    {
        return efeitoSobrecargaAtivo || efeitoFogoAtivo || efeitoSangramentoAtivo;
    }
}