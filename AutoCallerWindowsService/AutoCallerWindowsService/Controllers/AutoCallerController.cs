using AutoCallerWindowsService.Controllers.DbController;
using AutoCallerWindowsService.Editors.JsonEditor;
using AutoCallerWindowsService.Entities;
using AutoCallerWindowsService.Global;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace AutoCallerWindowsService.Controllers
{
    public class AutoCallerController : ApiController
    {
        [HttpPost]
        [ActionName("CallRequest")]
        public async Task<IHttpActionResult> CallRequestRegistration([FromUri]string param1, [FromUri]string param2, [FromBody]string param3)
        {
            try
            {
                var fileText = await Request.Content.ReadAsStringAsync();
                if (!CheckingHelper<string>.CheckForNullEmptyWhiteSpace(fileText))
                    return ResponseMessage(RequiredParameterIsLost("Текст файла"));

                FileController.FileController.SaveFile(param1, fileText, Settings.Instance.BasePath);
                var dbRequest = new DbRequest();
                dbRequest.InsertNewFileInfo(param2, param1);
                var response = new BaseStringResponse()
                {
                    Error = false,
                    Message = "Запрос выполнен успешно",
                    Response = null
                };
                var resultJson = Serializer.Serialize(response);
                var result = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
                };
                return ResponseMessage(result);
            }
            catch (Exception exc)
            {
                return ResponseMessage(RequestError(exc.Message));
            }
        }

        [HttpPost]
        [ActionName("GetCallState")]
        public IHttpActionResult GetCallState([FromBody]FileNames param1)
        {
            try
            {
                if (!CheckingHelper<FileNames>.CheckForNull(param1))
                    return ResponseMessage(RequiredParameterIsLost("Names"));
                var callStates = new List<CallState>();
                var dbRequest = new DbRequest();
                callStates = dbRequest.GetCallStates(param1.Files);
                var response = new BaseResponse<CallState>()
                {
                    Error = false,
                    Message = "Запрос выполнен успешно",
                    Response = callStates
                };
                var resultJson = Serializer.Serialize(response);
                var result = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
                };
                return ResponseMessage(result);
            }
            catch (Exception exc)
            {
                LogWriter.Add($"{exc.ToString()} {exc.InnerException.ToString()}");
                return ResponseMessage(RequestError(exc.Message));
            }
        }

        [NonAction]
        private HttpResponseMessage RequiredParameterIsLost(string parameterName)
        {
            var response = new BaseStringResponse()
            {
                Error = true,
                Message = $"Отсутствует обязательный параметр {parameterName}",
                Response = null
            };
            var resultJson = Serializer.Serialize(response);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
            };
        }

        [NonAction]
        private HttpResponseMessage RequestError(string message)
        {
            var response = new BaseStringResponse()
            {
                Error = true,
                Message = message,
                Response = null
            };
            var resultJson = Serializer.Serialize(response);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
            };
        }
    }

    public class FileText
    {
        public string Text { get; set; }
    }

    public class FileNames
    {
        public List<string> Files { get; set; }
    }
}
