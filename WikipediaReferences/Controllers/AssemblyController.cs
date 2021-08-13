using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VersionReferences.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AssemblyController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AssemblyController> logger;
        public readonly Assembly ExecutingAssembly;

        public AssemblyController(IConfiguration configuration, ILogger<AssemblyController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            ExecutingAssembly = Assembly.GetExecutingAssembly();
        }

        [HttpGet]
        public ContentResult GetAssemblyInfo()
        {
            var assemblyName = ExecutingAssembly.GetName();

            return Content($"{assemblyName.Name}\r\nVersion: { assemblyName.Version}");
        }

        [HttpGet("properties")]
        public IActionResult GetProperties()
        {
            try
            {
                var assemblyName = ExecutingAssembly.GetName();

                var properties = assemblyName.GetType().GetProperties();

                string response = $"{assemblyName.Name}\r\n\r\n";

                properties.ToList().ForEach(p =>
                {
                    response += $"{p.Name}: {GetAssemblyPropertyValue(assemblyName, p.Name)}\r\n";
                });

                return Ok(response);
            }
            catch (Exception e)
            {
                string message = $"Getting the properties failed.\r\nException:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }

        [HttpGet("property/{property}")]
        public IActionResult GetPropertyValue(string property)
        {
            try
            {
                var assemblyName = ExecutingAssembly.GetName();

                return Ok($"{assemblyName.Name}\r\n{property}: {GetAssemblyPropertyValue(assemblyName, property)}");
            }
            catch (Exception e)
            {
                string message = $"Getting the propery value failed. Property = {property}\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }

        private string GetAssemblyPropertyValue(AssemblyName assemblyName, string property)
        {
            try
            {
                if (property.Contains("CodeBase"))
                    return "not available";
                else
                    return assemblyName.GetType().GetProperty(property).GetValue(assemblyName, null).ToString();
            }
            catch (Exception)
            {
                return "not available";
            }
        }
    }
}
