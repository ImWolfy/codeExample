using Newtonsoft.Json;

namespace AutoCallerWindowsService.Editors.JsonEditor
{
    public static class Serializer
    {
        /// <summary>
        /// Получить JSON-текст для объекта Т
        /// </summary>
        /// <typeparam name="T">Тип сериализуемого объекта</typeparam>
        /// <param name="arg">Экземляр типа Т</param>
        /// <returns></returns>
        public static string Serialize<T>(this T arg)
        {
            string res = JsonConvert.SerializeObject(arg, Formatting.Indented);
            return res;
        }

        /// <summary>
        /// Получить объект типа Т для JSON
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="json">Текст для десериализации</param>
        /// <returns></returns>
        public static T Deserialize<T>(string json)
        {
            T res = JsonConvert.DeserializeObject<T>(json);
            return res;
        }
    }
}
