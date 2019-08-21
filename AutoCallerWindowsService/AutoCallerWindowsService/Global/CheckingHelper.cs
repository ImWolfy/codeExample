using System.Reflection;

namespace AutoCallerWindowsService.Global
{
    class CheckingHelper<T>
    {

        public static bool CheckForNullEmptyWhiteSpace(string parameter)
        {
            return !string.IsNullOrEmpty(parameter) && !string.IsNullOrWhiteSpace(parameter);
        }

        public static bool CheckForNull(T obj)
        {
            if (obj == null) return false;
            foreach (var pi in obj.GetType().GetProperties())
            {
                if (pi.PropertyType == typeof(string))
                {
                    string value = (string)pi.GetValue(obj);
                    if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                        return false;
                }
            }
            return true;
        }
    }
}
