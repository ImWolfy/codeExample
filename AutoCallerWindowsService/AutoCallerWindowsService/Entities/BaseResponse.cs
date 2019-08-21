using System.Collections.Generic;

namespace AutoCallerWindowsService.Entities
{
    class BaseResponse<T>
    {
        public bool Error { get; set; }        
        public string Message { get; set; }
        public List<T> Response { get; set; }
    }
}
