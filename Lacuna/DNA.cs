using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Lacuna
{
    public class DNA
    {
        private String Username { get; set; }
        private String Password { get; set; }
        
        public DNA(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public DNA()
        {
        }

        // Variáveis globais para armazenarem id e token
        static string? tokenAuthorization;
        static string? id;


        //Obter Token de Acesso
        public async Task<string> GetToken()
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage();

            var user = new { username = Username, password = Password };
            var json = JsonConvert.SerializeObject(user);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var url = "https://gene.lacuna.cc/api/users/login";
            var response = await client.PostAsync(url, data);

            var content = response.Content.ReadAsStringAsync().Result;
            var arrJSON = (JObject)JsonConvert.DeserializeObject(content);

            var getToken = arrJSON["accessToken"].ToString();

            return getToken;
        }

        //Solicitar um trabalho
        public async Task GetJob()
        {
            HttpClient httpClient = new HttpClient();
            var request = new HttpRequestMessage();

            var url = "https://gene.lacuna.cc/api/dna/jobs";
            tokenAuthorization = await GetToken();

            using HttpClient client = httpClient;
            client.DefaultRequestHeaders.Add("Authorization", "Bearer" + tokenAuthorization);

            var response = await client.GetAsync(url);

            var content = response.Content.ReadAsStringAsync().Result;
            var arrJSON = (JObject)JsonConvert.DeserializeObject(content);

            var type = arrJSON["job"]["type"].ToString();
            Console.WriteLine("type: " + type);
            id = arrJSON["job"]["id"].ToString();
 
            switch (type)
            {
                case "DecodeStrand":
                    var strandEncoded = arrJSON["job"]["strandEncoded"].ToString();
                    await DecodeStrand(strandEncoded);
                    
                    break;
                case "EncodeStrand":
                    var strand = arrJSON["job"]["strand"].ToString();
                    await EncodeStrand(strand);
                    break;
                case "CheckGene":
                    var strandEncodedCheck = arrJSON["job"]["strandEncoded"].ToString();
                    var geneEncoded = arrJSON["job"]["geneEncoded"].ToString();
                    await CheckGene(strandEncodedCheck, geneEncoded);
                    break;
                default:
                    break;
            }    
        }
        //Operação de decodificação de fita
        public async Task DecodeStrand(string strandEncoded)
        {
            var value = ConvertBase64ToString(strandEncoded);
            var url = "https://gene.lacuna.cc/api/dna/jobs/" + id + "/decode";
            var strand = new { strand = value };
            await PostRequest(url, strand, tokenAuthorization);
        }

        //Codificar operação de fita
        public async Task EncodeStrand(string strand)
        {
            var value = ConvertStringToBase64(strand);
            var url = "https://gene.lacuna.cc/api/dna/jobs/" + id + "/encode";
            var obj = new { strandEncoded = value };
            await PostRequest(url, obj, tokenAuthorization);
        }

        //Verificar a operação do gene
        public async Task CheckGene(string strandEncoded, string geneEncoded)
        {
            var value = CheckString(strandEncoded, geneEncoded);
            var url = "https://gene.lacuna.cc/api/dna/jobs/" + id + "/gene";
            var strand = new { strand = value };
            await PostRequest(url, strand, tokenAuthorization);
        }
        //Função para envio das respostas
        public async Task PostRequest(string url, object value, string tokenAuthorization)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage();        

            var json = JsonConvert.SerializeObject(value);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenAuthorization);

            var response = await client.PostAsync(url, data);

            var content = response.Content.ReadAsStringAsync().Result;
            var arrJSON = (JObject)JsonConvert.DeserializeObject(content);
            Console.WriteLine(arrJSON);
        }
        
        // Operações de Decodificação
        static string ConvertBase64ToString(string base64)
        {
            var hex = Base64ToHex(base64);
            var bin = HexToBinary(hex);
            var str = BinaryToString(bin);
            
            return str;
        }
        static string Base64ToHex(string strInput)
        {
            try
            {
                var bytes = Convert.FromBase64String(strInput);
                var hex = BitConverter.ToString(bytes);
                return hex.Replace("-", "").ToLower();
            }
            catch (Exception)
            {
                return "-1";
            }
        }
        static string HexToBinary(string hexValue)
        {
            string binaryVal = String.Join(String.Empty, hexValue.Select(
                c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );
            if (binaryVal.Length % 2 != 0)
            {
                Console.WriteLine("3");
                binaryVal = binaryVal.Insert(0, "0");
            }
            return binaryVal;
        }
        static string BinaryToString(string binaryValue)
        {
            var tam = binaryValue.Length / 2;
            string[] bits = new string[tam];
            string result = "";
            var count = 0;
            for (var i = 0; i < binaryValue.Length; i += 2)
            {
                bits[count] = binaryValue[i].ToString() + binaryValue[i + 1].ToString();
                count++;
            }
            for (var i = 0; i < bits.Length; i++)
            {
                switch (bits[i])
                {
                    case "00":
                        result += "A";
                        break;
                    case "01":
                        result += "C";
                        break;
                    case "11":
                        result += "T";
                        break;
                    case "10":
                        result += "G";
                        break;
                }
            }
            return result;
        }
        // Termina aqui as funções de decodificação


        // Operações de Codificação
        static string ConvertStringToBase64(string String)
        {
            var bin = StringToBinary(String);
            var hex = BinaryToHex(bin);
            var b64 = HexToBase64(hex);
            return b64;
        }
       
        static string StringToBinary(string stringValue)
        {
            string result = "";
            for (var i = 0; i < stringValue.Length; i++)
            {
                switch (stringValue[i])
                {
                    case 'A':
                        result += "00";
                        break;
                    case 'C':
                        result += "01";
                        break;
                    case 'T':
                        result += "11";
                        break;
                    case 'G':
                        result += "10";
                        break;
                }
            }
            return result;
        }
        public static string BinaryToHex(string binary)
        {
            if (string.IsNullOrEmpty(binary))
                return binary;
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);
            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }
            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }
            return result.ToString();
        }
        public static string HexToBase64(string hex)
        {
            byte[] data = ConvertFromStringToHex(hex);
            string base64 = Convert.ToBase64String(data);
            return base64;
        }
        public static byte[] ConvertFromStringToHex(string hex)
        {
            hex = hex.Replace("-", "");

            byte[] resultantArray = new byte[hex.Length / 2];
            for (int i = 0; i < resultantArray.Length; i++)
            {
                resultantArray[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return resultantArray;
        }
        // Termina aqui as operações de Decodificação

        // Função para checar se conteúdo do DNA está presente na fita molde do DNA 
        static Boolean CheckString(string strandEncoded, string geneEncoded)
        {
            return strandEncoded.Contains(geneEncoded);
        }


        static async Task Main(string[] args)
        {
            
            var token = new DNA("Kemuel", "Kemuel20");

            // Requisitar Job e realizar as operações
            await token.GetJob();

        }
    }
}
