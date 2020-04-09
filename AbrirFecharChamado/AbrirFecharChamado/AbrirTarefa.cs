namespace AbrirChamado
{
    public class DadosBasicos
    {
        public string Assunto { get; set; }
        public string Descricao { get; set; }
        public string DescricaoOrigem { get; set; }
        public int Atividade { get; set; }
    }

    public class Papeis
    {
        public string ANTE { get; set; }
    }

    public class Conjuntos
    {
        public DadosBasicos DadosBasicos { get; set; }
        public Papeis Papeis { get; set; }
    }

    public class RootObject
    {
        public int Formulario { get; set; }
        public Conjuntos Conjuntos { get; set; }
    }
}