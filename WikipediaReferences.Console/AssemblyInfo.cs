using System.Reflection;

namespace WikipediaReferences.Console
{
    public class AssemblyInfo
    {
        public readonly Assembly ExecutingAssembly;

        public AssemblyInfo()
        {
            ExecutingAssembly = Assembly.GetExecutingAssembly();
        }

        public AssemblyName GetAssemblyName()
        {
            return ExecutingAssembly.GetName();
        }

        public string GetAssemblyValue(string propertyName, AssemblyName assemblyName)
        {
            return GetAssemblyPropertyValue(assemblyName, propertyName).ToString();
        }

        private object GetAssemblyPropertyValue(object src, string propertyName)
        {
            return src.GetType().GetProperty(propertyName).GetValue(src, null);
        }
    }
}
