using CsvHelper;
using Newtonsoft.Json;
using System.Dynamic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;


namespace AverageDigital.Core.Serialization
{
    public static class SerializationExtensions
    {
        public static string ToCsv<T>(this T value)
        {
            var json = JsonConvert.SerializeObject(value);
            return FromJsonToCsv(json);
        }

        public static string FromJsonToCsv(this string jsonContent)
        {
            var expandos = JsonConvert.DeserializeObject<ExpandoObject[]>(jsonContent);

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csvWriter = new CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture);

            csvWriter.WriteRecords((expandos as IEnumerable<dynamic>));
            writer.Flush();

            var result = Encoding.UTF8.GetString(stream.ToArray());

            return result;
        }

        public static byte[] ToBytes<T>(this T objeto)
        {
            if (objeto == null)
                return null;

            var formatter = new BinaryFormatter();
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, objeto);

                ms.Flush();
                ms.Position = 0;

                buffer = new byte[ms.Length];
                ms.Read(buffer, 0, buffer.Length);
            }

            return buffer;
        }

        public static T FromBytes<T>(this byte[] bytes)
        {
            if (bytes == null) return default(T);

            var formatter = new BinaryFormatter();

            using (var ms = new MemoryStream(bytes))
            {
                try
                {
                    var value = (T)formatter.Deserialize(ms);
                    return value;
                }
                catch (Exception)
                {
                    throw new InvalidCastException($"Não foi possível converter os bytes informados para o tipo {typeof(T).FullName}.");
                }

            }
        }

        public static string ToBase64<T>(this T objeto)
        {
            var bytes = ToBytes(objeto);

            var encoding = Encoding.GetEncoding("iso-8859-1");
            var binaryText = encoding.GetString(bytes, 0, bytes.Length);
            var base64Text = StringToBase64(binaryText);

            return base64Text;
        }

        public static T FromBase64<T>(this string conteudo)
        {
            var binaryText = Base64ToString(conteudo);

            var encoding = Encoding.GetEncoding("iso-8859-1");
            var bytes = encoding.GetBytes(binaryText);

            return FromBytes<T>(bytes);
        }

        private static string StringToBase64(string text)
        {
            var textBytes = Encoding.Unicode.GetBytes(text);
            var returnValue = Convert.ToBase64String(textBytes);

            return returnValue;
        }

        private static string Base64ToString(string text)
        {
            var textBytes = Convert.FromBase64String(text);
            var returnValue = Encoding.Unicode.GetString(textBytes, 0, textBytes.Length);

            return returnValue;
        }

        public static string ToXml<T>(this T objeto)
        {
            string result;
            using (var sw = new StringWriter())
            {
                var serializador = new XmlSerializer(typeof(T));
                serializador.Serialize(sw, objeto);
                result = sw.ToString();
            }
            return result;
        }

        public static void SerializeTo<T>(this T obj, Stream stream)
        {
            new BinaryFormatter().Serialize(stream, obj);
        }

        public static T Deserialize<T>(this Stream stream)
        {
            var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
