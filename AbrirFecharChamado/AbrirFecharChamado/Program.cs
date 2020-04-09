using FecharChamado;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace AbrirChamado
{
    class Program
    {
        #region Método Principal chamado na execução do programa
        private static void Main(string[] args)
        {
            getArquivosAbrirChamado();
            getArquivosFecharChamado();
        }
        #endregion

        #region Busca os arquivos no diretório referentes a Abertura de chamados
        private static void getArquivosAbrirChamado()
        {
            string[] fileEntries = Directory.GetFiles(ConfigurationManager.AppSettings["diretorio-abrir-chamado"]);
            string destino = ConfigurationManager.AppSettings["diretorio-abrir-chamado"] + "Log-Chamados-Abertos\\";

            foreach (var item in fileEntries)
            {
                if (item.Contains(".txt"))
                {
                    var fileText = File.ReadAllText(item);
                    RootObject body = new RootObject();
                    body.Conjuntos = new Conjuntos();
                    body.Conjuntos.DadosBasicos = new DadosBasicos();
                    body.Conjuntos.Papeis = new Papeis();
                    Arquivo arquivo = JsonConvert.DeserializeObject<Arquivo>(fileText);

                    string assunto = arquivo.Descricao.Substring(0, arquivo.Descricao.IndexOf('-'));

                    body.Formulario = 68;
                    body.Conjuntos.DadosBasicos.Assunto = assunto;
                    body.Conjuntos.DadosBasicos.Descricao = arquivo.Descricao.ToString() + " \n Conforme chamado matriz - " + arquivo.Id;
                    body.Conjuntos.DadosBasicos.DescricaoOrigem = "Chatbot";
                    body.Conjuntos.DadosBasicos.Atividade = Convert.ToInt32(arquivo.IdTar);
                    body.Conjuntos.Papeis.ANTE = "{ IdParticipante : " + arquivo.IdParticipante + ", IdTipoPapel : 1 }";
                    
                    File.WriteAllText(destino + "Chamado - " + arquivo.Id + " - aberto.txt", body.Conjuntos.DadosBasicos.Descricao);
                    File.Delete(item);

                    AbrirTicket(body, arquivo.Id);
                }
            }
        }
        #endregion

        #region Realiza o POST na API do ASK para abertura de chamados
        private static void AbrirTicket(RootObject body, string idTicket)
        {
            string api = ConfigurationManager.AppSettings["api-url-abrir"].ToString();

            var URL = new Uri(api);

            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) })
            {
                client.DefaultRequestHeaders.Add("Authorization", ConfigurationManager.AppSettings["bearer"].ToString());
                var serializedProduto = JsonConvert.SerializeObject(body);
                var content = new StringContent(serializedProduto, Encoding.UTF8, "application/json");

                ServicePointManager.SecurityProtocol =
                                SecurityProtocolType.Ssl3 |
                                SecurityProtocolType.Tls |
                                SecurityProtocolType.Tls11 |
                                SecurityProtocolType.Tls12;

                try
                {
                    using (var response = client.PostAsync(URL, content).Result)
                    {
                        var retornoIdChamado = response.Content.ReadAsStringAsync().Result;
                        string idRequisicao = retornoIdChamado.Replace("\"", "");

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Chamado aberto com sucesso!" + retornoIdChamado);

                            ConcluirTarefa chamadoAberto = new ConcluirTarefa();

                            chamadoAberto.Id = idRequisicao;
                            chamadoAberto.Descricao = "Chamado nº " + idRequisicao + " - " + body.Conjuntos.DadosBasicos.Assunto;

                            string json = JsonConvert.SerializeObject(chamadoAberto);

                            File.WriteAllText(ConfigurationManager.AppSettings["diretorio-abrir-chamado"].ToString() + "\\Log\\"
                                   + idTicket + " - " + body.Conjuntos.DadosBasicos.Assunto + ".txt", json);

                            File.WriteAllText(ConfigurationManager.AppSettings["diretorio-abrir-chamado"].ToString() + "\\Log-Chamados-Abertos\\"
                                   + idRequisicao + " - " + body.Conjuntos.DadosBasicos.Assunto + ".txt", json);
                        }
                        else
                        {
                            Console.WriteLine("Não foi possível abrir o chamado: " + response.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string exception = "Não foi possível abrir o chamado: " + ex.InnerException;
                    Console.WriteLine(exception);
                    TratarExcecao(ex);
                }
            }
        }
        #endregion

        #region Busca os arquivos no diretório referentes a Encerramento de chamados
        private static void getArquivosFecharChamado()
        {
            string[] fileChamadosFechar = Directory.GetFiles(ConfigurationManager.AppSettings["diretorio-fechar-chamado"]);
            string[] fileChamadosAbertos = Directory.GetFiles(ConfigurationManager.AppSettings["diretorio-abrir-chamado"] + "Log\\");
            string destino = ConfigurationManager.AppSettings["diretorio-fechar-chamado"] + "Log-Chamados-Encerrados\\";

            foreach (var item in fileChamadosFechar)
            {
                if (item.Contains(".txt"))
                {
                    var file = File.ReadAllText(item);
                    ConcluirTarefa ticket = JsonConvert.DeserializeObject<ConcluirTarefa>(file);

                    foreach (var itemAberto in fileChamadosAbertos)
                    {
                        if (itemAberto.Contains(ticket.Id))
                        {
                            var fileTicketAberto = File.ReadAllText(itemAberto);
                            ConcluirTarefa ticketAberto = JsonConvert.DeserializeObject<ConcluirTarefa>(fileTicketAberto);

                            File.WriteAllText(destino + "Chamado - " + ticket.Id + " - encerrado.txt", file);
                            File.Delete(itemAberto);
                            ticket.Descricao += " \n " + ticketAberto.Descricao;
                        }
                    }

                    File.Delete(item);
                    ConcluirTicket(ticket);
                }
            }
        }
        #endregion

        #region Realiza o POST na API do ASK para encerramento de chamados
        private static void ConcluirTicket(ConcluirTarefa ticket)
        {
            string api = ConfigurationManager.AppSettings["api-url"].ToString();

            var URL = new Uri(api + "/requisicoes/" + ticket.Id + "/acoes");          

            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) })
            {

                ConcluirTarefa concluir = new ConcluirTarefa();

                concluir.Id = "Concluir Requisição [CONC]";
                concluir.Descricao = "Olá, sou a Gabi - " + ticket.Descricao;

                client.DefaultRequestHeaders.Add("Authorization", ConfigurationManager.AppSettings["bearer"].ToString());
                var serializedProduto = JsonConvert.SerializeObject(concluir);
                var content = new StringContent(serializedProduto, Encoding.UTF8, "application/json");

                ServicePointManager.SecurityProtocol =
                                    SecurityProtocolType.Ssl3 |
                                    SecurityProtocolType.Tls |
                                    SecurityProtocolType.Tls11 |
                                    SecurityProtocolType.Tls12;

                try
                {
                    using (var response = client.PostAsync(URL, content).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Requisição " + ticket.Id + " concluída com sucesso!");

                            //TO DO: Implementar método abaixo apenas em produção
                            //Email.EnviarEmailSucesso("chamado - " + ticket.Id + ticket.Descricao);
                        }
                        else
                        {
                            Console.WriteLine("Não foi concluir o chamado: " + response.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string exception = "Não foi possível concluir o chamado: " + ex.InnerException;
                    Console.WriteLine(exception);
                    TratarExcecao(ex);

                }
            }
        }
        #endregion

        #region Tratamento de exceção e envio de email reportando o erro
        private static void TratarExcecao(Exception ex)
        {
            string excecao = "";

            if (ex.InnerException != null)
            {
                excecao = ex.InnerException.ToString();
            }

            if (excecao.Contains("A task was canceled"))
            {
                excecao = "Erro de TimeOut";
            }

            string erro = "Não foi possível abrir o chamado - " + excecao;
            Console.WriteLine(erro);
            //TO DO: Implementar método abaixo apenas em produção
            //Email.EnviarEmail(erro);
        }
        #endregion
    }
}
